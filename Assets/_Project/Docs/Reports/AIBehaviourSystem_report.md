# AIBehaviourSystem レポート

## 概要

NPC・敵キャラクター共通の行動制御フレームワーク。  
ノードベースのステートマシンで行動を定義し、ScriptableObject としてアセット化することで、インスペクタから組み合わせを変えるだけで多様なキャラクター挙動を実現する。

NavMesh（事前Bake）を使わず、ウェイポイントと CharacterController による移動を採用しているため、AdditiveLoad 形式のプロシージャル地形とも相性がよい。

---

## 設計

### クラス構成

| クラス | 種別 | 責務 |
|--------|------|------|
| `AINode` | abstract ScriptableObject | 行動ノードの基底。OnEnter / Tick / OnExit を定義 |
| `AICondition` | abstract ScriptableObject | 遷移条件の基底。Evaluate() で true/false を返す |
| `AITransition` | Serializable | 条件と遷移先ノードのペア |
| `AIBlackboard` | Plain C# class | ノード間でランタイム状態を共有するキー・バリューストア |
| `AIBehaviourGraph` | ScriptableObject | ノードグラフの定義（StartNode を持つ） |
| `AIController` | MonoBehaviour | グラフを実行し、移動・回転・重力を適用する |

### 提供ノード（初期実装）

| クラス | 挙動 |
|--------|------|
| `IdleNode` | その場で静止 |
| `WanderNode` | HomePosition を中心にランダム徘徊 |

### 提供条件（初期実装）

| クラス | 評価内容 |
|--------|----------|
| `TimerCondition` | 現在ノードの経過時間が指定秒数を超えたら true |

### 依存関係

```
AIBehaviourGraph (ScriptableObject)
    └── AINode[] (ScriptableObject)
            └── AITransition[]
                    ├── AICondition (ScriptableObject)
                    └── AINode (次ノード)

AIController (MonoBehaviour)
    ├── AIBehaviourGraph  ← インスペクタでアサイン
    ├── AIBlackboard      ← 実行時に生成、ノードに渡す
    └── CharacterController (RequireComponent)
```

---

## 実装メモ

### ノードをステートレスに保つ理由

`AINode` は ScriptableObject なので、同じアセットを複数の AIController が共有する。  
ノード自身にランタイム状態（目標座標など）を持たせると全インスタンスで状態が混在する。  
そのため、ノードごとの状態はすべて `AIBlackboard` に書き込み、ノードは純粋なロジックのみを持つ設計にしている。

### WanderNode のターゲット選択

`HomePosition`（生成位置）を原点として半径 `_wanderRadius` 内のランダム点を選ぶ。  
到達判定は XZ 平面のみで行い、段差のある地形でも誤判定しない。

### 移動・重力

`AIController.ApplyMovement()` がノードの `DesiredMoveDirection` を読んで CharacterController に適用する。  
ノードは方向ベクトルを書くだけでよく、物理処理を意識しなくてよい。

---

## 既知の制限・TODO

- [ ] PatrolNode（ウェイポイント巡回）未実装
- [ ] ChaseNode（プレイヤー追跡）未実装
- [ ] AttackNode 未実装
- [ ] AlertNode（発見状態）未実装
- [ ] ReturnNode（帰還行動）未実装
- [ ] WorldStreaming との統合（シーンロード時のスポーン/デスポーン）未実装
- [ ] アニメーション連携未実装
- [ ] 障害物回避未実装（現状は直進のみ）

---

## 変更履歴

| 日付 | 内容 |
|------|------|
| 2026-05-08 | 初版作成（コア基盤 + IdleNode + WanderNode + TimerCondition） |
| 2026-05-11 | ビジュアルグラフエディタを追加（AIGraphEditorWindow / AIGraphView / AINodeView）。AINode に Position フィールド、AIBehaviourGraph に Nodes リストと SetStartNode を追加 |
| 2026-05-11 | NavMesh基盤を追加。WorldNavMeshManager（WorldStreaming連携・シーンロード時にランタイムBake）を実装 |
| 2026-05-11 | AIController を NavMesh ベースに移行。NavMeshAgent（updatePosition/Rotation=false）追加、SetDestination / StopMovement / HasArrived を実装。WanderNode・IdleNode を新 API に対応 |
