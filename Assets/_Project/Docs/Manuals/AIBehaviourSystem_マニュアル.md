# AIBehaviourSystem マニュアル

## 概要

NPC・敵キャラクター共通の行動制御システムです。  
ScriptableObject でノードとグラフを定義し、`AIController` コンポーネントに割り当てるだけで動作します。

---

## セットアップ手順

### 1. ノードアセットを作成する

Project ウィンドウで右クリック →「Create」から各ノード・条件を作成します。

```
LibraryOfGamecraft/AI/
├── BehaviourGraph      ← グラフ本体
├── Nodes/
│   ├── IdleNode        ← 静止
│   └── WanderNode      ← 徘徊
└── Conditions/
    └── TimerCondition  ← 時間経過で遷移
```

### 2. ノードに遷移を設定する

各ノードのインスペクタに **Transitions** リストがあります。

| フィールド | 内容 |
|-----------|------|
| `Condition` | 遷移するかどうかを判定する AICondition アセット |
| `Next Node` | 遷移先の AINode アセット |

- `Condition` が **null** の場合は **無条件に遷移** します。
- リストは上から順に評価され、最初に true になった遷移が使われます。

**例：Wander → Idle → Wander のループ**

```
WanderNode
  Transitions[0]
    Condition: TimerCondition (duration: 5)   ← 5 秒後
    NextNode: IdleNode

IdleNode
  Transitions[0]
    Condition: TimerCondition (duration: 2)   ← 2 秒後
    NextNode: WanderNode
```

### 3. BehaviourGraph を作成する

`LibraryOfGamecraft/AI/BehaviourGraph` を右クリックから作成し、  
インスペクタの `Start Node` に最初に実行したいノードをアサインします。

### 4. AIController をプレハブに追加する

キャラクターの GameObject に以下を追加します。

| コンポーネント | 備考 |
|--------------|------|
| `CharacterController` | 自動追加（RequireComponent） |
| `AIController` | `Graph` に手順 3 のアセットをアサイン |

`AIController` のインスペクタパラメータ：

| パラメータ | デフォルト値 | 説明 |
|-----------|-------------|------|
| `Graph` | — | 実行する AIBehaviourGraph |
| `Move Speed` | 3.0 | 移動速度（m/s） |
| `Rotation Speed` | 10.0 | 回転の滑らかさ |

---

## 新しいノードを追加する

### AINode を継承する

```csharp
[CreateAssetMenu(menuName = "LibraryOfGamecraft/AI/Nodes/MyNode")]
public class MyNode : AINode
{
    public override void OnEnter(AIController context)
    {
        // ノード開始時の処理
    }

    public override void Tick(AIController context)
    {
        // 毎フレームの処理
        // context.DesiredMoveDirection = ... で移動方向を指定
    }

    public override void OnExit(AIController context)
    {
        // ノード終了時の後処理
    }
}
```

### ランタイム状態の保持

ノードは ScriptableObject（共有インスタンス）なので、フィールドに状態を持たないでください。  
代わりに `AIBlackboard` を使います。

```csharp
// 書き込み
context.Blackboard.Set("my_key", someValue);

// 読み出し（デフォルト値を第 2 引数に指定）
var value = context.Blackboard.Get<float>("my_key", 0f);
```

キー名の衝突を避けるため、ノード名をプレフィックスにすることを推奨します（例：`"mynode_target"`）。

---

## 新しい条件を追加する

```csharp
[CreateAssetMenu(menuName = "LibraryOfGamecraft/AI/Conditions/MyCondition")]
public class MyCondition : AICondition
{
    [SerializeField] private float _threshold = 5f;

    public override bool Evaluate(AIController context)
    {
        // true を返すと遷移が発火する
        return context.ElapsedTimeInState >= _threshold;
    }
}
```

---

## AIController で利用できるコンテキスト情報

| プロパティ | 型 | 内容 |
|-----------|-----|------|
| `SelfTransform` | `Transform` | キャラクター自身の Transform |
| `HomePosition` | `Vector3` | 起動時の初期位置（行動範囲の原点として利用） |
| `ElapsedTimeInState` | `float` | 現在ノードに入ってからの経過秒数 |
| `DesiredMoveDirection` | `Vector3` | ノードが書き込む移動方向（XZ 平面、正規化推奨） |
| `Blackboard` | `AIBlackboard` | ノード間共有のキー・バリューストア |

---

## 既存ノードリファレンス

### IdleNode

その場で停止するだけのノード。他ノードへの中継点や休憩状態に使います。

インスペクタパラメータ：なし

---

### WanderNode

`HomePosition` を中心にランダムな点を選んで移動し続けます。

| パラメータ | デフォルト値 | 説明 |
|-----------|-------------|------|
| `Wander Radius` | 5.0 | 徘徊半径（HomePosition からの最大距離） |
| `Arrival Distance` | 0.5 | 目標に到着したとみなす距離 |

---

### TimerCondition

現在ノードへ入ってからの経過時間で遷移を発火します。

| パラメータ | デフォルト値 | 説明 |
|-----------|-------------|------|
| `Duration` | 3.0 | 遷移までの秒数 |
