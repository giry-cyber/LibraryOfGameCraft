# プロシージャルTerrain ツール マニュアル — Phase 1

## 概要

Unity Terrain をノイズベースで自動生成するエディタ拡張ツールです。
Phase 1 では **Terrain のハイトマップ生成・適用・保存** までを扱います。

---

## 事前準備

### 1. TerrainGenerationProfile を作成する

プロジェクトウィンドウで右クリック →
`Create > LibraryOfGamecraft > Terrain > Generation Profile`

生成パラメータを保存する ScriptableObject です。
プリセットとして複数作成・切り替えが可能です。

### 2. TerrainPersistentData を作成する

同様に右クリック →
`Create > LibraryOfGamecraft > Terrain > Persistent Data`

ハイトマップの保存先パスを管理します。
Terrain ごとに 1 つ用意してください。

---

## ウィンドウを開く

メニューバー → `Tools > LibraryOfGamecraft > Terrain Tool`

---

## Generate タブの使い方

### Profile セクション

| 操作 | 内容 |
| --- | --- |
| Profile フィールド | 使用する `TerrainGenerationProfile` を設定 |
| New Profile | 新規 Profile アセットを作成してそのまま使用 |
| Duplicate Profile | 現在の Profile を複製（パラメータを試すときに使用） |

Profile が未設定の場合は Generate できません。

---

### Profile パラメータ

| パラメータ | 説明 | 初期値 |
| --- | --- | --- |
| **seed** | 乱数シード。同じ値なら同じ地形が生成される | 42 |
| **tileSizeMeters** | タイルのワールドサイズ（メートル） | 500 |
| **heightmapResolution** | heightmap の解像度。Unity 制約: 33/65/129/257/513/1025/2049/4097 | 513 |
| **heightScale** | 高さの最大値（メートル） | 100 |
| **noiseScale** | ノイズのサンプリングスケール。小さいほど広域・なだらか | 0.003 |
| **octaves** | fBm のオクターブ数。多いほど細かい起伏が加わる | 6 |
| **persistence** | 各オクターブの振幅減衰率（0〜1） | 0.5 |
| **lacunarity** | 各オクターブの周波数倍率 | 2.0 |
| **useDomainWarp** | Domain Warp を有効にするか | false |
| **domainWarpStrength** | Warp の変位量（メートル相当） | 30 |
| **domainWarpScale** | Warp ノイズのスケール | 0.002 |

#### パラメータ調整のヒント

- **なだらかな平原**: `noiseScale` を小さく（0.001〜0.002）、`octaves` を少なく（3〜4）
- **険しい山岳**: `noiseScale` を大きく（0.005〜0.01）、`octaves` を多く（6〜8）、`persistence` を高く（0.6〜0.7）
- **自然な歪み**: `useDomainWarp` を有効にすると川筋・断崖のような有機的な形状になる

---

### Target Terrain セクション

| フィールド | 内容 |
| --- | --- |
| **Terrain** | シーン上の `Terrain` オブジェクトを設定 |
| **Persistent Data** | 上記で作成した `TerrainPersistentData` を設定 |
| **Origin X / Z** | タイルのワールド原点（複数タイル配置時に使用、通常は 0） |

---

### Generate ボタン

Terrain と Persistent Data が両方設定されると押せるようになります。

押すと以下の処理が順番に実行されます。

```text
1. Profile のパラメータでノイズを構築
2. ハイトマップ（float配列）を生成
3. 既存の manualDelta を読み込み（あれば）
4. finalHeight = generatedHeight + manualDelta を Unity Terrain に適用
5. generatedHeight を .bytes ファイルに保存
6. manualDelta が存在しない場合はゼロ配列を新規保存
```

完了すると Console に `[TerrainTool] Generate 完了` と表示されます。

---

## 保存ファイルについて

ハイトマップは `Assets/TerrainToolData/{GUID}/` 以下に保存されます。
`TerrainPersistentData` に GUID を設定し `SetPaths()` を呼ぶことで、保存先が自動決定されます。

> **注意**: `TerrainPersistentData` の GUID が未設定のままだとパスが空になり、保存されません。
> Inspector で `terrainGuid` に任意の文字列（例: `tile_00_00`）を入力したあと、
> Inspector 右上の **⋮ メニュー（または右クリック）→ Apply terrainGuid to Paths** を実行してください。
> 全パスフィールドが自動入力されます。

| ファイル | 内容 |
| --- | --- |
| `generated.bytes` | ノイズ生成結果（float32 配列） |
| `manualDelta.bytes` | 手動編集差分（Phase 2 で使用） |
| `protectedMask.bytes` | 再生成保護マスク（Phase 3 で使用） |
| `noVegetationMask.bytes` | 植生禁止マスク（Phase 3 で使用） |
| `flatten.bytes` | 平坦化マスク（Phase 3 で使用） |

---

## 再生成しても手編集が消えないしくみ

```
finalHeight = generatedHeight + manualDelta
```

Generate を実行するたびに `generatedHeight` は新しく計算されますが、
`manualDelta`（Phase 2 で加えた手動の高さ差分）はファイルから読み込まれ保持されます。
seed を変えて地形を作り直しても、手編集した盛り土や平坦化は維持されます。

---

## クイックスタート手順

1. `TerrainGenerationProfile` を作成して Profile フィールドに設定する
2. `TerrainPersistentData` を作成し、Inspector で `terrainGuid` に任意の文字列を入力する（例: `tile_00_00`）
3. `TerrainPersistentData` の Inspector を右クリック → **Apply terrainGuid to Paths** を実行する
4. シーンに Terrain を配置し、Terrain フィールドに設定する
5. **Generate** を押す

---

## Batch タブの使い方

複数タイルを一括で Generate する機能です。タイルごとにシーンを開いて Generate → 保存 → 次へ を自動で繰り返します。

### Batch 事前準備

Batch タブを使う前に、全タイルのシーンと PersistentData が用意されている必要があります（Generate タブの事前準備と同じ）。

加えて `TerrainBatchConfig` を作成します。

> Project ウィンドウ右クリック →
> `Create > LibraryOfGamecraft > Terrain > Batch Config`

### Batch Config の設定

| フィールド | 内容 |
| --- | --- |
| **Batch Config** | 作成した `TerrainBatchConfig` を設定 |
| **Profile** | 全タイル共通の `TerrainGenerationProfile` |
| **Tiles リスト** | タイルエントリを追加して各フィールドを設定 |

**タイルエントリのフィールド:**

| フィールド | 内容 | 例 |
| --- | --- | --- |
| label | タイルの識別名（ログに表示） | `tile_00_00` |
| Scene Path | シーンのアセットパス | `Assets/_Project/Scenes/SubScene_tile_00_00.unity` |
| Tile Origin | このタイルの tileOrigin | `(0, 0)` |
| Persistent Data | このタイル専用の PersistentData | |

### Generate All ボタン

全タイルのエントリが設定されたら **Generate All (N tiles)** を押します。

```text
1. 現在のシーンを記憶
2. 各タイルのシーンを順番に開く
3. シーン内の Terrain を自動検出
4. Generate 実行 → シーンを保存
5. 全タイル完了後、元のシーンに戻る
```

プログレスバーが表示され、途中でキャンセルも可能です。
各タイルの完了は Console に `[BatchGenerate] tile_XX_XX 完了` と表示されます。

> **注意**: Batch 実行中は Unity Editor の操作ができません。タイル数が多い場合は時間がかかります。

---

## 変更履歴

| 日付 | 内容 |
| --- | --- |
| 2026-03-31 | Phase 1 初版作成 |
| 2026-04-02 | Batch タブ追加 |
