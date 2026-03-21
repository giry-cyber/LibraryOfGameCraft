# Player 操作システム 設計・実装レポート

## 概要

Rigidbody ベースの 3D Player 操作システム。
コマンドパターン・ステートパターン・API Hub を組み合わせ、拡張性と保守性を重視した設計。

---

## ファイル構成

```
Assets/_Project/Scripts/MoveableObjects/Player/
├── PlayerController.cs        # 入口クラス（Facade）
├── PlayerApiHub.cs            # 全参照・設定値の集約ハブ
├── PlayerStateMachine.cs      # ステートマシン
├── InputReader.cs             # 入力管理
├── MovementMotor.cs           # Rigidbody 水平移動（PI制御）
├── GroundChecker.cs           # 接地判定（SphereCast）
├── JumpHandler.cs             # ジャンプ管理（2段ジャンプ）
├── Commands/
│   ├── IPlayerCommand.cs      # コマンド基底インターフェース
│   ├── MoveCommand.cs         # 移動コマンド
│   └── JumpCommand.cs         # ジャンプコマンド
└── States/
    ├── IPlayerState.cs        # ステート基底インターフェース
    ├── IdleState.cs           # 接地・静止
    ├── MoveState.cs           # 接地・移動
    ├── JumpState.cs           # 空中上昇
    └── FallState.cs           # 空中落下
```

---

## クラス責務一覧

| クラス | 種別 | 責務 |
|--------|------|------|
| `PlayerController` | MonoBehaviour | Update/FixedUpdate の分岐。コマンド生成・発行 |
| `PlayerApiHub` | MonoBehaviour | 全コンポーネント参照と設定値の一元管理 |
| `InputReader` | MonoBehaviour | 入力の読み取り（差し替えポイント） |
| `MovementMotor` | MonoBehaviour | PI制御による水平力の適用と向き制御 |
| `GroundChecker` | MonoBehaviour | SphereCast による接地判定 |
| `JumpHandler` | MonoBehaviour | ジャンプ要求の受付・実行・残数管理 |
| `PlayerStateMachine` | Plain C# | 状態の保持・遷移・Update委譲 |
| `IPlayerCommand` | Interface | コマンドパターンの基底 |
| `MoveCommand` | Plain C# | 入力値を MovementMotor に渡す |
| `JumpCommand` | Plain C# | JumpHandler にジャンプ要求を登録 |
| `IPlayerState` | Interface | ステートパターンの基底 |
| `IdleState` | Plain C# | 接地・静止。移動入力/ジャンプで遷移 |
| `MoveState` | Plain C# | 接地・移動。PI制御で地上移動 |
| `JumpState` | Plain C# | 空中上昇。頂点到達で FallState へ |
| `FallState` | Plain C# | 空中落下。着地で地上ステートへ |

---

## アーキテクチャ概要

### データフロー

```
[Update]
  InputReader.Update()
      ↓  MoveInput / JumpPressed
  PlayerController.Update()
      ↓  new MoveCommand(motor, input).Execute()   → motor.SetMoveInput()   ← 入力保持のみ
      ↓  new JumpCommand(handler).Execute()        → handler.RequestJump()  ← フラグをセット
      ↓  StateMachine.Update()                     ← 遷移判定

[FixedUpdate]
  PlayerController.FixedUpdate()
      ↓  StateMachine.FixedUpdate()
      ↓  CurrentState.FixedUpdate(hub)
          ↓  motor.ApplyMovement()    ← PI制御で Rigidbody に力を加える
          ↓  handler.TryExecuteJump() ← フラグを確認してジャンプ実行
          ↓  StateMachine.ChangeState(new XxxState())
```

### 依存関係

```
PlayerController
    └── PlayerApiHub（全参照の集約点）
            ├── Rigidbody
            ├── InputReader
            ├── MovementMotor
            ├── GroundChecker
            ├── JumpHandler
            └── PlayerStateMachine
                    └── IPlayerState（各ステート実装）
```

各コンポーネント間の直接参照はゼロ。すべて PlayerApiHub を経由する。

### ステート遷移図

```
          ┌────────────────────────────────────┐
          │             [IdleState]             │
          │         接地・入力なし              │
          └──┬──────────────┬──────────────────┘
     移動入力│         ジャンプ│
             ▼               ▼
    [MoveState]         [JumpState]
    接地・移動          空中上昇
             │               │ vy <= 0
     接地喪失│               ▼
             ├──────────[FallState]
             │          空中落下
             │               │
             └───────────────┘
                    着地
                     ↓
             IdleState / MoveState（入力有無で分岐）

    ※ JumpState / FallState では追加ジャンプ（2段ジャンプ）が可能
       追加ジャンプ実行 → JumpState へ遷移
```

---

## 物理モデル

### 線形抵抗モデルと終端速度

Unity の `Rigidbody.linearDamping`（drag）は毎物理フレームに以下を適用する：

$$v_{new} = v_{old} \times \max(0,\ 1 - drag \times \Delta t)$$

微小時間の近似として、これは線形抵抗力と等価：

$$F_{drag} = -mass \times drag \times v$$

終端速度 $v_t$ に達したとき合力 = 0（加速しなくなる）：

$$F_{applied} = mass \times drag \times v_t$$

$$\therefore \quad drag = \frac{F_{applied}}{mass \times v_t}$$

ここで $F_{applied}$ = `maxMoveForce`（Inspector 設定）。

**実装**（`MovementMotor.InitializePhysics()`）:

```csharp
float drag = _hub.MaxMoveForce / (_rb.mass * _hub.TerminalHorizontalVelocity);
_rb.linearDamping = drag;
```

Inspector で終端速度を変えるだけで drag が自動再計算される。

---

## PI 制御

### 採用理由

| 制御方式 | 問題点 |
|---------|--------|
| **P のみ** | 坂道・外力で定常偏差が残る |
| **PI** | I 項が誤差を積分して定常偏差をゼロに収束 ✅ |
| **PID** | D 項は速度の数値微分→ノイズが大きく逆効果 |

### 数式

$$error = v_{target} - v_{current}$$

$$\int error\ +=\ error \times \Delta t$$

$$F = K_p \times error + K_i \times \int error$$

$$F = \text{Clamp}(F,\ -F_{max},\ +F_{max})$$

### ゲイン設計

| パラメータ | 値 | 根拠 |
|-----------|-----|------|
| $K_p$ | $F_{max} / v_t$ | フル入力・ゼロ速度時（$error = v_t$）に最大力を出す |
| $K_i$ | Inspector 設定（推奨 0.5〜2.0） | 坂道などで微調整 |

### 注意点

- **積分ワインドアップ対策**：入力ゼロ時に $\int error$ をリセット
- $K_i$ を大きくしすぎると速度が振動する
- 急停止後の再加速に備え、`IdleState.Enter()` で `ResetIntegral()` を呼ぶ

**実装**（`MovementMotor.ApplyMovement()`）:

```csharp
float error = _hub.MoveSpeed - currentSpeed;
_integralError += error * Time.fixedDeltaTime;
float force = _kp * error + _hub.Ki * _integralError;
force = Mathf.Clamp(force, -_hub.MaxMoveForce, _hub.MaxMoveForce);
_rb.AddForce(moveDir * force, ForceMode.Force);
```

---

## 2段ジャンプ

### 仕組み

```
[接地時]
  JumpHandler.ResetExtraJumps()  → extraJumpsRemaining = maxExtraJumps (= 1)

[ジャンプ要求フロー]
  Update:      RequestJump()     → _jumpRequested = true
  FixedUpdate: TryExecuteJump()
                ├── IsGrounded → PerformJump()（回数消費なし）
                ├── extraJumpsRemaining > 0 → extraJumpsRemaining-- → PerformJump()
                └── 残数なし → false（何もしない）
```

### 高さ安定化

落下中に2段ジャンプしても高さが一定になるよう、Y 速度をリセットしてから Impulse を加える：

```csharp
Vector3 vel = _rb.linearVelocity;
vel.y = 0f;
_rb.linearVelocity = vel;
_rb.AddForce(Vector3.up * _hub.JumpForce, ForceMode.Impulse);
```

---

## Inspector 設定値（推奨初期値）

| フィールド | 推奨値 | 説明 |
|-----------|--------|------|
| Move Speed | 5 | 目標移動速度 [m/s] |
| Max Move Force | 50 | PI コントローラの出力上限 [N] |
| Terminal Horizontal Velocity | 8 | 水平終端速度（drag 自動計算に使用） |
| Ki | 1.0 | 積分ゲイン。振動したら下げる |
| Jump Force | 8 | ジャンプ Impulse |
| Max Extra Jumps | 1 | 2段ジャンプ = 1 |
| Ground Check Radius | 0.28 | CapsuleCollider 半径と合わせる |
| Ground Check Distance | 0.05 | 地面との許容距離 |
| Ground Layer | Default | 地面レイヤー |

### Rigidbody 設定

| 項目 | 設定値 |
|------|--------|
| Freeze Rotation | X, Y, Z すべてチェック |
| Collision Detection | Continuous |
| linearDamping | **自動計算（スクリプトが上書き）** |

---

## Unity セットアップ手順

### STEP 1 ── Ground レイヤーを作成する

> **最初に行う。** GroundChecker が地面を認識するために必須。

1. Hierarchy で地面に使う GameObject（Plane / Terrain など）を選択
2. Inspector 右上の **Layer** ドロップダウン → **Add Layer...**
3. User Layer の空き行に `Ground` と入力して保存
4. 地面の GameObject の Layer を `Ground` に変更
   - 「子オブジェクトも変更しますか？」と聞かれたら **Yes, change children** を選択

---

### STEP 2 ── Player GameObject を作成する

1. Hierarchy 上で右クリック → **Create Empty**
2. 名前を `Player` に変更
3. Transform の Position を地面より少し上に設定（例：`Y = 1`）

---

### STEP 3 ── コンポーネントを追加する

Inspector の **Add Component** から以下を追加する。
`PlayerController` を先に追加すると `[RequireComponent]` で依存コンポーネントが自動追加される。

| 追加順 | コンポーネント | 備考 |
| ------ | -------------- | ---- |
| 1 | `PlayerController` | これを追加すると Rigidbody も自動追加 |
| 2 | `CapsuleCollider` | 手動追加が必要 |
| 3 | `PlayerApiHub` | 自動追加済みのはず |
| 4 | `InputReader` | 自動追加済みのはず |
| 5 | `MovementMotor` | 自動追加済みのはず |
| 6 | `GroundChecker` | 自動追加済みのはず |
| 7 | `JumpHandler` | 自動追加済みのはず |

**確認方法**：Inspector に以下の7コンポーネントが並んでいれば OK。

```
✅ Transform
✅ Rigidbody
✅ CapsuleCollider
✅ PlayerController
✅ PlayerApiHub
✅ InputReader
✅ MovementMotor
✅ GroundChecker
✅ JumpHandler
```

---

### STEP 4 ── Rigidbody を設定する

Inspector で `Rigidbody` コンポーネントを開き、以下を設定する。

| 項目 | 設定値 | 理由 |
| ---- | ------ | ---- |
| **Mass** | `1` | drag 計算の基準。変えたら Terminal Velocity も見直す |
| **Linear Damping** | そのまま（0 でよい） | Start() でスクリプトが自動上書きする |
| **Angular Damping** | `0.05` | 空中での無駄な回転を緩やかに止める |
| **Collision Detection** | `Continuous` | 高速移動時のすり抜け防止 |
| **Constraints → Freeze Rotation** | **X / Y / Z すべてチェック** | スクリプトで向きを制御するため物理回転を禁止 |

> **Freeze Rotation を忘れると**、衝突時にプレイヤーが横倒しになる。

---

### STEP 5 ── CapsuleCollider を設定する

| 項目 | 設定値 | 理由 |
| ---- | ------ | ---- |
| **Center** | `(0, 1, 0)` | ピボットが足元なら Y=1 で中心が体中央になる |
| **Radius** | `0.3` | GroundCheckRadius と近い値にする |
| **Height** | `2` | 人型キャラクターの標準的な高さ |
| **Direction** | `Y-Axis` | 縦向きカプセル |

---

### STEP 6 ── PlayerApiHub を設定する

Inspector で `PlayerApiHub` コンポーネントを開き、各フィールドを入力する。

#### 移動設定

| フィールド | 推奨値 | 説明 |
|-----------|--------|------|
| **Move Speed** | `5` | 目標移動速度 [m/s]。体感速度に直結 |
| **Max Move Force** | `50` | PI コントローラの最大出力 [N]。大きいほど素早く加速 |
| **Terminal Horizontal Velocity** | `8` | 水平方向の最大速度 [m/s]。この値から drag を自動計算 |

> `Terminal Horizontal Velocity` ≥ `Move Speed` にすること。
> 小さくすると Move Speed に到達できなくなる。

#### PI 制御設定

| フィールド | 推奨値 | 説明 |
| ---------- | ------ | ---- |
| **Ki** | `1.0` | 積分ゲイン。平地で問題なければ触らなくてよい |

> Ki が大きすぎると速度が振動する。振動が出たら `0.1` まで下げて様子を見る。

#### ジャンプ設定

| フィールド | 推奨値 | 説明 |
|-----------|--------|------|
| **Jump Force** | `8` | ジャンプ時の Impulse 力 |
| **Max Extra Jumps** | `1` | 空中追加ジャンプ回数（2段ジャンプ = 1） |

#### 接地判定

| フィールド | 推奨値 | 説明 |
|-----------|--------|------|
| **Ground Check Radius** | `0.28` | CapsuleCollider の Radius より少し小さくする |
| **Ground Check Distance** | `0.05` | 地面との許容距離。小さいほど判定が厳密 |
| **Ground Layer** | `Ground` | STEP 1 で作成したレイヤーを選択 |

> **Ground Layer の設定方法**：
> フィールドをクリック → レイヤー一覧が出る → `Ground` にチェックを入れる。
> `-1`（Everything）のままにすると自分自身も地面扱いになりバグる。

---

### STEP 7 ── Physics Settings を確認する（任意）

**Edit → Project Settings → Physics** を開く。

| 項目 | 推奨値 | 理由 |
| ---- | ------ | ---- |
| **Gravity** | `(0, -9.81, 0)` | デフォルトのまま。ジャンプ感を変えたいなら `-15` 前後に調整 |
| **Default Contact Offset** | `0.01` | デフォルトのまま |
| **Fixed Timestep** | `0.02`（50Hz） | デフォルトのまま。変えると PI ゲインも再調整が必要 |

---

### STEP 8 ── Input Manager を確認する（旧 Input System 使用時）

**Edit → Project Settings → Input Manager** を開く。
以下の軸が定義されていれば OK（Unity デフォルトで存在する）。

| 軸名 | 対応キー | 使用箇所 |
| ---- | -------- | -------- |
| `Horizontal` | A/D / 左右矢印 | `InputReader.MoveInput.x` |
| `Vertical` | W/S / 上下矢印 | `InputReader.MoveInput.y` |
| `Space` キー | — | `Input.GetKeyDown(KeyCode.Space)` で直接参照 |

---

### STEP 9 ── 動作確認

1. **Play** ボタンを押す
2. Console に以下のログが出ていれば初期化成功：

   ```text
   [MovementMotor] 自動計算 → linearDamping=X.XXXX, Kp=X.XXXX  (maxForce=50, mass=1, vTerminal=8)
   ```

3. **WASD** で移動、**Space** でジャンプを確認
4. Scene ビューで GroundChecker の Gizmo（緑=接地 / 赤=空中）を確認

---

### STEP 10 ── よくあるトラブルと対処

| 症状 | 原因 | 対処 |
| ---- | ---- | ---- |
| プレイヤーが横倒しになる | Rigidbody の Freeze Rotation 未設定 | X/Y/Z すべてチェック |
| 地面にめり込む | Ground Check Radius が大きすぎる | CapsuleCollider の Radius より小さくする |
| 接地判定が取れない | Ground Layer が未設定 | PlayerApiHub の Ground Layer を `Ground` に設定 |
| ジャンプが高すぎる / 低すぎる | Jump Force の調整 | 8→12 で高く、8→5 で低くなる |
| 移動速度が出ない | Terminal Velocity < Move Speed | Terminal Velocity を Move Speed 以上に設定 |
| 速度が振動する | Ki が大きすぎる | Ki を 0.1〜0.5 に下げる |
| Console にエラーログ | コンポーネント不足 | STEP 3 の全コンポーネントが揃っているか確認 |
| Play 直後に吹き飛ぶ | Ground Layer に自分自身が含まれている | Ground Layer を Default/Everything から Ground 専用に変更 |

---

## 拡張例

### ダッシュを追加する

```csharp
// 1. DashCommand.cs（IPlayerCommand 実装）
public class DashCommand : IPlayerCommand
{
    private readonly MovementMotor _motor;
    public DashCommand(MovementMotor motor) { _motor = motor; }
    public void Execute() => _motor.StartDash();
}

// 2. DashState.cs（IPlayerState 実装）
public class DashState : IPlayerState { ... }

// 3. PlayerController.Update() に追加（1行だけ）
if (_hub.InputReader.DashPressed)
    new DashCommand(_hub.MovementMotor).Execute();
```

既存コードへの変更は PlayerController の if ブロック 1つのみ。

### 壁ジャンプを追加する

```
WallChecker.cs       → SphereCast を横方向に飛ばして壁接触を検出
WallJumpHandler.cs   → 壁法線方向に Impulse を加える
PlayerApiHub.cs      → WallChecker / WallJumpHandler の参照を追加
JumpState.cs         → 壁検出時に WallJumpState へ遷移を追加
```

### 攻撃を追加する

```
AttackCommand.cs     → IPlayerCommand 実装
AttackState.cs       → IPlayerState 実装（攻撃中は移動制限など）
PlayerApiHub.cs      → AttackHandler の参照を追加
```

---

## 設計原則との対応

| 原則 | 実装箇所 | 効果 |
|------|---------|------|
| **OCP**（開放/閉鎖） | Command / State は追加するだけ。既存コード修正不要 | 機能追加コストが低い |
| **SRP**（単一責務） | 入力・物理・状態・判定を完全分離 | バグ箇所が即座に特定できる |
| **依存集約**（ApiHub） | クラス間直接参照ゼロ。ApiHub 経由のみ | 依存関係が見渡せる |
| **Update/FixedUpdate 分離** | 入力はフラグで保持し FixedUpdate で消費 | 入力取りこぼしなし・物理安定 |

---

## 既知の制限と改善案

| 制限 | 改善案 |
|------|--------|
| カメラ方向を考慮しない移動 | `Camera.main.transform` から移動方向を変換する |
| linearDamping が全軸に適用される | 垂直方向の drag は別途制御（angular drag で対処） |
| 旧 Input Manager 使用 | `InputReader` を差し替えるだけで新 Input System に移行可能 |
| ジャンプの入力バッファなし | `RequestJump()` にバッファ時間を持たせると操作感が向上 |
