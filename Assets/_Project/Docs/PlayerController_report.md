# PlayerController レポート

## 概要

3Dアクションゲーム向けのプレイヤーキャラクター制御システム。
凹凸地形での安定した移動・斜面挙動・小段差自動登り・壁スライド・再現可能な入力ログ・拡張可能な行動システムを提供する。

---

## 設計

### クラス構成

| クラス / インターフェース | 責務 |
|---|---|
| `PlayerController` | エントリーポイント。各モジュールを組み立て更新順を制御 |
| `PlayerInputReader` | Unity Input System からフレーム単位の InputSnapshot を生成 |
| `InputRecorder` | 入力履歴を List で保存（リプレイ・デバッグ再現用） |
| `CharacterBrain` | InputSnapshot を解釈し Command を発行 |
| `CharacterStateMachine` | ICharacterState の生存管理と RequestTransition / ForceTransition |
| `ICharacterState` | State パターン基底インターフェース（Enter/Exit/Tick/CanTransitionTo） |
| `ICharacterMotor` | 移動実行層の抽象化（将来の自前モーター移行に備える） |
| `CharacterControllerMotor` | Unity CharacterController を基盤とした ICharacterMotor 実装 |
| `IMovementStrategy` | 移動計算アルゴリズムの Strategy インターフェース |
| `GroundMovementStrategy` | 地上移動（加速・減速あり） |
| `AirMovementStrategy` | 空中移動（AirControlFactor=0.4 で制限） |
| `PlayerAnimatorAdapter` | ロジック状態を Animator パラメータへ変換 |
| `PlayerEventSystem` | OnJump / OnLand / OnAttackStart / OnStateChanged をイベントで通知 |
| `MovementTuning` | 移動速度・斜面・段差・接地判定パラメータ（ScriptableObject） |
| `JumpSettings` | ジャンプ力・MinLandingTime（ScriptableObject） |
| `GravitySettings` | 重力スケール・最大落下速度・落下倍率・接地押し付け速度（ScriptableObject） |

### 状態一覧

| State | 内容 | 遷移許可 |
|---|---|---|
| Idle | 待機 | 全遷移許可 |
| Move | 移動 | 全遷移許可 |
| Jump | ジャンプ（上昇中） | Fall / Stun のみ |
| Fall | 落下 | Landing / Stun のみ |
| Landing | 着地（minLandingTime 待機） | Jump / Move / Idle / Stun |
| Attack | 攻撃（時間制） | Stun のみ（canJumpCancel=true なら Jump も可） |
| Dodge | 回避（時間制） | Stun のみ |
| Stun | 行動不能（時間制） | なし（ForceTransition で解除） |

### 更新フロー

```
PlayerController.Update()
  │
  ├─ PlayerInputReader.ReadSnapshot()        → InputSnapshot
  ├─ InputRecorder.Record(snapshot)
  ├─ CharacterBrain.Process(snapshot)        → Command → StateMachine.RequestTransition()
  ├─ CharacterStateMachine.Tick()            → ICharacterState.Tick()
  ├─ IMovementStrategy.ComputeVelocity()     → Motor.HorizontalVelocity に設定
  ├─ CharacterControllerMotor.Tick()         → 重力・GroundSnap・CharacterController.Move
  ├─ RotateTowardsMoveDirection()
  └─ PlayerAnimatorAdapter.Update()
```

### Animator パラメータ

| パラメータ | 型 | 内容 |
|---|---|---|
| Grounded | Bool | 接地しているか |
| Speed | Float | 水平速度の大きさ |
| VerticalSpeed | Float | 垂直速度 |
| ActionState | Int | CharacterStateType の int 値 |

---

## 実装メモ

### CharacterController の座標系
Unity の CharacterController で `transform.position` は GameObject の原点。
カプセル底面の球体中心は `transform.position + center - Vector3.up * (height/2 - radius)` で求め、SphereCast の起点とする。

### 接地判定の設計
`SphereCast(radius * 0.9f)` で壁面への誤検出を抑制。
`isGrounded` 条件: `dot(normal, up) >= MinGroundDotProduct && distance <= GroundedThreshold`

### 重力
```
gravityStrength = Physics.gravity.y * GravityScale   // 通常
gravityStrength *= FallMultiplier                     // 落下中のみ
```
接地かつ落下中は `GravitySettings.GroundStickSpeed` の負値で押し付けて浮き防止。

### 斜面処理
- ≤ MaxSlopeAngle (45°): 通常移動
- 45° ～ SlideAngle (55°): 移動制限（Brain からの移動は受け付けるが速度は制限される）
- > SlideAngle (55°): 斜面方向に強制スライド

### Landing 終了条件（仕様 23.4）
アニメーション終了ではなく `minLandingTime` 経過 + 接地確認 + 入力条件で遷移先を決定。

### Attack の Jump キャンセル（仕様 23.3）
`AttackState._canJumpCancel` フラグで制御。将来的に攻撃データ ScriptableObject から注入する想定。

---

## セットアップ手順

1. Player GameObject に `CharacterController` と `PlayerController` を追加
2. CharacterController の設定:
   - Height: 1.8, Radius: 0.3, Center: (0, 0.9, 0), Step Offset: 0.4
3. `Assets/Create/LibraryOfGamecraft/Player/` から以下を作成してアサイン:
   - `MovementTuning`
   - `JumpSettings`
   - `GravitySettings`
4. `Animator` と `CameraTransform` を Inspector でアサイン
5. Animator Controller に以下のパラメータを追加:
   - `Grounded` (Bool), `Speed` (Float), `VerticalSpeed` (Float), `ActionState` (Int)

---

## 既知の制限・TODO

- [ ] DodgeState: 回避方向へのダッシュ速度付与が未実装（現在は移動制限のみ）
- [ ] AttackState: `_attackDuration` と `_canJumpCancel` を AttackData ScriptableObject から注入する拡張が未実装
- [ ] StepClimb: CharacterController の StepOffset に委ねており、自前の細かい制御は未実装
- [ ] SwimmingMovement / ClimbMovement / LockOnMovement Strategy は未実装
- [ ] AI/リプレイ用の InputSnapshot 注入経路が未実装（PlayerInputReader を差し替える設計で対応予定）
- [ ] IK は別システムの責務（仕様 22 参照）

---

## 変更履歴

| 日付 | 内容 |
| --- | --- |
| 2026-03-27 | 初版作成。CharacterController ベースの Kinematic 実装 |
| 2026-03-27 | GravitySettings に GroundStickSpeed を追加（-2f のマジックナンバーを解消） |
