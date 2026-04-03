# プロシージャル建物生成ツール仕様書

**Version 0.6（整合版）**

---

# 1. 目的

Unity 上で建物を効率的に制作するための **半自動プロシージャル建物生成ツール** を開発する。

ユーザーは **ProBuilder を用いて建物のラフ形状（Shape）を作成**し、
ツールが形状を解析して **建築的意味（Semantic）を推定**する。

その後、ユーザーは必要に応じて意味づけを修正し、
最終的に **壁・屋根などのモジュールを自動配置**する。

目的：

* 建物制作効率の向上
* プロシージャル生成による量産
* 手動編集との共存
* 建築生成アルゴリズムのナレッジ蓄積

---

# 2. 基本設計

## 2.1 半自動生成

本ツールは **完全自動生成ではなく半自動生成**とする。

理由：

* 建築構造は形状だけでは判断できない場合がある
* 建物の意味（入口、装飾など）はユーザー判断が必要

---

## 2.2 編集可能設計

生成後も以下の編集を可能とする。

* 個別パーツ差し替え
* 手動配置
* Semantic変更
* 再生成

生成結果は単なるメッシュではなく
**意味情報付き生成データ**として扱う。

---

## 2.3 フェーズ構造

建物生成は以下のフェーズで構成される。

```text
Shape → Semantic → Generate → PostEdit → Bake
```

| Phase    | 役割      |
| -------- | ------- |
| Shape    | 建物形状作成  |
| Semantic | 建築意味推定  |
| Generate | モジュール配置 |
| PostEdit | 生成後編集   |
| Bake     | 最終メッシュ化 |

---

## 2.4 ナレッジ保存方針

将来的に Blender 等で同様のツールを作る可能性があるが、
本ツールでは **Unity実装を制限しない**。

代わりに

* アルゴリズム
* データ構造
* ワークフロー

を仕様書として記録する。

---

# 3. Shape フェーズ

## 3.1 概要

ユーザーが **ProBuilder を使って建物のラフ形状を作る段階**。

まだ建築意味は存在しない。

---

## 3.2 v0.1対応形状

* 直方体
* 単純閉ボリューム

---

## 3.3 ShapeSource 抽象

```text
ShapeSource
 ├ ProBuilderShape
 ├ MeshShape
 └ BlenderShape (future)
```

---

# 4. Semantic フェーズ

Shape を解析し、建築意味を推定する。

---

## 4.1 法線による面分類

| 条件                  | 分類           |
| ------------------- | ------------ |
| normal.y ≥ 0.9      | UpwardFace   |
| normal.y ≤ -0.9     | DownwardFace |
| abs(normal.y) ≤ 0.2 | VerticalFace |
| その他                 | SlopedFace   |

---

## 4.2 SlopedFace

斜面。

v0.1では

* 自動生成対象外
* 手動分類対象

将来：

* 屋根面として使用

---

## 4.3 Roof / Floor 判定

UpwardFace をさらに分類する。

```text
maxY = max(face.center.y)
if face.center.y >= maxY - roofHeightEpsilon
    → Roof
else
    → Floor
```

### パラメータ

* `roofHeightEpsilon = 0.05`
* `roofHeightEpsilon` は `GenerationSettings` に保持する

### ルール

* `maxY - roofHeightEpsilon` 以上の UpwardFace はすべて Roof とする
* 同じ高さ帯に複数面があれば、すべて Roof とする

---

## 4.4 Ceiling 候補

`DownwardFace` は v0.1 では **CeilingCandidate** として扱う。

### v0.1 の扱い

* 可視化では Ceiling 色を使用してよい
* 生成対象としては任意
* 天井が必要な場合のみ簡易メッシュ生成対象とする

---

## 4.5 OuterWall / InnerWall 判定

```text
dir = normalize(faceCenter - buildingCenter)
dot = dot(faceNormal, dir)
```

| 条件      | 分類          |
| ------- | ----------- |
| dot > 0 | OuterWall   |
| dot < 0 | InnerWall候補 |

### buildingCenter

`buildingCenter` は **Shape 全体のワールド空間 AABB 中心**とする。

### v0.1 の扱い

* OuterWall を主対象とする
* InnerWall は手動補正前提

---

## 4.6 Semantic 可視化

SceneView 上で **Handles / Gizmos** によるオーバーレイ表示を行う。

### 表示仕様

* 面重心付近に半透明ポリゴンまたは輪郭を描画
* Semantic に応じて色分け
* 要確認面は強調色表示

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
  * Semantic EditorWindow が開いており、対象が現在編集中

### 注意

* ProBuilder メッシュ自体のマテリアルや頂点カラーは変更しない

---

## 4.7 手動修正UI

実装方式：

**EditorWindow + Inspector**

### 操作

1. ProBuilderで面選択
2. EditorWindowでSemantic変更
3. Apply

### 変更可能項目

* FaceRole
* OpeningHost
* Auto / Manual override

---

# 5. GenerationSettings

建物生成パラメータ。

---

## 5.1 フロア情報

保持項目：

```text
floorCount
floorHeight
baseElevation
roofHeightEpsilon
```

将来拡張：

```text
storyHeights[]
topOffset
```

---

## 5.2 フロア境界の扱い

v0.1 ではフロア境界は Shape から自動推定せず、
**GenerationSettings により定義する**。

### 例

高さ 9m の建物

```text
floorCount = 3
floorHeight = 3
```

→

```text
0-3m
3-6m
6-9m
```

---

## 5.3 実メッシュ高さと不一致な場合

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

### 警告

最終階の高さが極端に小さい場合は警告を出す。

例：

```text
minLastFloorHeightRatio = 0.5
```

標準階高の 50% 未満なら Warning 表示。

### Warning 後の挙動

* Warning を表示する
* ただし **生成は継続する**
* v0.1 ではエラー停止は行わない

---

# 6. Generate フェーズ

Semantic情報に基づき **モジュールPrefabを配置する。**

---

## 6.1 モジュール生成対象

v0.1 の対象：

* 壁
* 屋根

将来対象：

* 柱
* アーチ
* 窓
* ドア

---

## 6.2 簡易メッシュ生成

v0.1 では以下は簡易メッシュ生成を許可する。

* 床
* 天井

### v0.1 の床

* 床は **簡易メッシュ生成方式**
* Prefab モジュール方式ではない

### v0.1 の天井

* 天井は必要な場合のみ簡易メッシュ生成方式
* 必須生成対象ではない

---

## 6.3 壁グリッド分割

```text
wallModuleWidth = 1m
wallModuleHeight = floorHeight
```

### 分割手順

1. 壁サイズ取得
2. フロア境界でY分割
3. 横方向グリッド分割
4. モジュール配置

---

## 6.4 余り処理

優先順位：

1. Adjustable モジュール
2. トリム
3. 軽微スケール

### Adjustable モジュール

専用の可変部材。

例：

```text
WallPanel_AdjustableWidth
```

幅または高さ方向のみスケール可能。

### トリム

端部余りを隠す装飾パーツ。
メッシュ切断は行わない。

### 軽微スケール

Adjustable モジュールにのみ適用。

```text
minScale = 0.9
maxScale = 1.1
```

---

## 6.5 屋根モジュール配置

v0.1 の屋根は **Roof 面ごとに1枚配置**する。

### 配置ルール

* Roof 判定された各面を対象にする
* 各 Roof 面に対して `FlatRoof` モジュールを 1 つ配置する
* 配置基準は

  * 面中心
  * 面法線
  * 面サイズ
* 必要に応じて平面方向にスケールしてフィットさせる

### v0.1 の前提

* 直方体対応
* 水平上面のみ
* 壁のようなグリッド分割は行わない
* 複数 Roof 面がある場合は、各面に1枚ずつ配置する

### 補足

* 屋根モジュールは `wallModuleWidth` を使わない
* Roof 面サイズに合わせて配置する

---

## 6.6 モジュール配置回転ルール

### 壁モジュール

* Prefab のローカル `+Z` を **対象面法線方向** に一致させる
* Prefab のローカル `+Y` を **ワールド上方向** に一致させる

### 屋根モジュール

* Prefab のローカル `+Y` を **ワールド上方向** に一致させる
* Prefab の平面軸を Roof 面平面に合わせる
* v0.1 では水平 Roof 面のみを対象とするため、特殊な傾斜回転は不要

---

# 7. モジュール管理

モジュールは **ScriptableObject カタログ**で管理する。

---

## 7.1 BuildingModuleCatalog

保持内容：

* WallModules
* RoofModules
* FloorSettings
* CeilingSettings

将来：

* PillarModules
* ArchModules
* WindowModules
* DoorModules

---

## 7.2 FloorSettings / CeilingSettings

`FloorSettings` / `CeilingSettings` は
**簡易メッシュ生成用設定**を保持する。

### 保持項目例

* `material`
* `generateCollider`
* `thickness`
* `offset`

### 用途

* 床・天井の簡易メッシュに使用する見た目や生成設定を定義する
* Prefab モジュール指定には使わない

---

## 7.3 BuildingModuleEntry

```text
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

## 7.4 参照方法

`BuildingAuthoring` が使用する `BuildingModuleCatalog` を参照する。

例：

```text
BuildingAuthoring.moduleCatalog
```

### v0.1 の使用

* 壁生成時 → `WallModules`
* 屋根生成時 → `RoofModules`
* 床・天井 → `FloorSettings` / `CeilingSettings`

---

## 7.5 モジュールアセット規約

### 壁モジュール

Pivot：
**底辺中央**

Axis：

```text
+Y = 上
+Z = 前
```

### nominalSize

```text
X = 幅
Y = 高さ
Z = 厚み
```

### 規約

* Prefabメッシュサイズと `nominalSize` は **完全一致必須**
* `nominalSize` はモジュールの **基準寸法** である
* 自動補正は行わない

### スケール規約

* 配置時のスケールは `allowScale = true` のモジュールにのみ許可する
* スケール可能軸は `adjustableAxes` に従う
* 通常壁モジュールは原則 `allowScale = false`
* Adjustable 壁モジュールは `allowScale = true`
* v0.1 の `FlatRoof` モジュールは `allowScale = true` を持つ屋根モジュールとして扱う

---

# 8. GeneratedObjects 管理

生成結果を管理する構造。

---

## 8.1 Scene管理方式

GeneratedObjects は **Scene内 MonoBehaviour** で保持する。
Assetには保存しない。

---

## 8.2 役割分担

```text
BuildingAuthoring
 ├ GeneratedObjectRegistry
 └ GeneratedObjects
```

### GeneratedObjects

**論理生成データ**を保持する。

保持内容：

* `GeneratedElement[]`
* Face / Edge / Volume との対応
* sourceId
* role
* generationGroup
* isLocked
* bounds

### GeneratedObjectRegistry

**Scene 上の GameObject 参照管理**を行う。

保持内容：

* 生成 Root
* `elementId -> GameObject` の引き当て
* `GameObject -> elementId` の引き当て
* 削除 / 再生成時の参照解決

### 要約

* `GeneratedObjects` = 生成記録
* `GeneratedObjectRegistry` = Scene参照管理

---

## 8.3 GeneratedElement

```text
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

## 8.4 generationGroup

再生成単位用の論理グループID。

例：

```text
Wall_Main
Roof_Main
Story_01
```

※ v0.1 では保持だけ可能だが、部分再生成にはまだ使用しない。

---

# 9. FaceID と再マッピング

ProBuilder の face index は **永続IDとして使用しない**。

FaceId は解析時に再生成される一時識別子とする。

---

## 9.1 再マッピング方法

以下を使って旧面と新面を対応付ける。

* 面中心
* 面法線
* 面積
* Bounds

---

## 9.2 再対応付け手順

### Step 1: 候補絞り込み

以下を満たす面を候補にする。

* 法線差が閾値以内
* 面中心距離が閾値以内
* 面積差が閾値以内

### Step 2: 複合スコア計算

候補ごとに以下を元に複合スコアを計算する。

* centerDistanceScore
* normalDifferenceScore
* areaDifferenceScore

### Step 3: 一意性チェック

* 最良候補が明確に1つなら採用
* 同等候補が複数あるなら **再対応失敗** とする

---

## 9.3 再対応失敗時

* Semantic を Auto に戻す
* 要確認表示を出す
* 必要に応じてユーザーが再設定する

---

## 9.4 再マッピング実行タイミング

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

```text
Rebuild All
```

### v0.1 に含む

* Rebuild All

### v0.1 に含まない

* Rebuild Selected
* Rebuild Roof
* 部分再生成

これらは v0.3 以降。

---

# 11. Bake

最終出力。

| Mode      | 内容       |
| --------- | -------- |
| Separated | 個別オブジェクト |
| Grouped   | カテゴリ統合   |
| Combined  | メッシュ結合   |

---

# 12. v0.1 実装範囲

v0.1 で実装する機能：

* 直方体対応
* Face自動分類
* Semantic可視化
* Semantic修正UI
* 壁モジュール生成
* 屋根モジュール生成
* 床メッシュ生成
* Rebuild All
* ModuleCatalog 参照
* フロア境界設定

---

# 13. 将来拡張

## v0.2

* 窓
* ドア
* 開口

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

# まとめ

本ツールは

**Shape → Semantic → Generate**

の三段階を中心とする
**半自動建物生成システム**である。

ユーザーによる意味修正と
プロシージャル生成を組み合わせることで
柔軟な建築制作ワークフローを提供する。
