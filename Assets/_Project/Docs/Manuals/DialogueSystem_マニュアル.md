# 会話システム マニュアル

## 目次

1. [全体ワークフロー](#1-全体ワークフロー)
2. [シーンセットアップ](#2-シーンセットアップ)
3. [DialogueSet の作成](#3-dialogueset-の作成)
4. [ノード種別リファレンス](#4-ノード種別リファレンス)
5. [フラグシステム](#5-フラグシステム)
6. [NPCセットアップ](#6-npcセットアップ)
7. [イベントハンドラーの実装](#7-イベントハンドラーの実装)
8. [複数会話セットによる台詞分岐](#8-複数会話セットによる台詞分岐)
9. [キー操作一覧](#9-キー操作一覧)
10. [トラブルシューティング](#10-トラブルシューティング)

---

## 1. 全体ワークフロー

```
① シーンセットアップ（初回のみ）
   └─ DialogueSystem GameObject を配置
   └─ UI を構築して DialogueUIController に紐付け

② フラグ定義（必要な場合）
   └─ DialogueFlagDatabase を作成
   └─ 使用するフラグを登録

③ 会話データ作成
   └─ DialogueSet ScriptableObject を作成
   └─ Inspector の Node ボタンでノードを追加・設定

④ NPC セットアップ
   └─ NpcDialogueTrigger をアタッチ
   └─ DialogueSet を紐付け
   └─ 頭上プロンプト を設定

⑤ 再生して動作確認
```

---

## 2. シーンセットアップ

### 2.1 DialogueSystem の配置

ヒエラルキーの**ルート**（他の GameObject の子にしない）に配置する。

```
DialogueSystem          ← ルートに置く（DontDestroyOnLoad 対象）
├── DialogueManager
├── DialogueUIController
└── Canvas (World Space または Screen Space Overlay)
    ├── DialogueWindow (Panel)
    │   ├── SpeakerNameText (TextMeshProUGUI)
    │   ├── DialogueText (TextMeshProUGUI)
    │   └── NextIndicator (GameObject)  例: "▼"
    ├── AutoIndicator (GameObject)      例: "AUTO"
    ├── SkipIndicator (GameObject)      例: "SKIP"
    └── ChoicePanel (Panel)
        └── ChoiceContainer (VerticalLayoutGroup)
```

### 2.2 DialogueManager の設定

| フィールド | 説明 | 推奨値 |
|------------|------|--------|
| Ui Controller | DialogueUIController コンポーネント | 同 GameObject |
| Flag Database | DialogueFlagDatabase アセット | 作成したものを設定 |
| Default Text Speed | 1文字あたりの表示秒数 | 0.03 |
| Default Auto Delay | オート進行の待機秒数 | 2.0 |
| Skip Interval | スキップ時のノード間隔秒数 | 0.02 |

### 2.3 DialogueUIController の設定

各フィールドにシーン上の GameObject / Component をドラッグして紐付ける。  
**未設定のフィールドはエラーにならず無視される**ため、最小構成から始めてよい。

**最小構成（まず動かす場合）:**
- Dialogue Window
- Dialogue Text

---

## 3. DialogueSet の作成

### 3.1 アセット作成

Project ウィンドウで右クリック →  
**Create → LibraryOfGamecraft → Dialogue → DialogueSet**

### 3.2 共通設定項目

| フィールド | 説明 |
|------------|------|
| Dialogue Set Id | 一意のID。フラグ連動やジャンプで参照する（例: `villager_default`） |
| Display Name | エディタ上での表示名（ゲームには影響しない） |
| Priority | 複数セットがある場合の優先度。**数値が大きいほど優先** |
| Start Conditions | この会話セットが選ばれる条件。空なら常に有効 |
| Start Node Id | 会話開始時に最初に実行するノードの NodeId |
| Default Skip Policy | セット全体のスキップ可否デフォルト（Allowed / Disallowed） |
| Default Auto Enabled | セット全体のオートモードデフォルト |
| Default Auto Delay | セット全体のオート待機秒数デフォルト |

### 3.3 ノードの追加

Inspector 下部のボタンで種別を選んで追加する。

```
ノードを追加:
[Message][Choice][Branch][Event][Sequence][Jump][End]
```

追加すると `Node_001`、`Node_002`... と自動採番される。  
`NodeId` は後から自由に変更可能。

---

## 4. ノード種別リファレンス

### 共通項目（全ノード）

| フィールド | 説明 |
|------------|------|
| **NodeId** | このノードを識別する一意のID。他のノードの NextNodeId から参照される |
| **NextNodeId** | このノード完了後に進む次のノードのID。空にすると会話終了 |
| **Comment** | メモ欄。ゲームには影響しない |
| **Skip Policy** | スキップ可否。`Inherit`=セットのデフォルトを使用 / `Allowed`=許可 / `Disallowed`=禁止 |
| **Log Policy** | `Record`=ログに記録する / `Skip`=記録しない |
| **Conditions** | このノードに入る前提条件。条件を満たさない場合は NextNodeId へスキップ（未実装） |
| **Pre Events** | ノード処理前に実行するイベント |
| **Post Events** | ノード処理後に実行するイベント |

---

### 4.1 MessageNode（テキスト表示）

NPC の台詞など、テキストを1つ表示するノード。最も基本的なノード。

| フィールド | 説明 | 例 |
|------------|------|-----|
| **Speaker Display Name** | 画面に表示される話者名 | `村人`、`勇者` |
| **Speaker Id** | 話者の内部ID。ボイス等の拡張で使用 | `npc_villager_01` |
| **Text** | 表示するテキスト本文 | `「旅人よ、よくぞ来た。」` |
| **Voice Id** | ボイスID（将来拡張用。現時点では未使用） | `voice_001` |
| **Portrait Id** | 立ち絵ID（将来拡張用。現時点では未使用） | `portrait_villager` |
| **Text Speed Override** | 1文字あたりの表示秒数を個別指定。`-1` でデフォルト使用 | `0.05`（遅め）|
| **Auto Advance Enabled** | このノードだけオート進行を強制するか | `false` |
| **Auto Advance Delay** | Auto Advance Enabled が true の場合の待機秒数 | `3.0` |

**接続例:**
```
Node_001 (Message) → NextNodeId: Node_002
Node_002 (Message) → NextNodeId: Node_999
Node_999 (End)
```

---

### 4.2 ChoiceNode（選択肢表示）

プレイヤーに選択肢を提示するノード。選択結果によって分岐する。

| フィールド | 説明 |
|------------|------|
| **Prompt Text** | 選択肢の上に表示する問いかけテキスト（省略可） |
| **Choices** | 選択肢のリスト（2件以上推奨） |

#### Choice（各選択肢）の設定項目

| フィールド | 説明 | 例 |
|------------|------|-----|
| **Choice Id** | 選択肢の内部ID | `choice_yes` |
| **Choice Text** | 画面に表示するテキスト | `「はい、お受けします」` |
| **Show Conditions** | 表示条件。空なら常に表示 | フラグ `has_item = true` |
| **Enable Conditions** | 選択可否条件。条件を満たさない場合はグレーアウト表示 | フラグ `gold >= 100` |
| **On Selected Events** | この選択肢を選んだときに発火するイベント | フラグ更新など |
| **Next Node Id** | この選択肢を選んだときに進むノードID | `Node_003` |

**接続例:**
```
Node_002 (Choice)
  ├── はい → Node_003
  └── いいえ → Node_004
```

> **スキップ動作:** 選択肢ノードに到達すると自動的にスキップが停止する。

---

### 4.3 BranchNode（条件分岐）

プレイヤー操作なしでフラグ等の条件に基づいて自動的に分岐するノード。  
ChoiceNode と異なり UI は表示されない。

| フィールド | 説明 |
|------------|------|
| **Branches** | 分岐条件のリスト。上から評価し最初に一致したものを採用 |
| **Default Next Node Id** | どの条件にも一致しなかった場合の遷移先 |

#### Branch（各分岐）の設定項目

| フィールド | 説明 | 例 |
|------------|------|-----|
| **Condition** | 分岐条件（後述の条件設定を参照） | フラグ `quest_done = true` |
| **Priority** | 評価優先度。**数値が大きいほど先に評価** | `10` |
| **Next Node Id** | 条件一致時の遷移先 | `Node_010` |

**接続例:**
```
Node_005 (Branch)
  ├── quest_done = true (Priority:10) → Node_010 (クエスト完了後の台詞)
  ├── quest_active = true (Priority:5) → Node_011 (クエスト中の台詞)
  └── Default → Node_012 (通常の台詞)
```

---

### 4.4 EventNode（イベント実行）

台詞表示を伴わずにイベントだけを実行するノード。  
フラグ更新・クエスト更新・SE再生などに使う。

| フィールド | 説明 |
|------------|------|
| **Events** | 実行するイベントのリスト |
| **Wait Mode** | `Immediate`=即時進行 / `WaitForComplete`=完了待ち / `WaitForSignal`=外部シグナル待ち |

#### Event（各イベント）の設定項目

| フィールド | 説明 | 例 |
|------------|------|-----|
| **Event Id** | 実行するハンドラーのID。コード側で登録したIDと一致させる | `SetFlag`、`PlaySE` |
| **Parameters** | ハンドラーに渡す文字列の引数リスト | `["quest_001_active", "true"]` |
| **Skip Policy** | `MustRunOnSkip`=スキップ中でも必ず実行 / `OptionalOnSkip`=省略可 / `VisualOnlyOnSkip`=視覚演出のため省略可 |
| **Wait Mode** | このイベント単体の待機方式 |

> **スキップ時の原則:** フラグ更新・クエスト更新・アイテム付与は `MustRunOnSkip` に設定すること。

**接続例:**
```
Node_003 (Event)
  Events:
    - EventId: SetFlag, Params: ["quest_001_active", "true"], SkipPolicy: MustRunOnSkip
  NextNodeId: Node_005
```

---

### 4.5 SequenceNode（外部シーケンス）

タイムラインやカメラ演出など外部システムのシーケンスを呼び出すノード。  
**現時点では即時完了扱い**（外部シーケンスシステムとの連携は将来実装）。

| フィールド | 説明 |
|------------|------|
| **Sequence Id** | 再生するシーケンスのID |
| **Wait For Completion** | シーケンス完了まで会話を待機するか |
| **Allow Skip** | シーケンス中のスキップを許可するか |

---

### 4.6 JumpNode（ジャンプ）

別のノードや別の DialogueSet へジャンプするノード。  
共通会話の再利用や会話ファイルの分割に使う。

| フィールド | 説明 | 例 |
|------------|------|-----|
| **Target Set Id** | ジャンプ先の DialogueSet の Id。空なら現在のセット内でジャンプ | `common_ending` |
| **Target Node Id** | ジャンプ先のノードId | `Node_001` |

**使用例:**
```
// 複数NPCで共通のエンディング会話を使いまわす場合
Node_020 (Jump)
  TargetSetId: common_ending
  TargetNodeId: Node_001
```

---

### 4.7 EndNode（会話終了）

会話を終了するノード。必ず会話の末尾に置く。

| フィールド | 説明 |
|------------|------|
| **End Reason** | 終了理由のメモ（ゲームには影響しない） |
| **End Events** | 終了時に実行するイベントのリスト |

> **注意:** EndNode の NextNodeId は無視される。  
> EndNode を置かずに NextNodeId を空にしても会話は終了するが、EndNode を明示的に置くことを推奨する。

---

## 5. フラグシステム

### 5.1 DialogueFlagDatabase の作成

Project ウィンドウで右クリック →  
**Create → LibraryOfGamecraft → Dialogue → FlagDatabase**

`DialogueManager` の **Flag Database** フィールドに設定する。

### 5.2 フラグの登録

| フィールド | 説明 | 例 |
|------------|------|-----|
| **Flag Id** | フラグの一意のID | `quest_001_active` |
| **Flag Type** | `Bool` または `Int` | `Bool` |
| **Default Bool Value** | Bool フラグの初期値 | `false` |
| **Default Int Value** | Int フラグの初期値 | `0` |

### 5.3 条件設定（Conditions / Start Conditions）

| フィールド | 説明 | 例 |
|------------|------|-----|
| **Target Type** | 条件の対象種別 | `FlagBool`、`FlagInt`、`DialogueRead` |
| **Target Id** | 対象のID | `quest_001_active` |
| **Operator** | 比較演算子 | `Equal`、`Greater` など |
| **Value** | 比較する値（文字列） | `"true"`、`"5"` |

**設定例:**

| Target Type | Target Id | Operator | Value | 意味 |
|-------------|-----------|----------|-------|------|
| FlagBool | `quest_done` | Equal | `true` | クエスト完了済み |
| FlagInt | `gold` | GreaterOrEqual | `100` | 所持金が100以上 |
| DialogueRead | `Node_001` | Equal | `true` | Node_001 を既読 |

### 5.4 コードからのフラグ操作

```csharp
var flags = DialogueManager.Instance.FlagService;

// 読み取り
bool active = flags.GetBool("quest_001_active");
int gold = flags.GetInt("gold");

// 書き込み
flags.SetBool("quest_001_active", true);
flags.SetInt("gold", 100);
flags.AddInt("gold", -50); // 減算
```

---

## 6. NPCセットアップ

### 6.1 GameObject 構成

```
NPC_Villager
├── Model (3Dモデル)
├── TriggerZone (空GameObject)
│   ├── SphereCollider
│   │     Is Trigger: ON / Radius: 2
│   └── NpcDialogueTrigger
│         Dialogue Sets: [DS_Villager_Default]
│         Interact Prompt: InteractPrompt
└── InteractPrompt (WorldSpace Canvas)
      Canvas / Render Mode: World Space
      Transform: NPCの頭上に配置
      └── Text (TextMeshPro): "E : 話す"
```

### 6.2 NpcDialogueTrigger の設定

| フィールド | 説明 |
|------------|------|
| **Dialogue Sets** | この NPC が持つ会話セットのリスト。複数登録可（Priority 順に評価） |
| **Player Tag** | プレイヤーの Tag（デフォルト: `Player`） |
| **Interact Prompt** | 頭上に表示するプロンプト GameObject |

### 6.3 プレイヤーへの設定

Player GameObject に `DialogueInteractController` をアタッチする。  
追加設定は不要（候補NPCの管理は自動）。

---

## 7. イベントハンドラーの実装

会話中イベント（フラグ更新・SE再生など）はコード側でハンドラーを登録して使う。

### 7.1 ハンドラーの実装

```csharp
using System.Collections;
using LibraryOfGamecraft.Dialogue;

public class FlagEventHandler : IDialogueEventHandler
{
    private readonly DialogueFlagService _flagService;

    public FlagEventHandler(DialogueFlagService flagService)
    {
        _flagService = flagService;
    }

    // 通常進行時（アニメーション等を伴う場合はここに記述）
    public IEnumerator Execute(DialogueEvent evt)
    {
        ApplyImmediate(evt);
        yield break;
    }

    // スキップ時・即時実行時（状態変更のみ。演出なし）
    public void ApplyImmediate(DialogueEvent evt)
    {
        // Parameters[0] = flagId, Parameters[1] = "true" or "false"
        if (evt.Parameters.Length >= 2)
            _flagService.SetBool(evt.Parameters[0], bool.Parse(evt.Parameters[1]));
    }
}
```

### 7.2 ハンドラーの登録

DialogueManager の初期化後（`Start()` など）に登録する。

```csharp
private void Start()
{
    var manager = DialogueManager.Instance;
    var flags = manager.FlagService;

    manager.EventExecutor.RegisterHandler("SetFlag",  new FlagEventHandler(flags));
    manager.EventExecutor.RegisterHandler("AddInt",   new AddIntEventHandler(flags));
    manager.EventExecutor.RegisterHandler("PlaySE",   new SEEventHandler());
}
```

### 7.3 EventId の命名規則（推奨）

| EventId | 用途 | Parameters 例 |
|---------|------|---------------|
| `SetFlag` | Bool フラグを設定 | `["quest_001_active", "true"]` |
| `AddInt` | Int フラグに加算 | `["gold", "100"]` |
| `PlaySE` | SE を再生 | `["se_item_get"]` |
| `StartQuest` | クエスト開始 | `["quest_001"]` |

---

## 8. 複数会話セットによる台詞分岐

同一NPCでゲーム状態に応じて台詞を変える場合、DialogueSet を複数作成して Priority で優先順位を設定する。

### 設定例：クエスト進行に応じた台詞

| SetId | Priority | Start Conditions | 使われるタイミング |
|-------|----------|-----------------|------------------|
| `villager_quest_done` | 20 | `quest_001_done = true` | クエスト完了後 |
| `villager_quest_active` | 10 | `quest_001_active = true` | クエスト受注後 |
| `villager_default` | 0 | なし | 上記以外 |

**NpcDialogueTrigger の Dialogue Sets に全て登録する:**

```
Dialogue Sets
  [0] DS_Villager_Quest_Done      Priority: 20
  [1] DS_Villager_Quest_Active    Priority: 10
  [2] DS_Villager_Default         Priority: 0
```

優先度の高いセットから順に条件を評価し、最初に一致したセットが使われる。

---

## 9. キー操作一覧

| キー | 動作 |
|------|------|
| `E` / `Enter` / `Space` | 会話開始 / 次送り / 選択確定 |
| `W` / `↑` | 選択肢カーソル上移動 |
| `S` / `↓` | 選択肢カーソル下移動 |
| `Tab` | オートモード ON/OFF |
| `Left Shift` | スキップモード ON/OFF |

---

## 10. トラブルシューティング

| 症状 | 原因 | 対処 |
|------|------|------|
| 会話が開始しない | Player の Tag が `Player` になっていない | Tag を確認・設定 |
| 会話が開始しない | `DialogueManager` が Idle 以外の状態 | Console でエラーを確認 |
| テキストが表示されない | `DialogueText` が未設定 | UIController のフィールドを確認 |
| ノードが見つからないエラー | `StartNodeId` と実際の `NodeId` が不一致 | 大文字・スペースを含めて一致させる |
| スキップが止まらない | 全ノードが既読済み | 一度 PlayMode を止めると既読がリセットされる |
| イベントが実行されない | ハンドラーが未登録 | `RegisterHandler` の呼び出しを確認 |
| `DontDestroyOnLoad` エラー | DialogueSystem が他 GameObject の子になっている | ヒエラルキーのルートに移動する |
| `?. SetActive` エラー | Unity の UnityEngine.Object は `?.` 非対応 | `if (obj != null) obj.SetActive()` を使う |
