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
