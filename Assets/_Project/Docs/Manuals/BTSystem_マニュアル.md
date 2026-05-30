# BTSystem マニュアル

## 目次

1. [全体ワークフロー](#1-全体ワークフロー)
2. [シーンセットアップ](#2-シーンセットアップ)
3. [BTGraph の作成とビジュアルエディタ](#3-btgraph-の作成とビジュアルエディタ)
4. [ビルトインノード一覧](#4-ビルトインノード一覧)
5. [カスタムノードの作り方](#5-カスタムノードの作り方)
6. [Blackboard の使い方](#6-blackboard-の使い方)
7. [グラフ構成例](#7-グラフ構成例)
8. [ランタイムデバッグ](#8-ランタイムデバッグ)
9. [トラブルシューティング](#9-トラブルシューティング)

---

## 1. 全体ワークフロー

```
① シーンセットアップ
   └─ NPC GameObject に BTRunner をアタッチ
   └─ CharacterMotor・PerceptionSensor を必要に応じてアタッチ

② BTGraph アセットを作成
   └─ Project ウィンドウで右クリック → Create → LibraryOfGamecraft/BT/BTGraph

③ ビジュアルエディタでグラフを組む
   └─ BTGraph をダブルクリックで開く
   └─ 右クリックでノードを追加・接続・ルート設定・保存

④ BTRunner に BTGraph をアサイン
   └─ Inspector の Graph フィールドに BTGraph アセットをドラッグ

⑤ Play してランタイムデバッグで動作確認
```

---

## 2. シーンセットアップ

### 2.1 必須コンポーネント

NPC GameObject に以下をアタッチする。

| コンポーネント | 用途 | 必須 |
|--------------|------|------|
| `BTRunner` | BTGraph を毎フレーム実行する | ◎ |
| `NavMeshAgent` | NavMesh 移動（CharacterMotor が必要とする） | 移動ノードを使う場合 |
| `CharacterController` | 物理移動（CharacterMotor が必要とする） | 移動ノードを使う場合 |
| `CharacterMotor` | NavMeshAgent + CharacterController のラッパー | 移動ノードを使う場合 |
| `PerceptionSensor` | 視野・聴覚によるターゲット自動検知 | 知覚機能を使う場合 |
| `AttackCapability` | 攻撃処理の委譲先。OnAttackTriggered イベントにダメージ処理を配線する | AttackAction を使う場合 |

### 2.2 BTRunner の設定

| フィールド | 説明 |
|-----------|------|
| **Graph** | 実行する BTGraph アセット |

`Blackboard` プロパティがスクリプトから公開されているため、外部からターゲットを設定することも可能。

```csharp
GetComponent<BTRunner>().Blackboard.Set(BTKeys.Target, enemy.transform);
```

### 2.3 CharacterMotor の設定

| フィールド | 説明 | 推奨値 |
|-----------|------|--------|
| **Move Speed** | デフォルト移動速度（m/s） | `3.5` |
| **Rotation Speed** | 旋回速度（度/秒） | `360` |
| **Stopping Distance** | 目標到達とみなす距離（m） | `0.2` |

> **注意:** NavMesh が事前 Bake されていないと移動しない。`Window → AI → Navigation` でシーンを Bake すること。

### 2.4 PerceptionSensor の設定

| フィールド | 説明 | 推奨値 |
|-----------|------|--------|
| **Detection Range** | 視野半径（m） | `15` |
| **Field Of View** | 視野角（度） | `120` |
| **Hearing Radius** | 聴覚半径（m） | `8` |
| **Target Tag** | 検知対象の Tag | `Player` |
| **Obstacle Layer** | 視線を遮るレイヤー | `Default` |
| **Scan Interval** | 検索間隔（秒） | `0.2` |

PerceptionSensor は検知したターゲットを自動的に `Blackboard["target"]` へ書き込む。

---

## 3. BTGraph の作成とビジュアルエディタ

### 3.1 BTGraph アセットの作成

Project ウィンドウで右クリック →
**Create → LibraryOfGamecraft → BT → BTGraph**

### 3.2 エディタを開く

BTGraph アセットを**ダブルクリック**すると BT Graph エディタウィンドウが開く。  
メニューからも開ける: **LibraryOfGamecraft → BT Graph Editor**

### 3.3 ノードの追加

グラフ上で**右クリック** → `ノードを追加 / {カテゴリ} / {ノード名}` を選択。

```
ノードを追加
├── Action
│   ├── MoveToTargetAction
│   └── WanderAction
├── Composite
│   ├── BTSelector
│   └── BTSequence
├── Condition
│   ├── IsTargetDetectedCondition
│   └── IsTargetLostCondition
└── Decorator
    ├── BTCooldown
    ├── BTInverter
    └── BTRepeater
```

> カスタムノードを作成すると自動的にこの一覧に追加される（リフレクションで列挙）。

### 3.4 ノードの接続

出力ポート（ノード下部）から入力ポート（ノード上部）へ**ドラッグ**してエッジを引く。

- **Composite（Selector / Sequence）**: 子の数だけ出力ポートが並ぶ。左から優先度順。
- **Decorator（Inverter / Cooldown 等）**: 子1つのみ接続できる出力ポート。
- **Action / Condition**: 出力ポートなし（リーフノード）。

`＋ 子を追加` ボタンで Composite の出力ポートを増やせる。

### 3.5 ルートノードの設定

ノードを**右クリック** → `ルートに設定`。  
設定されたノードは**黄色のボーダー**でハイライトされる。

### 3.6 ノードのプロパティ編集

ノードを**クリックして選択**すると、Unity の Inspector ウィンドウにそのノードのプロパティが表示される。

- ノード名の変更: Inspector 最上部の Name フィールド
- `_cooldownTime`・`_chaseSpeed` などの変数: Inspector のフィールドから直接編集

### 3.7 ツールバー

| ボタン | 動作 |
|--------|------|
| **保存** | `AssetDatabase.SaveAssets()` を実行してアセットに書き込む |
| **自動整列** | ルートノードを起点に BFS でノードを自動配置する |
| **全体表示** | グラフ全体が収まるようにズームを調整する |

> **注意:** エッジ接続・ノード移動の変更は即時 SetDirty されるが、**必ず保存ボタンを押してからシーンを実行**すること。

---

## 4. ビルトインノード一覧

### 4.1 Composite（合成）

| ノード | 挙動 |
|--------|------|
| `BTSequence` | **AND** — 子を左から順に評価。最初に Failure/Running を返した時点でその値を返す。全子が Success なら Success |
| `BTSelector` | **OR** — 子を左から順に評価。最初に Success/Running を返した時点でその値を返す。全子が Failure なら Failure |

> どちらも**毎フレーム先頭から再評価**する（Non-memory）。これにより条件ノードが常に再チェックされ、敵が視野から外れた瞬間に追跡を中断できる。

### 4.2 Decorator（装飾）

| ノード | 設定値 | 挙動 |
|--------|--------|------|
| `BTInverter` | なし | 子の Success ↔ Failure を反転。Running はそのまま通す |
| `BTCooldown` | **Cooldown Time**（秒） | 子が完了した後 N 秒間は Failure を返してブロック。攻撃間隔などに使う |
| `BTRepeater` | **Repeat Count**（0=無限） | 子が完了するたびに繰り返す。0 なら常に Running |

### 4.3 Action（行動）

| ノード | 設定値 | 挙動 |
|--------|--------|------|
| `WanderAction` | **Wander Radius**、**Move Time**、**Wait Time** | HomePosition 周辺をランダムに徘徊。常に Running |
| `MoveToTargetAction` | **Chase Speed**、**Update Interval** | `Blackboard["target"]` を追跡。target が null なら Failure |
| `AttackAction` | **Rotation Speed** | 移動を停止してターゲット方向を向き、`AttackCapability.TriggerAttack()` を呼んで Success を返す。クールダウンは `BTCooldown` で制御する |

### 4.4 Condition（条件）

| ノード | 設定値 | 評価 |
|--------|--------|------|
| `IsTargetDetectedCondition` | なし | `Blackboard["target"] != null` なら Success |
| `IsTargetLostCondition` | **Lost Range**（m） | target が null または指定距離以上なら Success |
| `IsInAttackRangeCondition` | **Attack Range**（m） | target が指定距離以内なら Success |

---

## 5. カスタムノードの作り方

### 5.1 Action ノード

`OnEnter` / `OnTick` / `OnExit` の3段階ライフサイクルを持つ。

```csharp
using UnityEngine;
using LibraryOfGamecraft.BT;

[CreateAssetMenu(menuName = "LibraryOfGamecraft/BT/Actions/AttackAction")]
public class AttackAction : BTAction
{
    [SerializeField] private float _attackRange = 2f;

    protected override void OnEnter(BTContext ctx)
    {
        // 行動開始時に1度だけ呼ばれる
    }

    protected override BTStatus OnTick(BTContext ctx)
    {
        var target = ctx.Blackboard.Get<Transform>(BTKeys.Target);
        if (target == null) return BTStatus.Failure;

        float dist = Vector3.Distance(ctx.Transform.position, target.position);
        if (dist > _attackRange) return BTStatus.Failure;

        // 攻撃処理...
        return BTStatus.Success;
    }

    protected override void OnExit(BTContext ctx, BTStatus status)
    {
        // 行動終了時（Success または Failure）に1度だけ呼ばれる
    }
}
```

### 5.2 Condition ノード

`Check()` が true なら Success、false なら Failure を返す。Running は返さない。

```csharp
using UnityEngine;
using LibraryOfGamecraft.BT;

[CreateAssetMenu(menuName = "LibraryOfGamecraft/BT/Conditions/IsHealthLowCondition")]
public class IsHealthLowCondition : BTCondition
{
    [SerializeField] private float _threshold = 30f;

    protected override bool Check(BTContext ctx)
    {
        var health = ctx.Get<HealthComponent>();
        return health != null && health.Current < _threshold;
    }
}
```

### 5.3 Decorator ノード

子を1つ持ち、子の実行結果を加工して返す。

```csharp
using UnityEngine;
using LibraryOfGamecraft.BT;

[CreateAssetMenu(menuName = "LibraryOfGamecraft/BT/Decorators/BTSucceeder")]
public class BTSucceeder : BTDecorator
{
    // 子が何を返しても Success に変換する
    protected override BTStatus Execute(BTContext ctx)
    {
        if (_child == null) return BTStatus.Success;
        _child.Tick(ctx);
        return BTStatus.Success;
    }
}
```

### 5.4 CapabilityComponent へのアクセス

ノード内では `ctx.Get<T>()` で MonoBehaviour を取得する。

```csharp
var motor  = ctx.Get<CharacterMotor>();
var sensor = ctx.Get<PerceptionSensor>();
```

Unity の `?.` 演算子は `UnityEngine.Object` で正常に動作しないため、必ず null チェックを明示的に書く。

```csharp
// NG
motor?.MoveTo(destination);

// OK
if (motor != null) motor.MoveTo(destination);
```

---

## 6. Blackboard の使い方

`BTBlackboard` はノード間でランタイム状態を共有するキー・バリューストア。  
BTRunner インスタンスごとに独立しているため、複数の NPC が同じ BTGraph アセットを使っても状態は混在しない。

### 6.1 定義済みキー（BTKeys）

| 定数 | 値 | 用途 |
|------|-----|------|
| `BTKeys.Target` | `"target"` | PerceptionSensor が検知したターゲットの Transform |
| `BTKeys.HomePosition` | `"homePosition"` | BTRunner 起動時の初期位置（Vector3）。Wander の中心点 |

### 6.2 読み書き

```csharp
// 書き込み
ctx.Blackboard.Set("myKey", someValue);
ctx.Blackboard.Set(BTKeys.Target, targetTransform);

// 読み込み（デフォルト値を指定できる）
var target = ctx.Blackboard.Get<Transform>(BTKeys.Target);
float hp   = ctx.Blackboard.Get<float>("hp", 100f);

// 存在確認
bool has = ctx.Blackboard.Has("myKey");

// 削除
ctx.Blackboard.Unset("myKey");
```

### 6.3 外部からの書き込み（BTRunner 経由）

```csharp
// 外部スクリプトからターゲットを強制設定する例
var runner = npc.GetComponent<BTRunner>();
runner.Blackboard.Set(BTKeys.Target, player.transform);
```

---

## 7. グラフ構成例

### 7.1 Wander → Chase（基本パターン）

敵を検知したら追跡、見失ったら徘徊に戻る最も基本的な構成。

```
BTSelector  ← ルート
├── [0] BTSequence          ← 追跡ブランチ（優先）
│   ├── IsTargetDetectedCondition
│   └── MoveToTargetAction
└── [1] WanderAction        ← 徘徊（フォールバック）
```

**動作フロー:**
1. `BTSelector` が `BTSequence` を試みる
2. `IsTargetDetected` が Success（ターゲットを検知）→ `MoveToTarget` が Running で追跡継続
3. ターゲットを見失うと `IsTargetDetected` が Failure → `BTSequence` が Failure → `BTSelector` が `WanderAction` にフォールバック

---

### 7.2 Wander → Alert → Chase（警戒フェーズ付き）

検知直後に一定時間停止して向きを変える警戒フェーズを挟む。

```
BTSelector  ← ルート
├── [0] BTSequence          ← 追跡ブランチ
│   ├── IsTargetDetectedCondition
│   ├── BTCooldown (3秒)    ← 警戒フェーズ（3秒間 Failure → Wander に戻る）
│   │   └── AlwaysSuccessNode（またはアラートアクション）
│   └── MoveToTargetAction
└── [1] WanderAction
```

**動作フロー:**
1. 敵を初めて検知 → `BTCooldown` が 3秒間 Failure を返す → Wander 継続（その場での自然な反応）
2. 3秒後 → `BTCooldown` が子を実行（Success）→ `MoveToTarget` で追跡開始

---

### 7.3 Patrol → Chase（巡回＋追跡）

通常は決められたルートを巡回し、敵を見つけたら追跡する。

```
BTSelector  ← ルート
├── [0] BTSequence          ← 追跡ブランチ（最優先）
│   ├── IsTargetDetectedCondition
│   └── MoveToTargetAction
└── [1] PatrolAction        ← 通常は巡回（カスタムノード）
```

---

### 7.4 Chase → Attack（近接攻撃付き）

追跡して攻撃距離に入ったら攻撃する。攻撃にはクールダウンを設ける。

```
BTSelector  ← ルート
├── [0] BTSequence          ← 戦闘ブランチ
│   ├── IsTargetDetectedCondition
│   └── BTSelector          ← 攻撃 or 接近
│       ├── [0] BTSequence  ← 攻撃（射程内のとき）
│       │   ├── IsInAttackRangeCondition（カスタム）
│       │   └── BTCooldown (1秒)
│       │       └── AttackAction（カスタム）
│       └── [1] MoveToTargetAction  ← 接近（射程外のとき）
└── [1] WanderAction
```

---

### 7.5 Decorator の使い方例

#### BTInverter — 「ターゲットがいない間だけ実行」

```
BTInverter
└── IsTargetDetectedCondition    → ターゲットなし = Success
```

`IsTargetDetectedCondition` は「ターゲットあり = Success」なので、  
`BTInverter` で反転すると「ターゲットなし = Success」になる。

#### BTRepeater — 「3回攻撃して終了」

```
BTRepeater (repeatCount = 3)
└── AttackAction
```

#### BTCooldown — 「5秒ごとに咆哮」

```
BTCooldown (cooldownTime = 5)
└── RoarAction（カスタム）
```

---

## 8. ランタイムデバッグ

BTGraph エディタウィンドウを**開いたまま Play Mode に入る**と、ノードの実行状態がリアルタイムに可視化される。

### 8.1 カラーコード

| 色 | 状態 | 意味 |
|----|------|------|
| 黄色（太） | **Running** | 現在このノードが実行中（アクティブパス） |
| 緑 | **Success** | このフレームで Success を返した |
| 赤 | **Failure** | このフレームで Failure を返した |
| なし（デフォルト） | 未実行 | このフレームは Tick されていない |

> Non-memory 設計のため、Composite は毎フレーム先頭から評価される。  
> Success / Failure はフラッシュして見えることがある（1フレームで完了するため）。

### 8.2 典型的な表示パターン

**徘徊中（ターゲット未検知）:**
- `BTSelector` → 黄（Running）
- `BTSequence` → 赤（Failure: IsTargetDetected が Failure）
- `IsTargetDetected` → 赤（Failure）
- `WanderAction` → 黄（Running）
- `MoveToTarget` → 未実行（グレー）

**追跡中（ターゲット検知済み）:**
- `BTSelector` → 黄（Running）
- `BTSequence` → 黄（Running: MoveToTarget が Running）
- `IsTargetDetected` → 緑（Success）
- `MoveToTarget` → 黄（Running）
- `WanderAction` → 未実行（グレー）

---

## 9. トラブルシューティング

| 症状 | 原因 | 対処 |
|------|------|------|
| NPC が動かない | NavMesh が Bake されていない | `Window → AI → Navigation` でシーンを Bake |
| NPC が動かない | BTRunner の Graph が未設定 | Inspector で BTGraph アセットをアサイン |
| NPC が動かない | CharacterMotor が未アタッチ | MoveToTargetAction などは CharacterMotor を要求する |
| エディタが空で開く | _allNodes に登録されていない古いアセット | 新しく BTGraph を作成し直す |
| ノード追加後に保存しないと消える | SetDirty されているが SaveAssets 未実行 | ツールバーの「保存」ボタンを押す |
| デバッグカラーが更新されない | エディタウィンドウが閉じている | Play 前に BT Graph エディタを開いておく |
| ターゲットが検知されない | PerceptionSensor の Target Tag が合っていない | Player オブジェクトの Tag を確認 |
| ターゲットが検知されない | 障害物レイヤーの設定ミス | Obstacle Layer を視線を遮るレイヤーに設定 |
| `?.` を使うとコンパイル警告 | Unity の null チェックに `?.` は使えない | `if (x != null) x.Method()` に書き換える |
| Composite の子順が意図と違う | ポートの `[0]` `[1]` の順に評価される | ビジュアルエディタで接続ポートの番号を確認 |
