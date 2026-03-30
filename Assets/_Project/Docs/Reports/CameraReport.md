# TPS カメラシステム 設計・実装レポート

## 概要

Cinemachine 3.x（`CinemachineCamera` + `CinemachineOrbitalFollow`）を使った三人称視点（TPS）カメラシステム。
マウス入力でカメラを自由に回転・ズームでき、プレイヤーはカメラの向きを基準に移動する。
既存の PlayerController（Rigidbody + PI 制御）との最小変更での統合を重視した設計。

---

## ファイル構成

```
Assets/_Project/Scripts/
├── Camera/
│   └── CameraController.cs        # カメラ回転・ズーム・カーソル管理
└── MoveableObjects/Player/
    ├── InputReader.cs              # [変更] LookInput を追加
    ├── PlayerApiHub.cs             # [変更] CameraTransform を追加
    └── MovementMotor.cs            # [変更] カメラ基準移動に変更
```

---

## クラス責務一覧

| クラス | 変更種別 | 責務 |
|--------|----------|------|
| `CameraController` | 新規 | OrbitalFollow の軸駆動・ズーム・カーソルロック管理 |
| `InputReader` | 追加 | マウスデルタ（LookInput）の読み取り |
| `PlayerApiHub` | 追加 | CameraTransform フィールドの公開 |
| `MovementMotor` | 変更 | moveDir をカメラ基準ベクトルに変換 |

---

## アーキテクチャ概要

### データフロー

```
[Update]
  InputReader.Update()
      ↓  MoveInput（WASD）/ LookInput（マウスデルタ）/ JumpPressed
  CameraController.Update()
      ↓  LookInput   → OrbitalFollow.HorizontalAxis.Value（水平旋回）
      ↓  LookInput   → OrbitalFollow.VerticalAxis.Value（垂直旋回）
      ↓  ScrollWheel → OrbitalFollow.Radius（ズーム距離）
  PlayerController.Update()
      ↓  new MoveCommand(motor, input).Execute() → motor.SetMoveInput()

[FixedUpdate]
  MovementMotor.ApplyMovement()
      ↓  GetCameraRelativeMoveDir()
              ↓  CameraTransform.forward を XZ 平面に投影
              ↓  CameraTransform.right   を XZ 平面に投影
              ↓  camForward * input.y + camRight * input.x → moveDir
      ↓  PI 制御で moveDir 方向に力を加える
      ↓  FaceDirection(moveDir) で Rigidbody を移動方向へ向ける
```

### 依存関係

```
CameraController
    └── InputReader（FindFirstObjectByType で取得）
    └── CinemachineCamera（Inspector で設定）
    └── CinemachineOrbitalFollow（CinemachineCamera から GetComponent で取得）

PlayerApiHub
    ├── （既存コンポーネント群）
    └── CameraTransform（Inspector で Main Camera をセット）

MovementMotor
    └── PlayerApiHub.CameraTransform → GetCameraRelativeMoveDir()
```

### Cinemachine の入力フロー

```
Cinemachine 3.x のデフォルト入力フロー（使用しない）:
  InputAxisController コンポーネント → Input Action / Input Manager を読み取り → 軸を駆動

このプロジェクトの入力フロー（採用）:
  InputReader → _actions.Player.Look（新 Input System）
      ↓  LookInput プロパティ
  CameraController.DriveCameraAxes()
      ↓  OrbitalFollow.HorizontalAxis.Value / VerticalAxis.Value を直接書き込む
  CameraController.DriveZoom()
      ↓  Mouse.current.scroll → OrbitalFollow.Radius を直接書き込む

採用の理由：
  InputAxisController を使う場合は Input Action を Inspector で再度アサインする手間がかかる。
  InputReader がすでに Look アクションを管理しているため、
  スクリプト側から軸値を直接書き込む方式のほうが依存関係がシンプルになる。
  InputAxisController コンポーネントを追加しなければ Cinemachine は自動で軸を動かさない。
```

---

## 主要実装の解説

### カメラ基準移動方向の計算

[MovementMotor.cs](../MoveableObjects/Player/MovementMotor.cs) の `GetCameraRelativeMoveDir()`

```csharp
Vector3 camForward = Vector3.ProjectOnPlane(_hub.CameraTransform.forward, Vector3.up).normalized;
Vector3 camRight   = Vector3.ProjectOnPlane(_hub.CameraTransform.right,   Vector3.up).normalized;
return (camForward * _moveInput.y + camRight * _moveInput.x).normalized;
```

`Vector3.ProjectOnPlane` で Y 成分を除去する理由：
- カメラが上や下を向いているとき、forward ベクトルには Y 成分が混入する
- そのままでは W キーで上昇・下降する力が加わってしまう
- XZ 平面への投影により、どの仰角からでも水平移動のみになる

`CameraTransform` が null の場合はワールド座標基準（カメラ追加前の動作）にフォールバックするため、
設定漏れでもゲームが止まらない。

### OrbitalFollow 軸の直接駆動

[CameraController.cs](CameraController.cs) の `DriveCameraAxes()`

```csharp
_orbitalFollow.HorizontalAxis.Value += look.x * _horizontalSensitivity;
_orbitalFollow.VerticalAxis.Value = Mathf.Clamp(
    _orbitalFollow.VerticalAxis.Value - look.y * _verticalSensitivity,
    _orbitalFollow.VerticalAxis.Range.x,
    _orbitalFollow.VerticalAxis.Range.y
);
```

| 軸 | 範囲 | 反転の理由 |
| -- | -- | -- |
| HorizontalAxis | Wrap = true でラップアラウンド | なし（右方向が正） |
| VerticalAxis | Range.x〜Range.y（Inspector で設定） | マウス上方向（正）でカメラが上昇するため -1 倍 |

Cinemachine 3.x では `InputAxisController` コンポーネントを追加しない限り自動入力は走らない。
そのためスクリプト側で軸値を直接書き込むだけでよく、2.x のような軸名クリアは不要。

### ズーム

[CameraController.cs](CameraController.cs) の `DriveZoom()`

```csharp
float scroll = Mouse.current.scroll.ReadValue().y;
_orbitalFollow.Radius = Mathf.Clamp(
    _orbitalFollow.Radius - scroll * _zoomSpeed,
    _zoomMin,
    _zoomMax
);
```

- スクロール上（正値）→ Radius を減らす → カメラが近づく
- スクロール下（負値）→ Radius を増やす → カメラが遠ざかる
- `_zoomMin` / `_zoomMax` で距離を上限・下限にクランプする

> **注意**：`CinemachineOrbitalFollow` の Orbit Style が **Sphere** のときのみ `Radius` が直接効く。
> **ThreeRing** の場合は Orbit Style を Sphere に変更すること。

### カーソル管理

| 操作 | 動作 |
| --- | --- |
| Play 開始 | `Cursor.lockState = Locked`（非表示・中央固定） |
| Escape キー | `Cursor.lockState = None`（表示・自由移動） |
| 左クリック（解放中） | `Cursor.lockState = Locked`（再ロック） |

カーソルが解放されている間は `DriveCameraAxes()` が早期リターンするため、
UI 操作中に意図せずカメラが動くことはない。

---

## Inspector 設定値（推奨初期値）

### CameraController

| フィールド | 推奨値 | 説明 |
| --- | --- | --- |
| Virtual Camera | （CinemachineCamera） | OrbitalFollow を持つ CinemachineCamera をアサイン |
| Horizontal Sensitivity | 0.3 | 水平感度。マウスが速すぎる場合は下げる |
| Vertical Sensitivity | 0.005 | 垂直感度。水平より小さい値が自然 |
| Zoom Speed | 1.0 | スクロール 1 ノッチあたりの距離変化量 |
| Zoom Min | 2.0 | カメラの最小距離（一番近い位置） |
| Zoom Max | 15.0 | カメラの最大距離（一番遠い位置） |
| Lock Cursor On Start | true | 通常は true のまま |

### PlayerApiHub（追加フィールド）

| フィールド | 推奨値 | 説明 |
| --- | --- | --- |
| Camera Transform | Main Camera | Hierarchy の Main Camera をドラッグ |

### CinemachineCamera（推奨初期値）

| 項目 | 設定値 | 説明 |
| --- | --- | --- |
| Tracking Target | Player | プレイヤーの Transform |
| Look At Target | （空欄） | 空欄のまま = Tracking Target と同じになる |

### CinemachineOrbitalFollow（推奨初期値）

| 項目 | 推奨値 | 説明 |
| --- | --- | --- |
| Orbit Style | Sphere | ズームスクリプトが Radius を直接操作するため必須 |
| Radius | 5.0 | 初期距離。Zoom Min〜Max の中間あたりに設定する |
| Vertical Axis（Range） | -30〜60 | 俯角 30°〜仰角 60°。好みで調整 |

---

## Unity セットアップ手順

### STEP 1 ── CinemachineCamera を作成する

1. Hierarchy で右クリック → **Cinemachine → CinemachineCamera**
2. 名前を `TPS Virtual Camera` に変更（任意）
3. Inspector の **Tracking Target** に Player オブジェクトをドラッグ

> CinemachineCamera を作成すると、Main Camera に **CinemachineBrain** が自動追加される。
> これがあることで Virtual Camera の出力が Main Camera に反映される。

---

### STEP 2 ── CinemachineOrbitalFollow を追加する

1. `TPS Virtual Camera` を選択
2. **Add Component → Cinemachine → CinemachineOrbitalFollow**
3. Inspector で以下を設定する

| 項目 | 設定値 |
| --- | --- |
| **Orbit Style** | Sphere |
| **Radius** | 5 |
| **Vertical Axis → Range** | X = -30、Y = 60 |

---

### STEP 3 ── CameraController を配置する

1. Hierarchy で右クリック → **Create Empty**
2. 名前を `CameraController` に変更
3. **Add Component** → `CameraController` を追加
4. Inspector の **Virtual Camera** に **TPS Virtual Camera** をドラッグ

---

### STEP 4 ── PlayerApiHub に Main Camera を設定する

1. Hierarchy で **Player** を選択
2. Inspector の `PlayerApiHub` を開く
3. **Camera Transform** フィールドに **Main Camera** をドラッグ

---

### STEP 5 ── 動作確認

1. **Play** ボタンを押す
2. Console に以下が出ていれば初期化成功：

   ```text
   [MovementMotor] 自動計算 → linearDamping=X.XXXX, Kp=X.XXXX  ...
   ```

3. 以下の操作を確認する

| 操作 | 期待する動作 |
|------|-------------|
| マウスを左右に動かす | カメラがプレイヤーを中心に水平旋回する |
| マウスを上下に動かす | カメラの仰角が変わる（見下ろし ↔ 見上げ） |
| マウスホイール上 | カメラが近づく |
| マウスホイール下 | カメラが遠ざかる |
| W キー | カメラが向いている方向へプレイヤーが移動する |
| A キー | カメラの左方向へプレイヤーが移動する |
| Escape キー | カーソルが解放される（カメラ回転が止まる） |
| 左クリック（解放中） | カーソルが再ロックされる |

---

### STEP 6 ── 感度・ズームを調整する

`CameraController` の Inspector でスライダーを調整する：

| 症状 | 対処 |
|------|------|
| カメラの回転が速すぎる | Horizontal / Vertical Sensitivity を下げる |
| カメラの回転が遅すぎる | Horizontal / Vertical Sensitivity を上げる |
| ズームが速すぎる | Zoom Speed を下げる |
| これ以上近づけたい / 遠ざけたい | Zoom Min / Zoom Max を変更する |
| マウスの DPI による差が大きい | Input Action の Look に `Scale` プロセッサを追加して統一する |

---

## よくあるトラブルと対処

| 症状 | 原因 | 対処 |
|------|------|------|
| カメラが動かない | Virtual Camera 未アサイン | `CameraController` の Inspector で CinemachineCamera を設定 |
| WASD がカメラと無関係な方向へ動く | Camera Transform 未設定 | `PlayerApiHub` の Camera Transform に Main Camera をドラッグ |
| カメラが上下逆に動く | Y 軸の反転が合っていない | `DriveCameraAxes()` の `-` を `+` に変える |
| ズームが効かない | Orbit Style が ThreeRing になっている | `CinemachineOrbitalFollow` の Orbit Style を Sphere に変更 |
| カーソルがロックされない | `LockCursorOnStart = false` | Inspector で true に変更 |
| Console に InputReader not found | Player より先に CameraController が Awake している | Script Execution Order で CameraController を後にする |
| `Input` クラスのエラーが出る | 新 Input System 専用プロジェクトで旧 API を使っている | `Input.GetKeyDown` → `Keyboard.current.xxx.wasPressedThisFrame` に変更 |
| Cinemachine の using が解決しない | Cinemachine 未インストール | Package Manager から Cinemachine を Install |

---

## 拡張例

### 障害物でのカメラクリップを防ぐ

`CinemachineCamera` に **CinemachineDeoccluder** コンポーネントを追加する（Cinemachine 3.x 付属）。

| 項目 | 推奨値 |
| --- | --- |
| Obstacle Layer Mask | Default（壁・地形など） |
| Damping | 0.5 |
| Damping When Occluded | 0.1 |

コードの変更は不要。Inspector の追加だけで機能する。

### ロックオンカメラを追加する

```text
追加ファイル:
  LockOnTarget.cs     → ターゲット選択ロジック

CameraController.cs に追加:
  [SerializeField] CinemachineCamera _lockOnCamera;

  public void SetLockOn(Transform target)
  {
      _lockOnCamera.LookAt = target;
      _lockOnCamera.Priority = 20;  // TPS カメラ（Priority=10）より高くして切り替え
  }
```

カメラの Priority を切り替えるだけで Cinemachine が自動でブレンドする。

### カメラシェイクを追加する

```csharp
// CinemachineImpulseSource コンポーネントをアタッチして使う
[SerializeField] private CinemachineImpulseSource _impulseSource;

public void Shake(float force)
{
    _impulseSource.GenerateImpulse(force);
}
```

---

## 設計原則との対応

| 原則 | 実装箇所 | 効果 |
|------|---------|------|
| **OCP**（開放/閉鎖） | `MovementMotor` の変更は内部メソッド追加のみ。既存ロジック不変 | カメラ対応の追加コストが低い |
| **SRP**（単一責務） | 回転・ズーム・カーソル管理を `CameraController` に集約。Player コードと分離 | カメラ挙動のバグが Player コードに影響しない |
| **フォールバック設計** | `CameraTransform == null` 時はワールド基準で動作 | カメラ未設定でもゲームが止まらない |
| **ApiHub 経由参照** | `CameraTransform` を `PlayerApiHub` に追加。直接参照ゼロを維持 | Player コンポーネント間の依存関係の一覧性を維持 |
