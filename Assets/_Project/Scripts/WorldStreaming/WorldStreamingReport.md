# グリッドベース Additive Load システム 設計・実装レポート

## 概要

512×512 ワールド座標単位のグリッドで分割されたオープンワールドを、
プレイヤー位置に応じて安定的に Additive Load / Unload するシステム。

---

## ファイル構成

```
Assets/_Project/Scripts/WorldStreaming/
├── WorldGridUtility.cs        # static: 座標変換・グリッド範囲列挙
├── SceneNameResolver.cs       # static: Scene 名の生成・逆パース
├── WorldSceneStreamer.cs       # MonoBehaviour: ロード/アンロード実行・状態管理
├── TerrainNeighborUpdater.cs  # MonoBehaviour: Terrain.SetNeighbors 更新
├── WorldStreamingHub.cs       # MonoBehaviour: 外部公開 API・イベント集約
└── WorldStreamingController.cs # MonoBehaviour: エントリーポイント・差分更新起点
```

---

## クラス責務一覧

| クラス | 種別 | 責務 |
|---|---|---|
| `WorldGridUtility` | static | ワールド座標 ↔ グリッド座標変換、半径内グリッド列挙、チェビシェフ距離 |
| `SceneNameResolver` | static | `World_{x}_{z}` 形式の Scene 名生成・逆パース |
| `WorldSceneStreamer` | MonoBehaviour | Scene の非同期ロード/アンロード・状態追跡・イベント発火 |
| `TerrainNeighborUpdater` | MonoBehaviour | `Terrain.SetNeighbors` による隣接 Terrain 接続更新 |
| `WorldStreamingHub` | MonoBehaviour | 他システムへの公開窓口（イベント購読・プロパティ参照） |
| `WorldStreamingController` | MonoBehaviour | プレイヤー位置監視・グリッド変更検知・差分ロード/アンロード起点 |

---

## アーキテクチャ概要

### データフロー

```
[Update: 毎フレーム]
  WorldStreamingController.Update()
      ↓  WorldToGrid(playerPos) でグリッド座標を計算
      ↓  前フレームと同じなら何もしない（毎フレームフルスキャンしない）
      ↓  グリッドが変わった時だけ ↓
  UpdateStreaming(centerGrid)
      ↓  requiredGrids = GetGridsInRadius(center, loadRadius)
      ↓  keepGrids     = GetGridsInRadius(center, loadRadius + unloadBuffer)
      ↓  差分チェック
  WorldSceneStreamer.RequestLoad()   → LoadSceneRoutine（コルーチン）
  WorldSceneStreamer.RequestUnload() → UnloadSceneRoutine（コルーチン）

[コルーチン完了時]
  OnSceneLoaded  → TerrainNeighborUpdater.UpdateNeighbors()
                 → CheckAndUnloadIfStale()（高速移動対策）
  OnSceneUnloaded → TerrainNeighborUpdater.UpdateNeighbors()
```

### 境界安定化の仕組み

```
loadRadius=1, unloadBuffer=1 の場合:

  ロード範囲  = 3×3 （center ±1 マス）
  キープ範囲  = 5×5 （center ±2 マス）

  プレイヤーが境界を行き来しても、5×5 の外に出るまでアンロードしない。
  → 境界付近の往復でロード/アンロードが暴れない。
```

### 依存関係

```
WorldStreamingController
    ├── WorldSceneStreamer  （ロード/アンロード実行）
    ├── TerrainNeighborUpdater  （Terrain 接続更新）
    └── WorldStreamingHub  （外部公開）

WorldGridUtility  （static、どこからでも参照可能）
SceneNameResolver （static、どこからでも参照可能）

外部システム（敵スポーン、BGM など）
    └── WorldStreamingHub.OnSceneLoaded / OnGridChanged を購読
```

---

## 4. Additive Load / Unload の制御方針

### ロード制御

1. `Update()` でプレイヤーのグリッドを算出（`WorldToGrid`）
2. 前フレームと同じグリッドなら即 return（毎フレームスキャンなし）
3. グリッドが変わった場合のみ `UpdateStreaming()` を実行
4. `GetGridsInRadius(center, loadRadius)` で必要グリッド一覧を作成
5. ロード済み・ロード中でないグリッドに `RequestLoad()` を発行

### アンロード制御

1. `GetLoadedGridsSnapshot()` でロード済みグリッドの安全なコピーを取得
2. `keepGrids`（loadRadius + unloadBuffer の範囲）に含まれないグリッドを対象にする
3. すでにアンロード中のものは重複してリクエストしない
4. `RequestUnload()` を発行

### 重複防止

| 状態 | RequestLoad の挙動 | RequestUnload の挙動 |
|---|---|---|
| ロード済み | スキップ | 実行 |
| ロード中 | スキップ | ロード完了後に CheckAndUnloadIfStale が対処 |
| アンロード中 | — | スキップ |
| 未ロード | 実行 | スキップ |

---

## 5. Terrain 接続更新の方針

### SetNeighbors のパラメータ対応

```
Terrain.SetNeighbors(left, top, right, bottom)
         ↕           ↕     ↕    ↕      ↕
Grid 方向         X-1  Z+1  X+1  Z-1
```

### 更新タイミング

- Scene ロード完了後 → `UpdateNeighbors(loadedGrid)`
- Scene アンロード完了後 → `UpdateNeighbors(unloadedGrid)`

### 更新範囲

`changedGrid` を中心に 3×3 の全グリッドに対して実行する。
これにより、変化したグリッドの隣接 Terrain も正しく再接続される。

### 安全設計

- 未ロードグリッドの Terrain は `null` → SetNeighbors で「接続なし」扱い
- Terrain コンポーネントが存在しない Scene は `FindTerrainInScene()` が `null` を返し、スキップ
- Scene ハンドルが無効な場合も早期リターン

---

## 6. Unity エディタでの設定手順

### STEP 1 — Scene を Build Settings に登録する

1. `File → Build Settings` を開く
2. ロードしたい全 Scene（`World_0_0`、`World_1_0` など）を **Add Open Scenes** または
   直接ドラッグで登録する
3. **重要**: Build Settings に登録されていない Scene は `LoadSceneAsync` が null を返す

### STEP 2 — WorldStreaming GameObject を作成する

1. Hierarchy で右クリック → **Create Empty**
2. 名前を `WorldStreaming` に変更
3. **Add Component** → `WorldStreamingController` を追加
   - `[RequireComponent]` により `WorldSceneStreamer`、`TerrainNeighborUpdater`、
     `WorldStreamingHub` が自動追加される

### STEP 3 — Inspector を設定する

`WorldStreamingController` の Inspector:

| フィールド | 設定値 | 説明 |
|---|---|---|
| Player Transform | Player オブジェクトの Transform | 必須 |
| Grid Size | 512 | Terrain のサイズに合わせる |
| Load Radius | 1 | 3×3 範囲をロード |
| Unload Buffer | 1 | 5×5 範囲を超えるまでアンロードしない |
| Load On Start | true | ゲーム開始時に自動ロード |
| Enable Debug Log | true（開発中）/ false（リリース） | ログの ON/OFF |

### STEP 4 — Terrain Scene を準備する

各 Scene（例: `World_0_0`）に:
- Terrain オブジェクトを 1 つ配置
- Terrain の Position を `WorldGridUtility.GridToWorldOrigin(grid, 512f)` に合わせる
  - `World_0_0` → Position (0, 0, 0)
  - `World_1_0` → Position (512, 0, 0)
  - `World_-1_2` → Position (-512, 0, 1024)
- Terrain の Width / Length を 512 に設定

### STEP 5 — 動作確認

1. **Play** を押す
2. Console に `[WorldStreaming] 初期グリッド: (0, 0) → ロード開始` が出れば初期化成功
3. Scene が Additive でロードされ、Hierarchy に追加される
4. プレイヤーをグリッド境界を越えて移動させると `グリッド変更: (0,0) → (1,0)` が出る

---

## 7. よくある不具合と対策

| 症状 | 原因 | 対処 |
|---|---|---|
| `LoadSceneAsync` が null を返す | Scene が Build Settings に未登録 | `File → Build Settings` に Scene を追加する |
| Scene が重複してロードされる | IsLoaded/IsLoading チェックが機能していない | WorldSceneStreamer の状態辞書をデバッグログで確認する |
| Terrain の縫い目が消えない | SetNeighbors が呼ばれていない | TerrainNeighborUpdater.UpdateNeighbors の呼び出しを確認する |
| ロード/アンロードが連続して発生する | unloadBuffer が 0 になっている | unloadBuffer を 1 以上に設定する |
| 最初のグリッドがロードされない | loadOnStart が false | Inspector で `Load On Start` を true にする |
| `playerTransform` null エラー | Inspector での設定漏れ | WorldStreamingController の Player Transform をアサインする |
| Scene アンロード後に Console エラー | アンロード後も Scene 内オブジェクトを参照している | OnSceneUnloaded イベントで参照をクリアする |
| 高速移動時に不要 Scene が残る | ロード中に範囲外に移動した | CheckAndUnloadIfStale が対処する（ロード完了後に即アンロード）|

---

## 8. 将来拡張案

### ロード半径の変更
`loadRadius` は SerializeField のため Inspector から変更可能。
実行中の変更が必要な場合は `WorldStreamingController` に `SetLoadRadius(int r)` メソッドを追加する。

### 先読み（プレイヤーの移動方向にバイアスをかけたロード）
```csharp
// WorldStreamingController に追加
Vector3 velocity = /* Rigidbody.velocity など */;
Vector2Int moveDir = WorldGridUtility.WorldToGrid(velocity.normalized * gridSize, gridSize);
var priorityGrids = GetGridsInRadius(centerGrid + moveDir, loadRadius);
// priorityGrids を先にロードリクエストする
```

### LOD 連携
`WorldStreamingHub.OnGridChanged` を購読し、
グリッド距離に応じて LOD Group の設定を切り替える。

### Addressables 移行
`WorldSceneStreamer` の `LoadSceneRoutine` / `UnloadSceneRoutine` 内の
`SceneManager.LoadSceneAsync` / `UnloadSceneAsync` を
`Addressables.LoadSceneAsync` / `Addressables.UnloadSceneAsync` に置き換えるだけでよい。
呼び出し側のコードは変更不要（差し替えポイントが明確）。

### NavMesh 管理
`WorldStreamingHub.OnSceneLoaded` を購読し、
ロード完了後に NavMeshSurface.BuildNavMesh() を呼ぶ。

### 敵スポーン・BGM 切り替え
```csharp
hub.OnGridChanged += grid => {
    EnemySpawner.Instance.LoadSpawnData(grid);
    BGMManager.Instance.ChangeBGM(grid);
};
hub.OnSceneLoaded += (grid, scene) => {
    ObjectPlacer.Instance.PlaceObjects(grid, scene);
};
```

### セーブ/ロード連携
`WorldStreamingHub.OnSceneLoaded` でシーンのロード完了を待ってから
セーブデータの状態を復元する。
`OnSceneUnloaded` でシーン内の動的状態を保存する。

---

## 9. この設計が拡張に強い理由

| 原則 | 実装箇所 | 効果 |
|---|---|---|
| **SRP（単一責務）** | 座標変換・Scene 名・ロード管理・Terrain 更新・外部公開を個別クラスに分割 | 各クラスの変更範囲が限定される |
| **OCP（開放/閉鎖）** | WorldSceneStreamer のロード処理を置き換えるだけで Addressables に移行できる | 呼び出し元のコードを触らずに内部実装を交換できる |
| **イベント駆動** | OnSceneLoaded / OnSceneUnloaded / OnGridChanged | 外部システムが Hub を購読するだけで連携できる |
| **状態管理の明確化** | HashSet / Dictionary で「ロード済み・中・アンロード中」を厳密に管理 | 重複リクエストや競合状態を確実に防げる |
| **境界安定化** | 2 重円構造（loadRadius / unloadBuffer）| プレイヤーが境界を往復してもストリーミングが暴れない |
| **最小変更での拡張** | Hub を通じて外部から購読するだけ。Controller のコードを直接触らない | 敵スポーン・BGM・NavMesh 等を後付けしやすい |
