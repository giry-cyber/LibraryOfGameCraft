# 会話システム レポート

## 概要

ゲーム内のNPC会話、イベント会話、分岐会話、フラグ連動会話を制御する汎用会話システム。  
仕様書 `Specs/会話システム仕様書.md` v1.0 に基づく初版実装。

## 設計

### クラス構成

| クラス | 責務 |
|--------|------|
| `DialogueManager` | 会話全体の開始・進行・終了・状態遷移を管理するMonoBehaviour |
| `DialogueRunner` | 現在ノードの解釈、次ノード決定、分岐評価（MonoBehaviour非依存） |
| `DialogueUIController` | UI表示・文字送り・選択肢表示・ログ表示 |
| `DialogueConditionEvaluator` | フラグ・既読状態等の条件判定 |
| `DialogueEventExecutor` | 会話中イベントを外部システムへ橋渡し |
| `DialogueHistoryService` | 既読管理・ログ管理 |
| `DialogueFlagService` | BoolフラグとIntフラグの読み書き |
| `DialogueDataRepository` | 会話セット解決・ノード取得 |
| `NpcDialogueTrigger` | NPC会話のトリガーコンポーネント |

### データ構造

| クラス | 説明 |
|--------|------|
| `DialogueSet` | ScriptableObject。1つの会話グラフ単位 |
| `DialogueFlagDatabase` | ScriptableObject。フラグ定義とデフォルト値 |
| `MessageNode` | テキスト表示ノード |
| `ChoiceNode` | 選択肢表示ノード |
| `BranchNode` | 条件分岐ノード |
| `EventNode` | イベント実行ノード |
| `SequenceNode` | 外部演出シーケンス実行ノード |
| `JumpNode` | 別セット・別ノードへのジャンプノード |
| `EndNode` | 会話終了ノード |

### 状態遷移

```
Idle → Opening → ResolvingStartNode
     → EnterNode ← ─────────────────────────────┐
         ↓                                       │
     [ノード種別]                                │
       Message: Typing → WaitingForAdvance ──────┤
                AutoAdvancing ──────────────────┤
       Choice:  SelectingChoice ────────────────┤
       Branch:  EvaluatingBranch ───────────────┤
       Event:   ExecutingEvent ─────────────────┤
       Sequence:WaitingExternalSequence ─────────┤
       End:     ExecutingEvent → (終了) ─────────┘
     → Closing → Idle
```

### 依存関係・使い方

```csharp
// DialogueManagerコンポーネントをシーンに配置し、
// DialogueUIControllerとDialogueFlagDatabaseを紐付ける。

// 外部イベントハンドラーの登録
dialogueManager.EventExecutor.RegisterHandler("Quest_001_Started", new QuestEventHandler());

// フラグ操作
dialogueManager.FlagService.SetBool("quest_001_active", true);

// 会話開始（NPCトリガーから自動、または直接呼び出し）
dialogueManager.StartDialogue(dialogueSetList);
```

#### IDialogueEventHandler の実装例

```csharp
public class FlagEventHandler : IDialogueEventHandler
{
    private DialogueFlagService _flagService;

    public FlagEventHandler(DialogueFlagService flagService) => _flagService = flagService;

    public IEnumerator Execute(DialogueEvent evt)
    {
        ApplyImmediate(evt);
        yield break;
    }

    public void ApplyImmediate(DialogueEvent evt)
    {
        // evt.Parameters[0] = flagId, Parameters[1] = "true"/"false"
        if (evt.Parameters.Length >= 2)
            _flagService.SetBool(evt.Parameters[0], bool.Parse(evt.Parameters[1]));
    }
}
```

## 実装メモ

### スキップ設計
スキップは初期設計から組み込み。`ResolveSkipState()` がノードごとのポリシー（`SkipPolicy`）と既読状態を確認し、スキップ継続・停止を決定する。スキップ中でも `MustRunOnSkip` イベントは `ApplyImmediate()` で必ず実行し、ゲーム状態の整合性を保つ。

### ノードのポリモーフィズム
`DialogueSet.Nodes` は `[SerializeReference]` を使用することで、Unityのインスペクター上で派生クラス（各ノード型）をリストに格納できる。

### 入力ブリッジ
`Update()` でキー入力を検出してフラグに記録し、コルーチン内でそのフラグを読み取る設計。これにより、コルーチンと入力処理が疎結合になっている。

### 外部システム連携
会話システムはイベントIDのみを保持し、実際の処理は `IDialogueEventHandler` を実装した外部クラスが担う。Quest更新・アイテム付与・SE再生等は全てハンドラーとして外部から登録する。

## 既知の制限・TODO

- [ ] `SequenceNode` の外部シーケンスシステム連携は未実装（即時完了として処理）
- [ ] `ConditionTargetType.Quest` / `Item` の条件評価は外部ハンドラー連携が必要
- [ ] 選択肢のグレーアウト表示はUIプレハブの構成に依存
- [ ] ボイス連携・立ち絵差分・表情制御は後続拡張（仕様書 §19.3）
- [ ] 既読情報・フラグのセーブ/ロードはセーブシステムと連携が必要（APIは用意済み）

## 変更履歴

| 日付 | 内容 |
|------|------|
| 2026-04-19 | 初版作成（仕様書v1.0 必須・準必須機能を実装） |
