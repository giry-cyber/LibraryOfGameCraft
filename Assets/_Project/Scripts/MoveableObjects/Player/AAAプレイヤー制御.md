# 3Dアクションゲーム

# キャラクターコントローラ仕様書（Unity）

Version 1.0

---

# 1. 目的

本仕様は、Unityを用いた3Dアクションゲームにおけるプレイヤーキャラクター制御システムの設計を定義する。

本システムは以下の目標を持つ。

* 凹凸地形で安定した移動
* 斜面の自然な挙動
* 小段差の自動登り
* 壁衝突時の滑り
* 再現可能な入力ログ
* 拡張可能な行動システム
* アニメーションとの柔軟な連携

---

# 2. 設計方針

本システムは以下の原則で設計する。

### 2.1 ロジックと見た目の分離

```text
Game Logic ≠ Animation
```

キャラクターのゲームロジック状態とアニメーション状態は分離する。

---

### 2.2 移動ロジックと物理の分離

キャラクター移動は Rigidbody 物理ではなく
**Kinematic Character Controller** として実装する。

移動実行基盤には **Unity標準 CharacterController** を採用する。
ただし Ground Detection・Ground Snap・Slope判定・状態管理・入力記録・Animator連携は自前実装とする。
将来的な完全自前モーター移行に備え、移動実行層は `ICharacterMotor` インターフェースで抽象化する。

---

### 2.3 単一責任

| モジュール    | 責務    |
| -------- | ----- |
| Input    | 入力取得  |
| Recorder | 入力記録  |
| Brain    | 入力解釈  |
| State    | 行動管理  |
| Motor    | 移動計算  |
| Animator | 見た目制御 |

---

# 3. システム構造

キャラクター制御は以下の層構造で構成する。

```
Input Source
↓
Input Recorder
↓
Character Brain
↓
State Machine
↓
Character Motor
↓
Animator Adapter
↓
Event System
```

---

# 4. 使用設計パターン

本システムでは以下の設計パターンを使用する。

| パターン        | 用途       |
| ----------- | -------- |
| Command     | 入力イベント   |
| State       | 行動状態     |
| Strategy    | 移動アルゴリズム |
| Observer    | イベント通知   |
| Data Driven | パラメータ管理  |

---

# 5. キャラクター物理構造

キャラクター衝突形状は以下とする。

```
Capsule Collider
```

理由

* 壁に引っかからない
* 段差処理が安定
* 斜面移動が自然

---

## 推奨サイズ

```
Height = 1.8
Radius = 0.3
Center = (0, 0.9, 0)
```

---

# 6. 座標系仕様

## 6.1 Transform

キャラクターTransformは**カプセル中心**を基準とする。

```
Transform.position = CapsuleCenter
```

---

## 6.2 FootPoint

地面判定は足元基準で行う。

```
FootPoint = Transform.position − CapsuleHeight / 2
```

---

# 7. Input 設計

入力はフレーム単位で記録する。

```csharp
struct InputSnapshot
{
    Vector2 move;
    Vector2 look;

    bool jumpPressed;
    bool attackPressed;
    bool dashPressed;
}
```

---

## Input Recorder

入力履歴は以下に保存する。

```
List<InputSnapshot> inputHistory
```

用途

* リプレイ
* デバッグ再現
* AI入力
* ネットワーク同期

---

## Command

単発入力イベントは Command として扱う。

例

```
JumpCommand
AttackCommand
DashCommand
InteractCommand
```

---

# 8. Character Brain

Brain は入力を解釈しキャラクターの意図を生成する。

例

| 入力     | 意図   |
| ------ | ---- |
| Move   | 移動   |
| Jump   | ジャンプ |
| Attack | 攻撃   |

Brain は State Machine へ指示を送る。

---

# 9. State Machine

キャラクター行動は State パターンで管理する。

```
interface ICharacterState
{
    Enter()
    Exit()
    Tick()
}
```

State は入力デバイスや生入力に依存しない。
入力解釈は Brain が担い、State は以下を保持する。

* 遷移を受け付けるか（拘束条件）
* どの Movement Strategy を使うか
* どの時間だけ拘束するか

---

## 状態一覧

| State   | 内容   |
| ------- | ---- |
| Idle    | 待機   |
| Move    | 移動   |
| Jump    | ジャンプ |
| Fall    | 落下   |
| Landing | 着地   |
| Attack  | 攻撃   |
| Dodge   | 回避   |
| Stun    | 行動不能 |

---

# 10. Movement Motor

Motor はキャラクターの実際の移動を計算する。

責務

* Ground Detection
* Slope Handling
* Step Climb
* Ground Snap
* Gravity
* Wall Sliding

---

# 11. Ground Detection

接地判定は **SphereCast** を使用する。

理由

* 安定した接地判定
* 段差対応
* カプセル底面に近い判定

---

## 判定基準

```
FootPoint
↓
SphereCast
↓
GroundHit
```

---

## 取得情報

```
isGrounded
groundNormal
groundPoint
groundDistance
slopeAngle
groundCollider
```

---

## 接地条件

接地とみなす条件

```
distance <= groundedThreshold
AND
dot(normal, up) >= minGroundDot
```

---

# 12. Slope Handling

斜面角度は以下で計算する。

```
slopeAngle = Angle(groundNormal, up)
```

---

## パラメータ

| パラメータ         | 値   |
| ------------- | --- |
| MaxSlopeAngle | 45° |
| SlideAngle    | 55° |

---

## 挙動

```
<45° → 登れる
45-55° → 移動制限
>55° → 滑る
```

---

# 13. Step Climb

小段差は自動で登る。

```
StepHeight = 0.4m
```

アルゴリズム

```
前方衝突
↓
高さ差チェック
↓
高さ < StepHeight
↓
位置を上に補正
```

---

# 14. Ground Snap

下り坂でキャラクターが浮くことを防ぐ。

```
RaycastDown
↓
距離取得
↓
地面へ補正
```

---

# 15. Wall Sliding

壁衝突時は速度を壁面に投影する。

```
velocity = ProjectOnPlane(velocity, wallNormal)
```

これにより壁沿い移動を実現する。

---

# 16. Movement Strategy

移動計算は Strategy パターンで切り替える。

```
interface IMovementStrategy
{
    ComputeVelocity()
}
```

---

## Strategy例

| Strategy         | 用途    |
| ---------------- | ----- |
| GroundMovement   | 地上    |
| AirMovement      | 空中    |
| SwimmingMovement | 水中    |
| ClimbMovement    | 梯子    |
| LockOnMovement   | ロックオン |

---

# 17. Animator Adapter

ロジック状態を Animator パラメータへ変換する。

```
animator.SetBool("Grounded")
animator.SetFloat("Speed")
animator.SetFloat("VerticalSpeed")
animator.SetInt("ActionState")
```

---

# 18. Event System

キャラクター状態変化はイベントとして通知する。

例

| Event          | 内容   |
| -------------- | ---- |
| OnJump         | ジャンプ |
| OnLand         | 着地   |
| OnAttackStart  | 攻撃   |
| OnStateChanged | 状態変更 |

用途

* SE
* VFX
* UI
* カメラ

---

# 19. Data Driven 設計

調整値は ScriptableObject で管理する。

例

```
MovementTuning
JumpSettings
GravitySettings
```

---

# 20. 拡張性

新しい行動追加手順

1. 新しい State 作成
2. 必要なら Strategy 作成
3. Animator パラメータ追加

---

# 21. テスト項目

| テスト  | 内容         |
| ---- | ---------- |
| 平地移動 | 正常         |
| 小段差  | 登れる        |
| 高段差  | 登れない       |
| 斜面   | 正常移動       |
| 急斜面  | 登れない       |
| 壁衝突  | スライド       |
| 下り坂  | GroundSnap |

---

# 22. IKの扱い

本仕様では IK は **別システムの責務**とする。

IKは以下の目的で別システムとして実装する。

* 足接地補正
* 骨盤補正
* 体姿勢補正

キャラクターコントローラは IK に依存しない。

---

# 23. 実装方式の確定

## 23.1 キャラクターモーター実装方式

移動実行基盤に **Unity標準 CharacterController** を採用する。

CharacterController の責務

* カプセル衝突
* 基本移動（`CharacterController.Move`）
* 壁との衝突解決の補助

自前実装とする責務

* Ground Detection
* Ground Snap
* Slope 判定
* 状態管理
* 入力記録
* Animator 連携

---

## 23.2 入力解釈方式

**Brain主導方式** を採用する。

* Brain は `InputSnapshot` を解釈し、行動意図（遷移要求）を StateMachine へ送る
* State は入力デバイスや生入力に依存しない
* 人間入力・AI入力・リプレイ入力は同一の `InputSnapshot` 形式に変換されてから Brain に渡る

---

## 23.3 Attack状態中のJump可否

Attack状態中は原則として Jump **不可** とする。

ただし攻撃データに `canJumpCancel` フラグを持たせ、これが有効な攻撃に限り Jump への遷移を許可する。

```
通常攻撃 → canJumpCancel = false → Jump 不可
特定攻撃 → canJumpCancel = true  → Jump 可
```

---

## 23.4 Landing状態の終了条件

Landing状態はアニメーション終了を直接の終了条件としない。

終了条件

1. `minLandingTime` が経過していること（推奨初期値: 0.08〜0.12秒）
2. 現在の接地状態が有効であること
3. 入力条件または速度条件に応じて次状態へ遷移可能であること

```
Landing開始
↓
minLandingTime 経過
↓
移動入力あり → Move
ジャンプ入力あり → Jump
それ以外 → Idle
```

---

## 23.5 Stun状態の終了条件

Stun状態は基本的に時間経過で終了する。

* `remainingStunTime <= 0` で解除
* 外部から `AddStun(float time)` により延長可能
* 外部から `ForceRecover()` により即時解除可能
* より高優先度の状態遷移要求（死亡など）が来た場合は中断される

---

# 仕様まとめ

本キャラクターシステムは以下の構造を採用する。

```
Input
↓
Command / Snapshot
↓
Brain
↓
State Machine
↓
Character Motor
↓
Animator Adapter
↓
Event System
```

この構造により以下を実現する。

* 安定したキャラクター移動
* 再現可能な入力ログ
* 拡張可能な行動設計
* アニメーションとの柔軟な連携

---
