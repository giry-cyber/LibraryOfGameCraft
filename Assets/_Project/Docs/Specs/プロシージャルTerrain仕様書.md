# プロシージャルTerrainツール仕様書

# Version 1.3

---

# 1. 目的

Unity Terrain を対象とした **エディタ拡張ツール**を開発する。

目的は次の3点。

1. ノイズベースで自然な Terrain を生成
2. 手動編集を許可
3. 再生成しても手編集が破壊されない

---

# 2. 設計原則

本ツールは以下の設計思想に基づく。

### 2.1 プロシージャル + 手編集

最終高さは以下で定義される。

```
finalHeight =
    generatedHeight
  + manualDelta
```

* generatedHeight
  プロシージャル生成結果

* manualDelta
  手動編集差分

---

### 2.2 再生成の安全性

再生成時は

```
newGenerated + manualDelta
```

を使用する。

manualDelta は保持されるため
**再生成しても手編集が消えない**

---

### 2.3 永続化

以下のデータは **必ず永続化する**

* generatedHeightMap
* manualDeltaMap
* 各種マスク

---

# 3. Terrain タイル仕様

初期ターゲットは **500m × 500m タイル**

ただし設計上は可変とする。

```
tileSizeMeters
```

---

## 3.1 heightmapResolution

Unity 制約：

```
33
65
129
257
513
1025
2049
4097
```

初期推奨：

```
513
```

---

## 3.2 サンプル間隔

heightmap は

```
resolution = R
```

のとき

```
(R - 1) 分割
```

となる。

例

```
500m / 512 ≒ 0.98m
```

---

# 4. データ構造

---

# 4.1 TerrainPersistentData

ScriptableObject

役割

* メタデータ保持
* バイナリデータ参照

**float配列本体は保持しない**

---

### フィールド

```
terrainGuid
width
height

generatedHeightPath
manualDeltaPath
protectedMaskPath
noVegetationMaskPath
flattenMaskPath
```

---

# 4.2 バイナリデータ

形式

```
float32 array
little-endian
```

---

保存場所

```
Assets/TerrainToolData/{terrainGuid}/
```

---

# 5. マップ一覧

| 名前                 | 内容      |
| ------------------ | ------- |
| generatedHeightMap | ノイズ生成結果 |
| manualDeltaMap     | 手編集差分   |
| protectedMask      | 再生成保護   |
| noVegetationMask   | 植生禁止    |
| flattenMask        | 平坦化領域   |

---

# 6. ノイズシステム

---

# 6.1 INoise2D

```
interface INoise2D
{
    float Sample(float x, float z);
}
```

---

# 6.2 ノイズ生成

ノイズは **ワールド座標基準**

```
height = noise.Sample(worldX, worldZ)
```

---

# 6.3 seed

seed は Factory で注入する。

```
interface INoiseFactory
{
    INoise2D Create(int seed);
}
```

---

# 7. Domain Warp

Domain Warp は optional。

```
Vector2 warp = warpNoise.Warp(x,z)
height = noise.Sample(x+warp.x,z+warp.y)
```

---

# 8. タイル境界

境界一致のため

```
worldX =
tileOriginX +
(indexX / (resolution - 1)) * tileSize
```

---

これにより
隣接タイルは同一 worldX を参照する。

---

# 9. Phase構成

| Phase | 内容        |
| ----- | --------- |
| 1     | Terrain生成 |
| 2A    | 数値編集      |
| 2B    | ブラシ編集     |
| 3     | マスク       |
| 4A    | テクスチャ + 草 |
| 4B    | 木         |

---

# 10. Phase1

Terrain生成

---

## UI

Generate Window

```
seed
tileSizeMeters
heightScale
noiseScale
octaves
persistence
lacunarity
```

---

## 受け入れ条件

* Terrain が生成される
* heightmap が保存される
* world座標サンプリングを使用する

---

# 11. Phase2A

数値編集

---

## 編集モード

| モード     | 内容   |
| ------- | ---- |
| Raise   | 高さ追加 |
| Lower   | 高さ減少 |
| Flatten | 平坦化  |

---

## Flatten

```
targetHeightMeters
```

指定値に平坦化

---

## falloff

SmoothStep

```
t = distance/radius
weight = 1 - (t*t*(3-2*t))
```

---

# 12. Phase2B

ブラシ編集

SceneView 上のブラシ

---

ブラシ要素

```
radius
strength
falloff
mode
```

---

# 13. Phase3

マスク

---

| マスク              | 用途    |
| ---------------- | ----- |
| protectedMask    | 再生成保護 |
| noVegetationMask | 植生禁止  |
| flattenMask      | 平坦地   |

---

## Overlay

マスクは

**Terrain XZ平面に投影**

screen-space overlay は使用しない。

---

# 14. Phase4A

Texture + Grass

---

# 14.1 傾斜計算

中央差分

```
dx =
(h[x+1]-h[x-1]) / 2

dz =
(h[z+1]-h[z-1]) / 2
```

傾斜

```
atan(sqrt(dx² + dz²))
```

単位

```
degree
```

---

# 14.2 Texture Rule

```
minHeight
maxHeight
minSlope
maxSlope
blendStrength
```

---

# 14.3 alphamap

各ルール weight を合計

正規化

---

# 14.4 Grass

Grass は

```
detail layer density map
```

で生成

---

条件

```
height
slope
noVegetationMask
```

---

# 15. Phase4B

Tree配置

Tree は

```
treeInstances
```

を使用

---

## 配置方法

確率付きグリッド

```
grid cellごとに
条件判定
乱数配置
```

---

将来

```
Poisson Disk
```

可能

---

# 16. クラス構成

---

# TerrainBuildService

全体オーケストレーション

---

# TerrainGenerator

heightmap生成

---

# TexturePainter

alphamap生成

---

# VegetationScatter

Tree / Grass配置

---

# TerrainApplier

Unity Terrain に適用

---

# 17. 保存仕様

---

float配列

```
width * height
```

---

例

513x513

```
263169 floats
```

約

```
1MB
```

---

# 18. Terrainリネーム

保存フォルダは

```
terrainGuid
```

ベース

名前変更に影響されない。

---

# 19. スケーラビリティ

本ツールは

```
tileSizeMeters
```

変更可能

---

推奨プリセット

Small

```
250m
257 resolution
```

Medium

```
500m
513 resolution
```

Large

```
1000m
1025 resolution
```

---

巨大地形は

```
複数タイル
```

で構築する。

---

# 20. パフォーマンス目標

---

513 terrain

生成時間

```
< 1秒
```

---

Texture + vegetation

```
< 4秒
```

---

# 総評

この v1.3 仕様は

* 仕様矛盾なし
* Unity制約考慮
* 実装可能
* 拡張可能

という状態です。

つまり

**「設計レビューが通る仕様」**

になっています。

---

## v1.3.1 追加確定事項

---

## 21. Phase 2A の操作対象範囲

Phase 2A は **数値指定の局所編集** です。全Terrain一括ではありません。

## 入力項目

```
centerX, centerZ
shapeType       // Circle or Rectangle
radius          // Circle のとき
width, height   // Rectangle のとき
strengthMeters
targetHeightMeters  // Flatten のとき
falloff
```

## 補足

* Phase 2A と 2B は別UIだが、**どちらも同じ `manualDeltaMap` に書き込む**
* Phase 2A は「ブラシなしの範囲編集」
* Phase 2B は「SceneView ブラシ編集」

---

## 22. Phase 3 マスク編集方法

## Phase 3A（必須）

数値指定による範囲編集

```
centerX, centerZ
shapeType       // Circle or Rectangle
radius または width/height
maskValue       // 0.0〜1.0
falloff
```

対象マスク: `protectedMask` / `noVegetationMask` / `flattenMask` のいずれかを選択

## Phase 3B（拡張・後回し可）

SceneView ブラシ編集

```
brush radius
strength
selectedMaskType
mode    // Add or Subtract
```

## 可視化

Terrain XZ 平面に対応したワールド空間プレビュー

---

## 23. Phase 4B Tree Prototype 設定方法

`TerrainGenerationProfile` に `TreePrototypeRule` のリストを保持する。

## TreePrototypeRule フィールド

```
prefab
bendFactor
minHeight, maxHeight
minSlopeDeg, maxSlopeDeg
densityPer100m2
randomScaleMin, randomScaleMax
randomRotationY
colorJitter
heightScaleJitter
```

## グリッド cell サイズ

```
cellSizeMeters = sqrt(100 / densityPer100m2)
```

例:

* 4本/100m² → cellSize = 5m
* 25本/100m² → cellSize = 2m

## 初期推奨上限

* `densityPer100m2 <= 1.0` を推奨

## 適用方法

* Terrain に存在しない TreePrototype は生成時に自動追加
* 各ルールごとに候補点を生成し、条件を満たした点に `TreeInstance` を作る

---

## 24. Domain Warp インターフェース

```csharp
public interface IDomainWarp2D
{
    Vector2 Warp(float x, float z);
}

public interface IDomainWarp2DFactory
{
    IDomainWarp2D Create(int seed);
}
```

## 挙動

```
offset = warp.Warp(x, z)
height = baseNoise.Sample(x + offset.x, z + offset.y)
```

## 備考

* Domain Warp は Phase 1 optional
* `useDomainWarp == false` のとき UI 項目は無効表示

---

## 25. Editor Window 構成

クラス名: `TerrainToolWindow`

単一ウィンドウ＋タブ構成

## タブ一覧

| タブ名 | 内容 |
| --- | --- |
| Generate | Phase 1: Terrain生成 |
| Edit | Phase 2A/2B: 数値・ブラシ編集 |
| Mask | Phase 3A/3B: マスク編集 |
| Texture | Phase 4A: テクスチャ |
| Vegetation | Phase 4A/4B: 草・木 |
| Persistence | Save/Load/Apply 操作 |
| Debug | 可視化デバッグ |

## SceneView 連携

SceneView ブラシは TerrainToolWindow の補助機能として実装する

---

## 26. マスクの float 解釈

保存形式はすべて float32。意味はマスクごとに定義する。

| マスク | 値域 | 意味 |
| --- | --- | --- |
| protectedMask | 0.0〜1.0 | 0=保護なし、1=完全保護（blend mask） |
| noVegetationMask | 0.0〜1.0 | 0=配置可、1=配置禁止（将来は密度減衰マスクに拡張可） |
| flattenMask | 0.0〜1.0 | 平坦化影響度（blend mask） |

binary 専用型にはしない。将来の拡張性を確保する。

---

## 27. 保存タイミング

明示的 Save ボタン方式

### 操作定義

| 操作 | 内容 |
| --- | --- |
| Apply | メモリ上のデータを Unity TerrainData に反映 |
| Save | `.bytes` ファイルに書き出し（永続化） |
| Load | `.bytes` ファイルから再読込 |

### Dirty 状態管理

* 未保存変更がある場合、ウィンドウ上に警告表示
* 自動保存は初期実装では行わない

### 将来拡張

* Auto Save on Apply
* Auto Save every N seconds

---

## 総評（v1.3.1）

この v1.3.1 仕様は v1.3 の未定義部分をすべて確定させたものです。

* Phase 1: 即着手可能
* Phase 2A: 即着手可能
* Phase 2B: SceneView ブラシとして実装可能
* Phase 3A: 即着手可能
* Phase 3B: Phase 3A 完了後に追加
* Phase 4A/4B: Profile ベースで設計可能

---
