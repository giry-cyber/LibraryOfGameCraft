# プロシージャル建物生成ツール仕様書

**Version 0.8（v0.1 実装確定版）**

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

---

## 2.2 編集可能設計

生成後も以下の編集を可能とする。

* 個別パーツ差し替え
* 手動配置
* Semantic変更
* 再生成

生成結果は単なる見た目ではなく、
**意味情報付き生成データ** として扱う。

---

## 2.3 フェーズ構造

建物生成は以下のフェーズで構成される。

```text id="m5e2xl"
Shape → Semantic → Generate → PostEdit → Bake
```

| Phase    | 役割      |
| -------- | ------- |
| Shape    | 建物形状作成  |
| Semantic | 建築意味推定  |
| Generate | モジュール配置 |
| PostEdit | 生成後編集   |
| Bake     | 最終出力    |

---

## 2.4 ナレッジ保存方針

将来的に Blender 等で同様のツールを作る可能性はあるが、
本ツールでは **Unity 実装を制限しない**。

代わりに以下を記録対象とする。

* アルゴリズム
* データ構造
* ワークフロー
* Unity 固有実装を採用した理由

目的は **設計の可搬性の強制** ではなく、
**知識の再利用性の確保** である。

---

# 3. Shape フェーズ

## 3.1 概要

ユーザーが **ProBuilder を使って建物のラフ形状を作る段階**。
この段階では建築意味はまだ存在しない。

---

## 3.2 v0.1 対応形状

* 直方体
* 単純閉ボリューム

※ 三角柱、切妻屋根、L字建物などは v0.4 以降の拡張対象とする。

---

## 3.3 ShapeSource 抽象

```text id="ai16xr"
ShapeSource
 ├ ProBuilderShape
 ├ MeshShape
 └ BlenderShape (future)
```

v0.1 の主入力は ProBuilderShape だが、
内部の解析処理は ShapeSource から必要情報を取得する前提で実装する。

---

# 4. Semantic フェーズ

## 4.1 法線による面分類

| 条件                  | 分類           |
| ------------------- | ------------ |
| normal.y ≥ 0.9      | UpwardFace   |
| normal.y ≤ -0.9     | DownwardFace |
| abs(normal.y) ≤ 0.2 | VerticalFace |
| その他                 | SlopedFace   |

---

## 4.2 SlopedFace

SlopedFace は斜面を表す。

v0.1 では

* 自動生成対象外
* 手動分類対象
* 可視化のみ対象

とする。

将来的には屋根面候補として使用する。

---

## 4.3 Roof / Floor 判定

UpwardFace を以下のルールで Roof / Floor に分類する。

```text id="9wi88o"
maxY = max(face.center.y)
if face.center.y >= maxY - roofHeightEpsilon
    → Roof
else
    → Floor
```

### パラメータ

* `roofHeightEpsilon = 0.05`
* 保持場所: `GenerationSettings`

### 補足

* `maxY - roofHeightEpsilon` 以上の UpwardFace はすべて Roof とする
* 同一高さ帯に複数面がある場合はすべて Roof とする

---

## 4.4 Ceiling 候補

`DownwardFace` は v0.1 では **CeilingCandidate** として扱う。

### v0.1 の扱い

* 可視化では Ceiling 色を使用してよい
* 生成対象としては任意
* `GenerationSettings.generateCeiling = true` の場合のみ簡易メッシュ生成対象とする

---

## 4.5 OuterWall / InnerWall 判定

```text id="1l3m6l"
dir = normalize(faceCenter - buildingCenter)
dot = dot(faceNormal, dir)
```

| 条件      | 分類          |
| ------- | ----------- |
| dot > 0 | OuterWall   |
| dot < 0 | InnerWall候補 |

### buildingCenter

`buildingCenter` は **Shape 全体のワールド空間 AABB.center** とする。

### v0.1 の扱い

* OuterWall を主対象とする
* InnerWall は手動補正前提

---

## 4.6 Semantic 可視化

SceneView 上で **Handles / Gizmos** によるオーバーレイ表示を行う。

### 表示内容

* 面重心付近への半透明ポリゴンまたは輪郭描画
* Semantic に応じた色分け
* 要確認面の強調表示

### 色分け

| Semantic    | Color  |
| ----------- | ------ |
| Wall        | Red    |
| Floor       | Green  |
| Roof        | Blue   |
| Ceiling     | Cyan   |
| OpeningHost | Yellow |
| Uncertain   | Orange |

### 表示条件

以下をすべて満たす場合のみ描画する。

* 対象 `BuildingAuthoring` がアクティブ
* `showSemanticOverlay = true`
* かつ以下のいずれか

  * `BuildingAuthoring` が選択中
  * Semantic EditorWindow が開いており対象を編集中

### 注意

* ProBuilder メッシュ自体のマテリアルや頂点カラーは変更しない

---

## 4.7 手動修正 UI

v0.1 の実装方式は **EditorWindow + Inspector** とする。

### 操作

1. ProBuilder で面選択
2. Semantic EditorWindow で Semantic を変更
3. Apply 実行

### 変更可能項目

* FaceRole
* OpeningHost
* Auto / Manual override

---

# 5. GenerationSettings

## 5.1 概要

建物生成に必要なパラメータを保持する。

---

## 5.2 保持項目

```text id="x1h5uo"
floorCount
floorHeight
baseElevation
roofHeightEpsilon
minLastFloorHeightRatio
generateCeiling
maxTrimCoverageRatio
```

将来拡張:

```text id="wlhg4f"
storyHeights[]
topOffset
```

---

## 5.3 baseElevation の意味

`baseElevation` は **フロア境界計算の基準 Y 座標** とする。

### 使用方法

* Floor 0 下端 = `baseElevation`
* Floor 1 下端 = `baseElevation + floorHeight`
* Floor 2 下端 = `baseElevation + floorHeight * 2`

### デフォルト

未指定時は **Shape 全体の Bounds.min.y** を使用する。

### 注意

* `baseElevation` は buildingCenter 計算には使わない

---

## 5.4 フロア境界の扱い

v0.1 ではフロア境界は Shape から自動推定せず、
**GenerationSettings により定義する**。

### 例

```text id="xhobtu"
floorCount = 3
floorHeight = 3
baseElevation = 0
```

→ フロア境界

```text id="8jol58"
0-3m
3-6m
6-9m
```

---

## 5.5 実メッシュ高さと不一致な場合

実際のメッシュ高さが `floorCount * floorHeight` と一致しない場合、
v0.1 では **最終階で差分を吸収**する。

### 例

* `floorCount = 3`
* `floorHeight = 3.0`
* 実高さ = `8.7`

→

* 1階 = 3.0
* 2階 = 3.0
* 3階 = 2.7

### 警告条件

```text id="g43fmp"
minLastFloorHeightRatio = 0.5
```

最終階高さが `floorHeight * 0.5` 未満なら Warning を出す。

### Warning 後の挙動

* Warning を表示する
* ただし生成は継続する
* v0.1 ではエラー停止しない

---

# 6. Generate フェーズ

## 6.1 概要

Semantic 情報に基づき **モジュール Prefab を配置**する。

---

## 6.2 v0.1 の生成対象

* 壁
* 屋根

将来対象:

* 柱
* アーチ
* 窓
* ドア

---

## 6.3 簡易メッシュ生成

v0.1 では以下を簡易メッシュ生成対象とする。

* 床
* 天井

### 床

* v0.1 の床は **簡易メッシュ生成方式**
* Prefab モジュール方式ではない

### 天井

* `GenerationSettings.generateCeiling = true` の場合のみ生成する
* CeilingCandidate 面に対して簡易メッシュ生成を行う
* デフォルトでは必須生成対象ではない

---

## 6.4 壁グリッド分割

### 基準幅の出所

`wallModuleWidth` は固定値ではなく、
**BuildingModuleCatalog で定義される基準壁モジュール（PrimaryWallModule）の `nominalSize.x`** を用いる。

### 高さ方向

* 高さ方向の分割は `floorHeight` を優先する
* `PrimaryWallModule.nominalSize.y` が `floorHeight` と不一致な場合は Warning を出す

### v0.1 の壁分割基準

```text id="0k0v9e"
wallModuleWidth = PrimaryWallModule.nominalSize.x
wallModuleHeight = floorHeight
```

### 分割手順

1. 壁サイズ取得
2. フロア境界で Y 分割
3. 横方向グリッド分割
4. モジュール配置

---

## 6.5 横方向グリッド起点

v0.1 の横方向グリッド分割は
**対象面を外側から見たときの左端** を起点として、右方向へ順に配置する。

### 方向定義

* `forward = faceNormal`
* `up = Vector3.up`
* `right = normalize(cross(up, forward))`
* 左方向 = `-right`

### 余りの扱い

* 余りは右端で吸収する

### v0.1 の割り切り

* 中央吸収
* 左右均等配置
* 対称配置

は行わない。

---

## 6.6 余り処理

優先順位:

1. Adjustable モジュール
2. トリム
3. 軽微スケール

### Adjustable モジュール

専用の可変部材。

例:

```text id="jwiqarf"
WallPanel_AdjustableWidth
```

幅または高さ方向のみスケール可能。

**採用条件**

* 対応モジュールが存在する
* 必要スケール値が `scaleRange` 内に収まる

これを満たさない場合はトリムへ移行する。

### トリム

端部余りを隠す装飾パーツ。
メッシュ切断は行わない。

**採用条件**

* トリム部材が存在する
* 余り量が `maxTrimCoverageRatio * wallModuleWidth` 以下

これを満たさない場合は軽微スケールへ移行する。

### 軽微スケール

Adjustable モジュールにのみ適用する。

```text id="pkhd38"
minScale = 0.9
maxScale = 1.1
```

**採用条件**

* 対象モジュールが `allowScale = true`
* 必要スケール値が `scaleRange` 内に収まる

### 失敗時

上記いずれも適用できない場合は

* Warning を表示する
* その区画は未生成とする

---

## 6.7 屋根モジュール配置

v0.1 の屋根は **Roof 面ごとに 1 枚配置**する。

### 配置ルール

* Roof 判定された各面を対象とする
* 各 Roof 面に対して `FlatRoof` モジュールを 1 つ配置する
* 配置基準:

  * 面中心
  * 面法線
  * 面サイズ
* 必要に応じて平面方向にスケールしてフィットさせる

### v0.1 の前提

* 直方体対応
* 水平上面のみ
* 壁のようなグリッド分割は行わない
* 複数 Roof 面がある場合は各面に 1 枚ずつ配置する

### 補足

* 屋根モジュールは `wallModuleWidth` を使わない
* Roof 面サイズに合わせて配置する

---

## 6.8 モジュール配置回転ルール

### 壁モジュール

* Prefab のローカル `+Z` を **対象面法線方向** に一致させる
* Prefab のローカル `+Y` を **ワールド上方向** に一致させる

### 屋根モジュール

* Prefab のローカル `+Y` を **ワールド上方向** に一致させる
* Prefab のローカル `+Z` を **`BuildingAuthoring.transform.forward` の水平投影方向** に一致させる
* 水平投影がゼロに近い場合は `Vector3.forward` を用いる
* v0.1 では水平 Roof 面のみ対象とするため、傾斜回転は扱わない

---

# 7. モジュール管理

## 7.1 概要

モジュールは **ScriptableObject カタログ** で管理する。

---

## 7.2 BuildingModuleCatalog

保持内容:

* `PrimaryWallModule`
* `WallModules`
* `RoofModules`
* `FloorSettings`
* `CeilingSettings`

将来:

* `PillarModules`
* `ArchModules`
* `WindowModules`
* `DoorModules`

### PrimaryWallModule と WallModules の関係

* `PrimaryWallModule` は **`WallModules` 内の 1 エントリを参照する基準壁モジュール** とする
* 壁分割基準は `PrimaryWallModule.nominalSize.x` を使う
* 標準壁としてまず `PrimaryWallModule` を優先配置し、余り処理時に `WallModules` から Adjustable / Trim 候補を探索する

---

## 7.3 FloorSettings / CeilingSettings

`FloorSettings` / `CeilingSettings` は
**簡易メッシュ生成用設定** を保持する。

### 保持項目例

* `material`
* `generateCollider`
* `thickness`
* `offset`

### 用途

* 床・天井の簡易メッシュに使用する
* Prefab 指定には使わない

---

## 7.4 BuildingModuleEntry

```text id="x0m74n"
moduleId
role
prefab
nominalSize
adjustableAxes
allowScale
scaleRange
priority
```

---

## 7.5 参照方法

`BuildingAuthoring` が使用する `BuildingModuleCatalog` を参照する。

例:

```text id="dnt9r2"
BuildingAuthoring.moduleCatalog
```

### v0.1 の使用

* 壁生成時 → `PrimaryWallModule`, `WallModules`
* 屋根生成時 → `RoofModules`
* 床・天井 → `FloorSettings`, `CeilingSettings`

---

## 7.6 モジュールアセット規約

### 壁モジュール

* Pivot: **底辺中央**
* 軸規約:

```text id="78raz5"
+Y = 上
+Z = 前
```

### 屋根モジュール（FlatRoof）

* Pivot: **底面中心**
* 軸規約:

```text id="2232qu"
+Y = 上
+Z = 前
```

* 底面が Roof 面に接する前提で配置する
* `allowScale = true`
* `adjustableAxes = XZ`

### nominalSize

壁モジュールでは以下とする。

```text id="efqggw"
X = 幅
Y = 高さ
Z = 厚み
```

屋根モジュールでは以下とする。

```text id="irknm7"
X = 幅
Y = 厚み
Z = 奥行き
```

### 実寸規約

* Prefab メッシュサイズと `nominalSize` は **完全一致必須**
* `nominalSize` はモジュールの基準寸法である
* 自動補正は行わない

### スケール規約

* 配置時スケールは `allowScale = true` のモジュールにのみ許可する
* スケール可能軸は `adjustableAxes` に従う
* 通常壁モジュールは原則 `allowScale = false`
* Adjustable 壁モジュールは `allowScale = true`
* v0.1 の `FlatRoof` は `allowScale = true`, `adjustableAxes = XZ` を持つ屋根モジュールとして扱う

---

# 8. GeneratedObjects 管理

## 8.1 Scene 管理方式

GeneratedObjects は **Scene 内 MonoBehaviour** で保持する。
Asset には保存しない。

---

## 8.2 役割分担

```text id="6ea6kj"
BuildingAuthoring
 ├ GeneratedObjectRegistry
 └ GeneratedObjects
```

### GeneratedObjects

**論理生成データ** を保持する。

保持内容:

* `GeneratedElement[]`
* Face / Edge / Volume との対応
* `sourceId`
* `generationGroup`
* `isLocked`
* `bounds`

### GeneratedObjectRegistry

**Scene 上の GameObject 参照管理** を行う。

保持内容:

* 生成 Root
* `elementId -> GameObject`
* `GameObject -> elementId`
* 削除 / 再生成時の参照解決

### 要約

* `GeneratedObjects` = 生成記録
* `GeneratedObjectRegistry` = Scene 参照管理

---

## 8.3 GeneratedElement

```text id="efcocz"
elementId
sourceKind
sourceId
moduleAssetId
generationGroup
isLocked
bounds
```

※ `instanceObject` は保持しない。
Scene 上の実体参照は `GeneratedObjectRegistry` が保持する。

---

## 8.4 Rebuild All 時の Registry 更新

v0.1 の `Rebuild All` では以下の順で処理する。

1. 旧生成オブジェクトを取得
2. 旧生成オブジェクトを破棄
3. `GeneratedObjectRegistry` をクリア
4. Shape 再解析
5. Semantic 再マッピング
6. 新規 `GeneratedElement` 群を作成
7. 新規 `elementId` を採番
8. 新規 GameObject 群を生成
9. 新しい Registry を構築

### ルール

* `elementId` は再利用しない
* 再生成時に再採番する

---

## 8.5 generationGroup

再生成単位用の論理グループ ID。

例:

```text id="muwttr"
Wall_Main
Roof_Main
Story_01
```

※ v0.1 では保持のみ行い、部分再生成にはまだ使用しない。

---

# 9. FaceID と再マッピング

## 9.1 方針

ProBuilder の face index は **永続 ID として使用しない**。
FaceId は解析時に再生成される一時識別子とする。

---

## 9.2 再マッピングに使う情報

旧面と新面の対応付けには以下を使う。

* 面中心
* 面法線
* 面面積
* Bounds

---

## 9.3 候補絞り込み閾値

* `maxNormalAngleDeg = 5.0`
* `maxCenterDistance = 0.25`
* `maxAreaRelativeDiff = 0.15`

### 面積差の計算

```text id="92hlqc"
abs(newArea - oldArea) / max(oldArea, epsilon) <= 0.15
```

---

## 9.4 複合スコア

各候補について以下のスコアを 0〜1 に正規化し、
重み付き加算で評価する。

* `centerDistanceScore`
* `normalDifferenceScore`
* `areaDifferenceScore`

### 合算式

```text id="h8qhhp"
totalScore =
    0.5 * centerDistanceScore +
    0.3 * normalDifferenceScore +
    0.2 * areaDifferenceScore
```

### 意味

* 中心距離を最重視
* 次に法線差
* 面積差は補助要素

---

## 9.5 一意性チェック

* 最良候補が明確に 1 つなら採用
* 次点候補との差が以下未満なら同等候補とみなす

```text id="i2qaqc"
equivalentScoreDelta = 0.05
```

この場合は **再対応失敗** とする。

---

## 9.6 再対応失敗時

* Semantic を Auto に戻す
* 要確認表示を出す
* 必要に応じてユーザーが再設定する

---

## 9.7 再マッピング実行タイミング

v0.1 では **`Rebuild All` 実行時にのみ** 再マッピングを行う。

### 手順

1. Shape 再解析
2. 旧 Semantic との再マッピング
3. 再生成

### v0.1 では行わないこと

* ProBuilder 編集の常時監視
* EditorWindow オープン時の自動再マッピング

---

# 10. 再生成

v0.1 の再生成は **全体再生成のみ**。

```text id="qjkbxz"
Rebuild All
```

### v0.1 に含む

* Rebuild All

### v0.1 に含まない

* Rebuild Selected
* Rebuild Roof
* 部分再生成

これらは v0.3 以降とする。

---

# 11. Bake

Bake フェーズおよび出力モードの本実装は **v0.2 以降** とする。

### v0.1 の扱い

* Scene 上への生成のみ対応
* Rebuild All による再生成のみ対応
* Bake ボタンや最終出力処理は未実装

### 将来の出力モード定義

| Mode      | 内容       |
| --------- | -------- |
| Separated | 個別オブジェクト |
| Grouped   | カテゴリ統合   |
| Combined  | メッシュ結合   |

---

# 12. v0.1 実装範囲

v0.1 で実装する機能:

* 直方体対応
* Face 自動分類
* Semantic 可視化
* Semantic 修正 UI
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
* generationGroup の活用
* 手編集保護

## v0.4

* 三角柱
* 切妻屋根
* アーチ
* 階段

---

# 14. 自己査読メモ

本版では以下の観点で自己査読を行った。

### 1. 実装不能な曖昧表現の削減

* 固定値だった `wallModuleWidth` を基準壁モジュール参照へ変更
* FlatRoof の Pivot / 軸 / adjustableAxes を明文化
* Ceiling 生成条件を `generateCeiling` に一本化
* Bake を v0.1 未実装として明確化

### 2. 実装フローの明示

* Rebuild All 時の Registry 更新順序を明文化
* 再マッピングの発火タイミングを Rebuild All に限定
* 余り処理の fallthrough 条件を段階ごとに定義

### 3. 座標系・向きのぶれ対策

* 壁の `+Z = faceNormal`
* 屋根の `+Z = BuildingAuthoring.forward の水平投影`
* 壁の「左」を外側視点で定義

### 4. 残っている前提

* v0.1 は直方体・単純閉ボリューム前提
* OuterWall 主対象
* SlopedFace は将来拡張
* 部分再生成と Bake は v0.2 以降

現時点で v0.1 実装に必要な主要仕様はかなり明文化できている。
残る論点は主に **実装クラス責務** と **Editor API への落とし込み** である。

---

# まとめ

本ツールは

**Shape → Semantic → Generate**

の三段階を中心とする
**半自動建物生成システム**である。

ユーザーによる意味修正と
プロシージャル生成を組み合わせることで、
柔軟な建築制作ワークフローを提供する。
