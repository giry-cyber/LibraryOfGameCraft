# BTSystem レポート

## 概要

汎用ビヘイビアツリー（Behavior Tree）AI フレームワーク。  
ノードを ScriptableObject として定義し、BTGraph アセットにまとめることで、実装者がツリー構造を Inspector またはビジュアルエディタで組み立てられる。  
CapabilityComponent（CharacterMotor・PerceptionSensor）を介して Unity API と分離しており、ノードは純粋なロジックのみを担当する。

---

## 設計

### クラス構成（コア）

| クラス | 種別 | 責務 |
|--------|------|------|
| `BTNode` | abstract ScriptableObject | 全ノードの基底。EditorPosition を持つ |
| `BTStatus` | enum | Success / Failure / Running |
| `BTContext` | Plain C# | ノードへ渡すコンテキスト（Owner, Blackboard, Get\<T\>） |
| `BTBlackboard` | Plain C# | ランタイム状態のキー・バリューストア |
| `BTKeys` | static class | Blackboard キー文字列の定数置き場 |
| `BTGraph` | ScriptableObject | RootNode + AllNodes（サブアセット一覧）を管理 |
| `BTRunner` | MonoBehaviour | BTGraph を毎フレーム Tick。Blackboard を外部公開 |

### クラス構成（合成ノード）

| クラス | 挙動 |
|--------|------|
| `BTSequence` | AND：全子が Success なら Success（non-memory、毎フレーム先頭から評価） |
| `BTSelector` | OR：最初に Success/Running を返した子を採用（non-memory） |
| `BTDecorator` | 子1つを持つ装飾ノードの基底 |
| `BTInverter` | 子の Success/Failure を反転 |
| `BTCooldown` | 子完了後 N 秒間 Failure を返す（Blackboard でタイマー管理） |
| `BTRepeater` | 子を N 回繰り返す（0 = 無限） |
| `BTAction` | 行動ノード基底。OnEnter / OnTick / OnExit ライフサイクル |
| `BTCondition` | 条件ノード基底。Check() → Success/Failure |

### 提供ノード（Actions）

| クラス | 挙動 |
|--------|------|
| `WanderAction` | HomePosition 中心にランダム徘徊 |
| `MoveToTargetAction` | Blackboard["target"] を追跡 |
| `AttackAction` | 停止してターゲット方向を向き AttackCapability.TriggerAttack() を発火。BTCooldown と組み合わせて使う |

### 提供ノード（Conditions）

| クラス | 評価 |
|--------|------|
| `IsTargetDetectedCondition` | target != null |
| `IsTargetLostCondition` | target == null または指定距離以上 |
| `IsInAttackRangeCondition` | target が指定距離以内 |

### CapabilityComponent

| クラス | 責務 |
|--------|------|
| `CharacterMotor` | NavMeshAgent + CharacterController ラッパー。MoveTo / Stop / FaceToward / HasArrived |
| `PerceptionSensor` | 視野（角度+Raycast）と聴覚（半径 OverlapSphere）でターゲットを自動検知し Blackboard に書き込む |
| `AttackCapability` | 攻撃処理の委譲先。OnAttackTriggered UnityEvent に外部からダメージ処理を配線する |

### ビジュアルエディタ（Editor 専用）

| クラス | 責務 |
|--------|------|
| `BTGraphEditorWindow` | EditorWindow 本体。BTGraph ダブルクリックで起動 |
| `BTGraphView` | GraphView 継承。ノード配置・エッジ描画・変更検知・自動整列 |
| `BTNodeView` | Node 継承。上=入力ポート、下=出力ポート（Composite は N 個、Decorator は 1 個） |

---

## 実装メモ

### Non-memory Composite の設計判断

Sequence/Selector は現在の子インデックスを記憶しない。  
毎フレーム先頭から評価し直すことで、前段の条件ノードが常に再チェックされる（リアクティブ BT）。  
これにより「敵が視野から外れた瞬間に追跡を中断」が自然に実現する。

### BTAction のステート管理

ScriptableObject は複数の BTRunner で共有されるため、ノード自身にランタイム状態を持てない。  
`__act_{InstanceID}` キーを Blackboard に書くことで「前フレームにこのアクションが実行中だったか」を判定し、  
OnEnter/OnExit を正確に 1 回だけ呼ぶ。

### CharacterMotor.MoveTo の NavMesh スナップ

`NavMesh.SamplePosition(destination, out hit, 5f, NavMesh.AllAreas)` で目標座標を NavMesh 上に強制スナップしてから `SetDestination` を呼ぶ。  
これを省略すると Inspector で設定した座標が NavMesh 外の場合に `hasPath=False` になり移動しない。

### ビジュアルエディタのデータ同期

エッジ追加 → `BTComposite.Editor_SetChildAt(index, node)` / `BTDecorator.Editor_SetChild(node)` を即時呼び出し。  
エッジ削除 → 同インデックスのスロットを null に設定。  
ノード削除 → `AssetDatabase.RemoveObjectFromAsset` でサブアセットを除去。  
全変更後に `EditorUtility.SetDirty` → Save ボタンで `AssetDatabase.SaveAssets()`。

---

## 既知の制限・TODO

- [ ] BTCooldown / BTRepeater のビジュアルエディタでの設定値表示（InspectorView 統合）
- [ ] 実行時ノード状態のデバッグ可視化（Running=黄色枠など）
- [ ] より多くのビルトイン Action / Condition ノード
- [ ] WorldStreaming との統合（シーンロード時のスポーン/デスポーン）

---

## 変更履歴

| 日付 | 内容 |
|------|------|
| 2026-05-22 | Phase 1: コア基盤（BTNode / BTContext / BTBlackboard / BTRunner / BTGraph / BTSequence / BTSelector）実装 |
| 2026-05-22 | Phase 2: CapabilityComponent（CharacterMotor / PerceptionSensor）実装。BTAction.OnEnter/OnExit ライフサイクル追加 |
| 2026-05-22 | Phase 3: WanderAction / MoveToTargetAction / IsTargetDetectedCondition / IsTargetLostCondition 実装 |
| 2026-05-22 | Phase 4A: BTDecorator 基底 + BTInverter / BTCooldown / BTRepeater 実装 |
| 2026-05-26 | Phase 4D: ビジュアルエディタ実装（BTGraphEditorWindow / BTGraphView / BTNodeView）。[OnOpenAsset] で BTGraph ダブルクリック起動、反射ベースノード追加、BFS 自動整列 |
| 2026-05-27 | AttackAction / IsInAttackRangeCondition / AttackCapability を追加 |
| 2026-05-27 | ランタイムデバッグ可視化を追加。BTNode.Tick() を非 abstract ラッパーに変更し EditorLastStatus / EditorLastTickFrame を記録。BTGraphView が 50ms ポーリングでノードカラーを更新（Running=黄、Success=緑、Failure=赤）。EditMode 復帰時に全ノードリセット |
