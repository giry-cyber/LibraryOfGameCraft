# プロシージャル建物生成ツール仕様書

**Version 0.4（実装前確定版）**

---

# 1. 目的

Unity 上で建物を効率的に制作するための **半自動プロシージャル建物生成ツール** を開発する。

ユーザーは **ProBuilder を用いて建物のラフ形状（Shape）を作成**し、
ツールが形状を解析して **建築的意味（Semantic）を推定**する。

その後、ユーザーは必要に応じて意味づけを修正し、
最終的に **壁・屋根・柱などのモジュールを自動配置**する。

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

```
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

## 概要

ユーザーが **ProBuilder を使って建物のラフ形状を作る段階**。

まだ建築意味は存在しない。

---

## v0.1対応形状

* 直方体
* 単純閉ボリューム

---

## ShapeSource 抽象

```
ShapeSource
 ├ ProBuilderShape
 ├ MeshShape
 └ BlenderShape (future)
```

---

# 4. Semantic フェーズ

Shape を解析し、建築意味を推定する。

---

# 4.1 法線による面分類

| 条件                  | 分類           |
| ------------------- | ------------ |
| normal.y ≥ 0.9      | UpwardFace   |
| normal.y ≤ -0.9     | DownwardFace |
| abs(normal.y) ≤ 0.2 | VerticalFace |
| その他                 | SlopedFace   |

---

## SlopedFace

斜面。

v0.1では

* 自動生成対象外
* 手動分類対象

将来：

* 屋根面として使用

---

# 4.2 Roof / Floor 判定

UpwardFace をさらに分類。

```
maxY = max(face.center.y)
```

```
if face.center.y >= maxY - roofHeightEpsilon
    → Roof
else
    → Floor
```

---

## パラメータ

```
roofHeightEpsilon = 0.05
```

GenerationSettings で定義。

---

# 4.3 OuterWall / InnerWall 判定

```
dir = normalize(faceCenter - buildingCenter)
dot = dot(faceNormal, dir)
```

| 条件      | 分類          |
| ------- | ----------- |
| dot > 0 | OuterWall   |
| dot < 0 | InnerWall候補 |

v0.1では OuterWall を主対象とする。

---

# 4.4 Semantic 可視化

SceneView 上で **Handles/Gizmos** によるオーバーレイ表示。

| Semantic    | Color  |
| ----------- | ------ |
| Wall        | Red    |
| Floor       | Green  |
| Roof        | Blue   |
| Ceiling     | Cyan   |
| OpeningHost | Yellow |

ProBuilderメッシュ自体は変更しない。

---

# 4.5 手動修正UI

実装方式：

**EditorWindow + Inspector**

操作：

1. ProBuilderで面選択
2. EditorWindowでSemantic変更
3. Apply

---

変更可能項目

* FaceRole
* OpeningHost
* Auto / Manual override

---

# 5. GenerationSettings

建物生成パラメータ。

---

## フロア情報

```
floorCount
floorHeight
baseElevation
roofHeightEpsilon
```

---

### 例

高さ9mの建物

```
floorCount = 3
floorHeight = 3
```

→

```
0-3m
3-6m
6-9m
```

---

# 6. Generate フェーズ

Semantic情報に基づき
**モジュールPrefabを配置する。**

---

# 6.1 モジュール生成

対象：

* 壁
* 屋根
* 柱
* アーチ

---

# 6.2 簡易メッシュ生成

以下はメッシュ生成を許可

* 床
* 天井

---

# 6.3 壁グリッド分割

```
wallModuleWidth = 1m
wallModuleHeight = floorHeight
```

---

分割手順

1. 壁サイズ取得
2. フロア境界でY分割
3. 横方向グリッド分割
4. モジュール配置

---

# 6.4 余り処理

優先順位

1. Adjustable モジュール
2. トリム
3. 軽微スケール

---

### Adjustable モジュール

例

```
WallPanel_AdjustableWidth
```

幅のみスケール可能。

---

### トリム

端部余りを隠す装飾パーツ。

メッシュ切断は行わない。

---

### 軽微スケール

```
minScale = 0.9
maxScale = 1.1
```

Adjustableモジュールのみ。

---

# 7. モジュールアセット規約

Prefab 制作ルール。

---

## 壁モジュール

Pivot：

**底辺中央**

Axis：

```
+Y = 上
+Z = 前
```

---

## nominalSize

```
X = 幅
Y = 高さ
Z = 厚み
```

Prefabメッシュサイズと **完全一致必須**。

---

# 8. GeneratedObjects 管理

生成結果を管理する構造。

---

## Scene管理方式

GeneratedObjects は

**Scene内 MonoBehaviour で保持する**

Assetには保存しない。

---

構造

```
BuildingAuthoring
 ├ GeneratedObjectRegistry
 └ GeneratedObjects
```

---

## GeneratedElement

```
elementId
sourceKind
sourceId
moduleAssetId
instanceObject
generationGroup
isLocked
```

---

## generationGroup

再生成単位。

例

```
Wall_Main
Roof_Main
Story_01
```

---

# 9. FaceID 問題

ProBuilder の face index は **永続IDとして使用しない**。

FaceId は解析時に再生成される。

---

## 再マッピング方法

以下を使う

* 面中心
* 面法線
* 面積
* Bounds

これらを用いて
**最も近い面を再対応付けする。**

---

# 10. 再生成

v0.1 の再生成は **全体再生成のみ**。

```
Rebuild All
```

部分再生成は v0.3 以降。

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

---

# 13. 将来拡張

v0.2

* 窓
* ドア
* 開口

v0.3

* 部分再生成
* generationGroup
* 手編集保護

v0.4

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
