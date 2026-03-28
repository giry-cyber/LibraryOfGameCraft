# PlayerDebugger レポート

## 概要

プレイヤーキャラクターの接地判定とステート遷移をリアルタイムで可視化するデバッグコンポーネント。
接地パラメータの設定ミスの発見や、ステート遷移の動作確認を目的とする。
`PlayerController` と同一 GameObject にアタッチして使用する。

---

## 設計

### クラス構成

| クラス | 責務 |
| --- | --- |
| `PlayerDebugger` | 接地 SphereCast の Gizmos 描画と画面オーバーレイ表示 |

### 依存関係・使い方

`PlayerController` と `CharacterController` を `GetComponent` で取得する。
`MovementTuning` は SphereCast パラメータ（GroundCheckDistance・GroundedThreshold）の参照に使用するため、Inspector で同一 ScriptableObject をアサインする。

```csharp
// アタッチするだけで動作する。Inspector で以下をアサイン
[SerializeField] private MovementTuning _movementTuning;
```

`_showDebug` を false にするとすべての表示が消える。

---

## 表示内容

### 画面オーバーレイ（OnGUI）

| 項目 | 内容 |
| --- | --- |
| `State` | 現在のステート名。ステートごとに色分け |
| `Grounded` | 接地判定結果。true=緑 / false=赤 |
| `VertVel` | 垂直速度 (m/s) |
| `HorizSpd` | 水平速度の大きさ (m/s) |
| `Slope` | 斜面角度（接地中のみ表示） |
| `GroundDist` | 地面までの距離（接地中のみ表示） |

### Gizmos（シーンビュー・プレイ中共通）

| 色 | 内容 |
| --- | --- |
| 緑 / 赤の球 | SphereCast 起点。接地=緑、空中=赤 |
| 黄のライン | GroundCheckDistance の検出範囲 |
| 黄の輪郭球 | SphereCast 終端位置 |
| シアンのライン | GroundedThreshold（この距離以内で接地と判定） |
| 水色の点 | 地面ヒット点（接地中のみ） |
| 青のライン | 地面法線（接地中のみ） |

---

## 実装メモ

`PlayerController.Motor` と `PlayerController.CurrentState` を通じてロジック情報を取得するため、
`PlayerController` の内部実装に依存しない。`ICharacterMotor` のインターフェース越しに参照している。

---

## 既知の制限・TODO

- [ ] `_showDebug` のランタイム切り替えをキーバインドで行えるようにする
- [ ] GroundSnap の Raycast 範囲も可視化する

---

## 変更履歴

| 日付 | 内容 |
| --- | --- |
| 2026-03-27 | 初版作成 |
