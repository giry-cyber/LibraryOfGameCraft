# プロシージャル建物生成ツール仕様書

**Version 0.3（統合版）**

---

# 1. 目的

Unity 上で建物を効率的に作成するための **半自動プロシージャル建物生成ツール** を開発する。

ユーザーは **ProBuilder を用いて建物のラフ形状（Shape）を作成**し、
ツールが形状を解析して **建築的意味（Semantic）を推定**する。
ユーザーは必要に応じて意味づけを修正し、最終的に **壁・屋根・柱・アーチなどのモジュールを自動配置**する。

本ツールは以下を目的とする。

* 建物制作の効率化
* プロシージャル生成による量産
* 手動編集との共存
* 生成ロジックの再利用可能なナレッジ化

---

# 2. 基本設計方針

## 2.1 半自動生成

建物生成は **完全自動ではなく半自動**とする。

ツールは形状から建築的意味を推定するが、
最終判断はユーザーが修正できる。

理由:

* 建築構造は形状だけでは判断できない場合がある
* 建物の意図（入口・装飾など）はユーザーが決定する必要がある

---

## 2.2 編集可能性の確保

生成後も以下の操作が可能であることを前提とする。

* 個別パーツ差し替え
* 部分再生成
* 手動配置
* Semantic変更による再生成

生成結果は単なるメッシュではなく
**意味情報を保持した生成データ構造**として扱う。

---

## 2.3 フェーズベースワークフロー

建物生成は以下のフェーズで構成される。

```
Shape → Semantic → Generate → PostEdit → Bake
```

各フェーズは以下の役割を持つ。

| Phase    | 役割      |
| -------- | ------- |
| Shape    | 建物形状作成  |
| Semantic | 建築意味推定  |
| Generate | モジュール配置 |
| PostEdit | 生成後編集   |
| Bake     | 最終化     |

---

## 2.4 ナレッジ保存方針

本ツールは当面 **Unity + ProBuilder** 上で実装する。

将来的に Blender など他ツールで同様の機能を実装する可能性はあるが、
そのために **現在の実装設計を過度に制約することは行わない**。

代わりに以下を方針とする。

* 実装で得られた **アルゴリズム・データ構造・ワークフローの知見**を仕様書や設計メモとして記録する
* Shape解析、Semantic推定、モジュール配置などのロジックを文書化する
* Unity固有機能を使用した場合、その目的と代替可能性を簡潔に記録する

つまり本仕様では

* **設計の可搬性は強制しない**
* **ナレッジの可搬性を確保する**

ことを目的とする。

---

# 3. Shape フェーズ（形状作成）

## 概要

ユーザーが **ProBuilder を用いて建物のラフ形状を作成する段階**。

この段階ではまだ建築的意味は存在しない。

---

## 対応形状（v0.1）

* 直方体
* 単純閉ボリューム

複雑形状は将来拡張とする。

---

## ShapeSource 抽象

入力形状は抽象インターフェースとして扱う。

```
ShapeSource
 ├ ProBuilderShape
 ├ MeshShape
 └ Future: BlenderShape
```

---

# 4. Semantic フェーズ（意味づけ）

## 概要

Shape を解析し、建築的意味を推定する。

---

# 4.1 面の自動分類

面法線に基づき初期分類する。

| 条件                  | 分類           |
| ------------------- | ------------ |
| normal.y > 0.9      | UpwardFace   |
| normal.y < -0.9     | DownwardFace |
| abs(normal.y) < 0.2 | Wall         |

---

# 4.2 Roof / Floor 分類

UpwardFace を以下のルールで分類する。

### Roof 判定

```
maxY = max(faceCenter.y)
```

```
if faceCenter.y >= maxY - epsilon
    → Roof
else
    → Floor
```

パラメータ

```
epsilon = 0.02
```

---

# 4.3 OuterWall / InnerWall 判定

建物中心から面中心へのベクトルを使用する。

```
dir = normalize(faceCenter - buildingCenter)
dot = dot(faceNormal, dir)
```

| 条件      | 分類          |
| ------- | ----------- |
| dot > 0 | OuterWall   |
| dot < 0 | InnerWall候補 |

v0.1では **OuterWallを主対象**とする。

---

# 4.4 Semantic 可視化

Semantic は **SceneView オーバーレイ描画**で可視化する。

実装方式

* Handles / Gizmos 描画
* 面重心付近に半透明ポリゴン表示

ProBuilder メッシュ自体のマテリアルや頂点カラーは変更しない。

---

## 色分け

| Semantic    | Color  |
| ----------- | ------ |
| Wall        | Red    |
| Floor       | Green  |
| Roof        | Blue   |
| Ceiling     | Cyan   |
| OpeningHost | Yellow |

---

# 4.5 手動修正 UI

v0.1 では **EditorWindow + Inspector 方式**を採用する。

### 操作フロー

1. ProBuilder で面選択
2. Semantic EditorWindow が選択面を取得
3. Role変更
4. Apply

---

### 変更可能項目

* FaceRole
* OpeningHost
* Roof override
* Auto / Manual

---

# 5. Generate フェーズ（建物生成）

Semantic 情報に基づき
**モジュールアセットを配置する。**

---

# 5.1 モジュール生成方式

v0.1 の生成方式は **Prefab モジュール配置方式**とする。

対象

* 壁
* 屋根
* 柱
* アーチ

---

# 5.2 簡易メッシュ生成

以下は簡易生成を許可する。

* 床
* 天井
* デバッグ表示
* Bake結合メッシュ

---

# 5.3 壁グリッド分割

分割方式

```
wallModuleWidth = 1m
wallModuleHeight = 3m
```

---

## 分割手順

1. 壁面サイズ取得
2. モジュールサイズで整数分割
3. 余り計算
4. 余り処理

---

# 5.4 余り処理

優先順位

1. Adjustable モジュール
2. トリム部材
3. 軽微スケール

---

### Adjustable モジュール

幅または高さのみスケール可能な専用モジュール。

例

```
WallPanel_AdjustableWidth
```

---

### トリム

端部余りを隠す装飾部材。

メッシュ切断は行わない。

---

### 軽微スケール

Adjustable モジュールにのみ適用。

```
minScale = 0.9
maxScale = 1.1
```

---

# 6. PostEdit フェーズ

生成後の編集を可能にする。

可能操作

* 個別パーツ差し替え
* 手動追加
* 削除
* Semantic変更

---

# 再生成トリガー

Semantic変更では **自動再生成は行わない**。

ユーザー操作で再生成する。

例

```
Rebuild All
Rebuild Selection
Rebuild Roof
```

---

# 7. モジュール管理

モジュールは **ScriptableObject カタログ**で管理する。

---

## BuildingModuleCatalog

保持内容

* 壁モジュール
* 屋根モジュール
* 柱モジュール
* アーチモジュール

---

## BuildingModuleEntry

```
moduleId
role
prefab
adjustableAxes
nominalSize
priority
allowScale
scaleRange
```

---

# 8. GeneratedObjects データ構造

GeneratedObjects は
**Semantic要素と生成結果の対応を保持する管理構造**である。

```
GeneratedObjects
 ├ GeneratedElement[]
 ├ FaceId → ElementList
 ├ EdgeId → ElementList
 ├ VolumeId → ElementList
 └ Root GameObject
```

---

## GeneratedElement

```
elementId
sourceKind
sourceId
role
moduleAssetId
instanceObject
bounds
isLocked
generationGroup
```

---

## generationGroup

再生成スコープ用の論理グループID。

例

```
Wall_Main
Roof_Main
Story_01
```

---

# 9. Bake / Export

生成結果を最終データへ変換する。

---

## Output Mode

| Mode      | 内容     |
| --------- | ------ |
| Separated | 部材ごと   |
| Grouped   | カテゴリごと |
| Combined  | メッシュ結合 |

出力モードは **生成中でも変更可能**。

---

# 10. v0.1 実装スコープ

v0.1 では以下を実装する。

* 直方体対応
* Face自動分類
* Semantic可視化
* 最小Semantic修正UI
* 壁モジュール生成
* 屋根モジュール生成
* 床簡易メッシュ生成
* 再生成機能

---

# 将来拡張

v0.2以降

* 開口生成
* 窓 / ドア
* 柱
* 屋根バリエーション

v0.3

* 部分再生成
* 手編集保護
* 出力モード拡張

v0.4

* 三角柱
* 切妻屋根
* アーチ
* 階段

---

# まとめ

本ツールは

* Shape（形状）
* Semantic（意味）
* Generate（部材配置）

の三段階を中心とする **半自動建物生成ツール**である。

ユーザーによる手動修正と
プロシージャル生成を共存させることで
実用的な建築制作ワークフローを実現する。
