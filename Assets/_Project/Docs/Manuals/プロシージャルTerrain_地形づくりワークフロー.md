# プロシージャルTerrain 地形づくりワークフロー

---

## 全体像

```
[1. 設計]         グリッド構成を決める
    ↓
[2. アセット準備]  タイルごとにアセットを用意する
    ↓
[3. シーン準備]    SubScene を作り Terrain を配置する
    ↓
[4. 生成]         TerrainToolWindow で Generate する
    ↓
[5. 確認・調整]   パラメータを変えて再生成する
```

---

## 1. グリッド設計

タイルサイズとグリッド構成を決める。

```
例: 3×3 タイル、各 500m

(0,0)     (500,0)   (1000,0)
  □ ─────── □ ─────── □
  │         │         │
  □ ─────── □ ─────── □
  │         │         │
  □ ─────── □ ─────── □
(0,1000)          (1000,1000)
```

タイル名のルール例: `tile_{x}_{z}`（例: `tile_00_00`、`tile_01_00`）

---

## 2. アセット準備

タイルごとに 3 種類のアセットを `Assets/_Project/Terrain/` 以下に用意する。

### 2-1. TerrainGenerationProfile（全タイル共通）

`Assets/_Project/Terrain/Profiles/` に 1 つ作成。
複数タイルで共有してよい。

> Project ウィンドウ右クリック →
> `Create > LibraryOfGamecraft > Terrain > Generation Profile`

### 2-2. TerrainData（タイルごと）

`Assets/_Project/Terrain/TerrainData/` にタイル数分作成。

> Project ウィンドウ右クリック →
> `Create > 3D Object > Terrain Layer`... ではなく
> シーンに Terrain を作ると自動生成される → それを移動する（後述）

### 2-3. TerrainPersistentData（タイルごと）

`Assets/_Project/Terrain/PersistentData/` にタイル数分作成。

> Project ウィンドウ右クリック →
> `Create > LibraryOfGamecraft > Terrain > Persistent Data`

作成後、Inspector で `terrainGuid` を入力（例: `tile_00_00`）→
右クリック → **Apply terrainGuid to Paths** を実行。

---

## 3. シーン準備

### 3-1. 最初のタイルのシーンを作る

1. シーンに `Terrain` を新規作成（`GameObject > 3D Object > Terrain`）
2. 自動生成された `TerrainData` を `Assets/_Project/Terrain/TerrainData/` に移動してリネーム
3. Terrain の位置を tileOrigin に合わせる（例: `tile_00_00` なら Position `(0, 0, 0)`）

### 3-2. 2枚目以降のタイルは「シーンをコピー」ではなく個別に作る

> **注意**: シーンをコピーすると TerrainData が共有されてしまい、
> 一方を Generate したときにもう一方も変わってしまう。

各タイルを独立したシーンとして作成し、それぞれ専用の TerrainData を割り当てる。

```
SubScene_tile_00_00.unity → TerrainData_tile_00_00.asset
SubScene_tile_01_00.unity → TerrainData_tile_01_00.asset  ← 別アセット
```

---

## 4. 生成

`Tools > LibraryOfGamecraft > Terrain Tool` を開く。

### Generate タブの設定

| フィールド | 設定値 |
|---|---|
| Profile | 共通の TerrainGenerationProfile |
| Terrain | このタイルのシーン上の Terrain |
| Persistent Data | このタイル専用の TerrainPersistentData |
| Origin X | `タイルX番号 × tileSizeMeters`（例: 1列目なら `500`） |
| Origin Z | `タイルZ番号 × tileSizeMeters`（例: 2行目なら `1000`） |

**Generate** を押す。

### tileOrigin の早見表（tileSizeMeters = 500 の場合）

| タイル | Origin X | Origin Z |
|---|---|---|
| tile_00_00 | 0 | 0 |
| tile_01_00 | 500 | 0 |
| tile_02_00 | 1000 | 0 |
| tile_00_01 | 0 | 500 |
| tile_01_01 | 500 | 500 |

---

## 5. 確認・調整

### 境界の確認

隣接タイルを同時にシーンに開き、境界が一致しているか確認する。
ノイズはワールド座標基準なので数学的には一致しているが、
目視で違和感がある場合はノイズスケールや heightScale を調整する。

### パラメータを変えて再生成する

Profile のパラメータを変更 → Generate を押すだけで再生成できる。
手動編集（Phase 2）をしていない段階であれば何度でも試せる。

### 手動編集後に再生成する場合

`manualDelta`（手動差分）は保持されるため、
再生成しても手動編集は消えない。

---

## フォルダ構成まとめ

```
Assets/
├── _Project/
│   ├── Terrain/
│   │   ├── Profiles/
│   │   │   └── DefaultProfile.asset          ← 全タイル共通
│   │   ├── TerrainData/
│   │   │   ├── TerrainData_tile_00_00.asset  ← タイルごと
│   │   │   └── TerrainData_tile_01_00.asset
│   │   └── PersistentData/
│   │       ├── PersistentData_tile_00_00.asset
│   │       └── PersistentData_tile_01_00.asset
│   └── Scenes/
│       ├── SubScene_tile_00_00.unity
│       └── SubScene_tile_01_00.unity
└── TerrainToolData/                           ← 自動生成バイナリ
    ├── tile_00_00/
    │   ├── generated.bytes
    │   └── manualDelta.bytes
    └── tile_01_00/
        ├── generated.bytes
        └── manualDelta.bytes
```

---

## 変更履歴

| 日付 | 内容 |
|------|------|
| 2026-04-02 | 初版作成 |
