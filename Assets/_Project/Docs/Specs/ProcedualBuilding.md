# プロシージャル建物生成ツール仕様書

Version 1.5（配置式・confidence・TrimWall 修正版 / v0.1 実装仕様）

---

# 1. 目的

Unity 上で建物を効率的に制作するための **半自動プロシージャル建物生成ツール** を開発する。

ユーザーは **ProBuilder を用いて建物のラフ形状（Shape）を作成**し、
ツールが形状を解析して **建築的意味（Semantic）を推定**する。
その後、必要に応じてユーザーが意味づけを修正し、
最終的に **壁・屋根などのモジュールを自動配置**する。

本ツールの目的は以下とする。

* 建物制作効率の向上
* プロシージャル生成による量産
* 手動編集との共存
* 建築生成アルゴリズムのナレッジ蓄積

---

# 2. 基本設計

## 2.1 半自動生成

本ツールは **完全自動生成ではなく半自動生成** とする。

理由:

* 建築構造は形状だけでは判断できない場合がある
* 建物の意味（入口、装飾など）はユーザー判断が必要

## 2.2 編集可能設計

生成後も以下の編集を可能とする。

* 個別パーツ差し替え
* 手動配置
* Semantic変更
* 再生成

生成結果は単なる見た目ではなく、
**意味情報付き生成データ** として扱う。

## 2.3 フェーズ構造

```text id="c3ajvb"
Shape → Semantic → Generate → PostEdit → Bake
```

| Phase    | 役割      |
| -------- | ------- |
| Shape    | 建物形状作成  |
| Semantic | 建築意味推定  |
| Generate | モジュール配置 |
| PostEdit | 生成後編集   |
| Bake     | 最終出力    |

## 2.4 ナレッジ保存方針

Unity 実装は制限せず、
アルゴリズム・データ構造・ワークフロー・採用理由を記録する。

---

# 3. Shape フェーズ

## 3.1 概要

ユーザーが ProBuilder で建物のラフ形状を作る段階。

## 3.2 v0.1 対応形状

* 直方体
* 単純閉ボリューム

## 3.3 ShapeSource 抽象

```text id="zk0ie4"
ShapeSource
 ├ ProBuilderShape
 ├ MeshShape
 └ BlenderShape (future)
```

## 3.4 座標系

以下は判定前にワールド空間へ変換する。

* `faceNormal`
* `faceCenter`
* `Bounds`

---

# 4. Semantic フェーズ

## 4.1 法線による一次分類

| 条件                    | 分類           |
| --------------------- | ------------ |
| `normal.y ≥ 0.9`      | UpwardFace   |
| `normal.y ≤ -0.9`     | DownwardFace |
| `abs(normal.y) ≤ 0.2` | VerticalFace |
| その他                   | SlopedFace   |

## 4.2 SlopedFace

v0.1 では可視化のみ対象、生成対象外。

## 4.3 Roof / Floor 判定

```text id="7wdo2k"
maxY = max(face.center.y)
if face.center.y >= maxY - roofHeightEpsilon
    → Roof
else
    → Floor
```

* `roofHeightEpsilon = 0.05`

## 4.4 CeilingCandidate

`DownwardFace` は `CeilingCandidate` とする。
`generateCeiling = true` の場合のみ生成対象。

## 4.5 OuterWall / InnerWall 判定

```text id="k2i4ms"
dir = normalize(faceCenter - buildingCenter)
dot = dot(faceNormal, dir)
```

| 条件        | 分類                 |
| --------- | ------------------ |
| `dot > 0` | OuterWall          |
| `dot < 0` | InnerWallCandidate |

* `buildingCenter` は Shape 全体のワールド空間 AABB.center
* v0.1 の壁生成対象は OuterWall のみ

## 4.6 Semantic 可視化

| Semantic           | Color   |
| ------------------ | ------- |
| OuterWall          | Red     |
| InnerWallCandidate | Magenta |
| Floor              | Green   |
| Roof               | Blue    |
| CeilingCandidate   | Cyan    |
| SlopedFace         | Gray    |
| OpeningHost        | Yellow  |

### Uncertain 表示

* `confidence < 0.75` の面を Uncertain とする
* ベース色は Semantic 色を使用する
* Uncertain 面には **Orange の面アウトライン** を追加描画する
* Orange は Semantic 色を上書きしない
* v0.1 では「追加ハイライト」は採用せず、**アウトライン方式に統一**する

### 表示条件

* 対象 `BuildingAuthoring` がアクティブ
* `showSemanticOverlay = true`
* かつ

  * `BuildingAuthoring` が選択中
  * または Semantic EditorWindow が対象を編集中

## 4.7 手動修正 UI

方式: **EditorWindow + Inspector**

## 4.8 Semantic データの永続化

Semantic データは `BuildingAuthoring` 配下の `SemanticStore` に Scene シリアライズする。

### `FaceSemanticRecord`

* `sourceId`
* `semantic`
* `isManualOverride`
* `confidence`
* `geometrySignature`

### confidence

* 明確な自動分類面 → `1.0`
* 再マッピング成功だがやや不安定 → `0.75`
* 曖昧分類 / 再マッピング失敗後の自動再分類 → `0.5`
* 手動修正済み面 → `1.0`

### geometrySignature

`geometrySignature` は量子化済み幾何特徴セット。

#### 構成

* `quantizedCenter`
* `quantizedNormal`
* `quantizedArea`
* `quantizedBoundsSize`

#### 量子化単位

* `quantizedCenter`: 0.05
* `quantizedNormal`: 0.1
* `quantizedArea`: 0.05
* `quantizedBoundsSize`: 0.05

#### quantizedBoundsSize

`Bounds.size.x/y/z` を各軸ごとに量子化した 3 次元ベクトル。

#### 量子化式

各成分は以下で整数ビンへ変換する。

```text id="hzk4ju"
bin = RoundToInt(value / step)
```

#### 役割

* `geometrySignature` は候補検索用
* 最終一致判定は 9章の複合スコアで行う

---

# 5. GenerationSettings

## 5.1 保持場所

`BuildingAuthoring` のシリアライズフィールドとして保持する。

## 5.2 保持項目

```text id="4yyzyf"
floorCount
floorHeight
baseElevation
roofHeightEpsilon
minLastFloorHeightRatio
generateCeiling
maxTrimCoverageRatio
```

### デフォルト値

```text id="xzmkhe"
roofHeightEpsilon = 0.05
minLastFloorHeightRatio = 0.5
generateCeiling = false
maxTrimCoverageRatio = 0.25
```

## 5.3 baseElevation

フロア境界計算の基準 Y 座標。
未指定時は Shape 全体の `Bounds.min.y` を使用。

## 5.4 フロア境界

Shape から自動推定せず、GenerationSettings により定義する。

## 5.5 実メッシュ高さ不一致

最終階で差分吸収。
Warning は出すが生成は継続する。

---

# 6. Generate フェーズ

## 6.1 v0.1 の生成対象

* 壁
* 屋根
* 床（簡易メッシュ）
* 任意の天井（簡易メッシュ）

## 6.2 OpeningHost

保持と可視化のみ。Generate では未使用。

## 6.3 床・天井の簡易メッシュ生成

* 床: `Floor` 面ごとに生成
* 天井: `generateCeiling = true` の場合のみ `CeilingCandidate` 面ごとに生成
* `generateCollider = true` の場合は非 Trigger の MeshCollider を付与

## 6.4 壁グリッド分割

* `wallModuleWidth = PrimaryWallModule.nominalSize.x`
* `wallModuleHeight = floorHeight`

### 壁面の幅・高さ

壁面頂点群を `right/up` 軸へ投影して求める。

```text id="85ubta"
forward = faceNormal
up = Vector3.up
right = normalize(cross(up, forward))
u = dot(v, right)
h = dot(v, up)
width  = max(u) - min(u)
height = max(h) - min(h)
```

### 左端・下端

* 左端 = `min(u)`
* 下端 = `min(h)`

### 面奥行きオフセット

`faceNormal` の Y 成分による垂直位置汚染を避けるため、
奥行き計算には `faceNormal` の XZ 投影を使用する。

任意の面頂点 `p0` に対して

```text
forwardXZ      = normalize(Vector3(faceNormal.x, 0, faceNormal.z))
faceDepthOffset = forwardXZ * dot(p0, forwardXZ)
```

### 壁セルの配置基準

* `cellMinY` はセル下端の理論 Y
* `placementY` は実配置用の Y
* v0.1 の壁モジュールでは **`placementY = cellMinY`** とする
* v0.1 では壁モジュールに `offset` は適用しない
* `offset` は床・天井の簡易メッシュ生成にのみ適用する

```text
cellMinY   = baseElevation + floorIndex * floorHeight
placementY = cellMinY
```

### ピボット配置位置

列 `col` の壁モジュール Pivot 世界座標は以下の 3 成分を直接合算する。
Y 成分は `Vector3.up * placementY` のみで決定し、汚染を排除する。

```text
pivotWorldPos =
    faceDepthOffset
    + right * (min(u) + col * wallModuleWidth + wallModuleWidth * 0.5)
    + Vector3.up * placementY
```

## 6.5 横方向グリッド起点

対象面を外側から見た左端を起点に、右方向へ配置する。
余りは右端で吸収する。

## 6.6 余り処理

### Step 1: AdjustableWall

* `AdjustableWall` が存在
* 必要スケールがその `scaleRange` 内

### Step 2: TrimWall

* `TrimWall` が存在
* 余り量が `maxTrimCoverageRatio * wallModuleWidth` 以下

TrimWall の配置ルール:

* 配置位置: **右端揃え**（余りスペースの右端に揃えて配置）
* 使用幅: `TrimWall.nominalSize.x`（固定幅、スケールしない）
* TrimWall は余りスペースを視覚的に隠すだけでよく、サイズ完全一致は不要
* TrimWall の幅が余りスペースより大きい場合（はみ出す場合）は採用しない

### Step 3: 軽微スケール

対象: `PrimaryWallModule`

* `PrimaryWallModule.allowScale = true`
* 必要スケールがその `scaleRange` 内

### 補足

* `PrimaryWallModule` は原則 `allowScale = false`
* Step 3 は任意救済手段

### 失敗時

* Warning を表示
* その区画は未生成

## 6.7 屋根モジュール配置

* Roof 面ごとに `FlatRoof` を 1 枚配置
* 面サイズに合わせてフィット
* 壁のようなグリッド分割はしない

## 6.8 モジュール配置回転

### 壁

* `+Z = faceNormal`
* `+Y = worldUp`

### 屋根

* `+Y = worldUp`
* `+Z = BuildingAuthoring.transform.forward` の水平投影方向
* 水平投影がゼロに近い場合は `Vector3.forward`

---

# 7. モジュール管理

## 7.1 ModuleRole

有効値:

* `PrimaryWall`
* `AdjustableWall`
* `TrimWall`
* `FlatRoof`

## 7.2 BuildingModuleCatalog

* `PrimaryWallModule`
* `WallModules`
* `RoofModules`
* `FloorSettings`
* `CeilingSettings`

## 7.3 FloorSettings / CeilingSettings

* `material`
* `generateCollider`
* `thickness`
* `offset`

### offset

面法線方向のスカラーオフセット量。

## 7.4 BuildingModuleEntry

```text id="r7prb2"
moduleId
role : ModuleRole
prefab
nominalSize
adjustableAxes
allowScale
scaleRange
priority
```

## 7.5 モジュールアセット規約

### 壁

* Pivot: 底辺中央
* `+Y = 上`
* `+Z = 前`

### 屋根（FlatRoof）

* Pivot: 底面中心
* `+Y = 上`
* `+Z = 前`
* `allowScale = true`
* `adjustableAxes = XZ`

### nominalSize

壁:

* `X = 幅`
* `Y = 高さ`
* `Z = 厚み`

屋根:

* `X = 幅`
* `Y = 厚み`
* `Z = 奥行き`

---

# 8. 生成結果管理

## 8.1 Scene 管理方式

Scene 内 MonoBehaviour で保持する。

## 8.2 構造

```text id="p0x97j"
BuildingAuthoring
 ├ SemanticStore
 ├ GeneratedObjects
 └ GeneratedObjectRegistry
```

## 8.3 GeneratedElement

```text id="3v74wj"
elementId
sourceKind
sourceId
role
moduleAssetId
generationGroup
isLocked
bounds
```

### sourceId の共有

同一解析サイクル内で、

* `FaceSemanticRecord.sourceId`
* `GeneratedElement.sourceId`
  は同じ一時整数 ID を共有する。

## 8.4 GeneratedObjectRegistry

* `elementId -> GameObject`
* `GameObject -> elementId`
* 生成 Root
* 削除 / 再生成時の参照解決

## 8.5 Rebuild All 時の更新

1. 旧生成オブジェクト破棄
2. Registry クリア
3. Shape 再解析
4. Semantic 再マッピング
5. 新規 GeneratedElement 作成
6. 新規 `elementId` 採番
7. 新規 GameObject 生成
8. Registry 構築

---

# 9. Semantic 再マッピング

## 9.1 方針

ProBuilder の face index は永続 ID として使わない。
Semantic の引き継ぎは `SemanticStore` を入力として行う。

## 9.2 入出力

### 入力

* Rebuild 前の `SemanticStore.records`
* 再解析後の新 Face 群

### 出力

* 新 Face 群に対応した新しい `SemanticStore.records`

## 9.3 再マッピング手順

1. 旧 `SemanticStore.records` を保持
2. Shape を再解析して新 Face 群を作る
3. 各新 Face に対して

   * 新 `sourceId`
   * 新 `geometrySignature`
   * `faceCenter / faceNormal / faceArea / Bounds`
     を計算
4. 旧 record 群から `geometrySignature` 近傍候補を探す
5. 候補群に対して複合スコアを計算
6. 一意に対応成功した場合、旧 record の

   * `semantic`
   * `isManualOverride`
     を引き継ぐ

   `confidence` は以下のルールで更新する。

   * `isManualOverride = true` の場合 → `1.0`（手動修正済みは格下げしない）
   * `isManualOverride = false` の場合 → `0.75`（再マッピング成功・やや不安定）
7. 失敗時は自動分類結果を設定
8. 新しい record 群で `SemanticStore.records` を全面更新

## 9.4 geometrySignature 近傍検索

### Phase A: 完全一致検索

`quantizedCenter + quantizedNormal + quantizedArea` の完全一致で候補を探す。

### Phase B: 近傍ビン検索

完全一致候補が 0 件のときのみ、
上記3要素に対して ±1 bin を許可した候補検索を行う。

### quantizedBoundsSize の使い方

`quantizedBoundsSize` は **主検索キーではなく後フィルタ** として使う。

* 主検索で得た候補に対し
* `Bounds.size.x/y/z` 各軸について **±1 bin 以内**
  のものだけ残す

### 性能方針

v0.1 の対象面数規模では、±1 bin 検索の計算量は Editor ツール用途として許容する。

## 9.5 候補絞り込み閾値

* `maxNormalAngleDeg = 5.0`
* `maxCenterDistance = 0.25`
* `maxAreaRelativeDiff = 0.15`

## 9.6 線形正規化

```text id="1crj5a"
centerDistanceScore = clamp01(distance / maxCenterDistance)
normalDifferenceScore = clamp01(angleDeg / maxNormalAngleDeg)
areaDifferenceScore = clamp01(relativeAreaDiff / maxAreaRelativeDiff)
```

## 9.7 複合スコア

```text id="q8rf30"
totalScore =
    0.5 * centerDistanceScore +
    0.3 * normalDifferenceScore +
    0.2 * areaDifferenceScore
```

## 9.8 一意性チェック

```text id="v5nqtl"
equivalentScoreDelta = 0.05
```

## 9.9 失敗時

* Semantic を Auto に戻す
* `confidence = 0.5`
* 要確認表示

## 9.10 実行タイミング

再マッピングは **Rebuild All 時のみ** 実行する。

---

# 10. 再生成

v0.1 は `Rebuild All` のみ。

---

# 11. Bake

v0.1 では未実装。

---

# 12. v0.1 実装範囲

* 直方体対応
* Face 自動分類
* Semantic 可視化
* Semantic 修正 UI
* SemanticStore 永続化
* 壁モジュール生成
* 屋根モジュール生成
* 床メッシュ生成
* 任意の天井メッシュ生成
* Rebuild All
* ModuleCatalog 参照
* フロア境界設定
* 再マッピング
* Registry 再構築

---

# 13. 将来拡張

## v0.2

* 窓
* ドア
* 開口
* Bake 実装

## v0.3

* 部分再生成
* generationGroup 活用
* 手編集保護

## v0.4

* 三角柱
* 切妻屋根
* アーチ
* 階段

---

# 14. 整合性確認メモ

本版では追加で以下を固定した。

* `quantizedBoundsSize` を後フィルタ用途に限定
* Uncertain は Orange 上書きではなくアウトライン重ね表示
* 量子化式を `RoundToInt(value / step)` に固定
* ±1 bin 検索は v0.1 面数規模では許容と明記
* `faceBasePoint` を廃止し `faceDepthOffset + right + Vector3.up` の 3 成分直接合算式に置換
* `forwardXZ` 投影により `faceNormal.y` による Y 成分汚染を完全排除
* `placementY` と `cellMinY` の関係を明確化
* Uncertain 可視化は Orange アウトラインに固定
* 再マッピング成功後の `confidence` 更新ルールを `isManualOverride` で分岐して明記
* `TrimWall` の配置位置（右端揃え）・使用幅（`nominalSize.x` 固定）・はみ出し時の非採用を定義

---
