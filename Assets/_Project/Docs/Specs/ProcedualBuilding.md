# プロシージャル建物生成ツール仕様書

**Version 0.2（改訂版）**

---

# 1. 目的

Unity 上で建物を効率的に作成するための **半自動プロシージャル建物生成ツール** を開発する。

ユーザーは **ProBuilder を用いて建物のラフ形状を作成**し、
ツールが形状を解析して **建築的意味（Semantic）を推定**する。
ユーザーは必要に応じて意味づけを修正し、
最終的に **壁・屋根・柱・アーチなどのモジュールを自動配置**する。

本ツールは以下を目的とする。

* 建物制作の効率化
* プロシージャル生成による量産
* 手動編集との共存

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

---

## 2.4 ナレッジ保存方針

本ツールは当面 Unity + ProBuilder 上で実装する。
将来的に Blender など他ツールで同様の機能を実装する可能性はあるが、
そのために 現在の実装設計を過度に制約することは行わない。

代わりに以下を方針とする。

* 実装で得られた アルゴリズム・データ構造・ワークフローの知見を仕様書・設計メモとして記録する
* 建物生成の主要ロジック（Shape解析・Semantic推定・モジュール配置など）の考え方を文書化する
* Unity固有APIを使用する場合でも、その 目的・役割・代替手段を簡潔に記録しておく
* Blender版を開発する際は、これらのナレッジを参考に 再実装することを想定する

つまり本仕様では

* 設計の可搬性を強制しない
* 知識の可搬性を確保する

ことを目的とする。

---

# 3. フェーズ構成

---

# Phase 1: Shape（形状作成）

## 概要

ユーザーが **ProBuilder を用いて建物のラフ形状を作成する段階**。

この段階ではまだ建築的意味は存在しない。

### 形状例

* 直方体
* 三角柱
* 複数ボリュームの組み合わせ

---

## ShapeSource 抽象

入力形状は抽象インターフェースとして扱う。

```
ShapeSource
 ├ ProBuilderShape
 ├ MeshShape
 └ Future: BlenderShape
```

これにより Unity / Blender 間の移植性を確保する。

---

# Phase 2: Semantic（意味づけ）

## 概要

Shape を解析し、
**建築的意味（Semantic）を自動推定する段階。**

ユーザーはこの結果を確認し、
必要に応じて手動修正できる。

---

# 2.1 自動分類

面の法線に基づき初期分類する。

| 条件                  | 分類      |
| ------------------- | ------- |
| normal.y > 0.9      | Floor   |
| normal.y < -0.9     | Ceiling |
| abs(normal.y) < 0.2 | Wall    |

---

# 2.2 OuterWall / InnerWall 判定

v1 では **閉じた単一建物ボリューム**を前提とする。

### 判定方法

```
dir = normalize(faceCenter - buildingCenter)
dot = dot(faceNormal, dir)
```

| 条件               | 分類          |
| ---------------- | ----------- |
| dot > threshold  | OuterWall   |
| dot < -threshold | InnerWall候補 |
| その他              | 要確認         |

補足:

* 主生成対象は **OuterWall**
* InnerWall は必要に応じて手動修正を前提とする

---

# 2.3 Semantic 可視化

Semanticは **色分け表示**される。

| Semantic    | Color  |
| ----------- | ------ |
| Wall        | Red    |
| Floor       | Green  |
| Roof        | Blue   |
| Ceiling     | Cyan   |
| OpeningHost | Yellow |

---

# 2.4 手動修正

ユーザーは以下を変更可能。

* 面の役割
* 開口指定
* 特殊壁
* 屋根指定

例

```
Right Click → Set Face Role → Entrance
```

---

# 3. Generate（建物生成）

Semantic情報に基づき
**モジュールアセットを配置する。**

---

# 4. モジュールアセット仕様

v1 の生成方式は **事前定義モジュール配置方式**とする。

モジュールは

* Prefab
* Mesh + Material

など再利用可能な部材として登録する。

---

## モジュール種類

### 壁

* Wall Panel
* Window Wall
* Door Wall

### 屋根

* Flat Roof
* Gable Roof
* Roof Edge

### 構造

* Pillar
* Beam

### 装飾

* Arch
* Balcony

---

## 例外: 簡易メッシュ生成

以下は簡易生成を許可する

* 床
* 天井
* デバッグ面
* Bake結合メッシュ

---

# 4.2 壁グリッド分割

v1 では **固定サイズ分割方式**を採用する。

例:

```
wallModuleWidth = 1.0m
wallModuleHeight = 3.0m
```

---

## 分割手順

1. 壁面サイズ取得
2. 基準サイズで整数分割
3. 余り計算
4. 余り処理

---

## 余り処理

優先順:

1. 調整パネル
2. トリム
3. 軽微スケール

---

# 5. Post Edit（生成後編集）

生成後にユーザーが直接編集可能。

可能操作

* 個別パーツ差し替え
* 削除
* 手動追加
* Semantic変更

---

# 再生成トリガー

Semantic変更では **自動再生成は行わない**。

再生成はユーザー操作による。

例

```
Rebuild All
Rebuild Selection
Rebuild Roof
```

---

# 6. Bake / Export

生成結果を最終データへ変換する。

---

# 出力モード

| Mode      | 内容     |
| --------- | ------ |
| Separated | パーツ単位  |
| Grouped   | カテゴリ単位 |
| Combined  | メッシュ結合 |

出力モードは **生成中でも変更可能**。

内部データは常に分離状態で保持する。

---

# 7. 追加機能

---

# A. 自動分類信頼度

Semantic推定に **Confidence** を持たせる。

| 状態  | 意味       |
| --- | -------- |
| 確定  | アルゴリズム確信 |
| 要確認 | 曖昧       |

---

# B. 再生成範囲指定（重要）

再生成対象:

* 全体
* 面単位
* 階単位
* 屋根のみ
* 壁のみ

例

```
Rebuild → Selected Face
```

---

# C. 手編集保護（重要）

生成パーツに **Lockフラグ**を持たせる。

Lockされたパーツは

* 再生成対象外
* 自動更新対象外

---

# 8. GeneratedObjects データ構造

GeneratedObjects は
**生成要素とSemantic要素の対応を保持する管理構造**とする。

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
GeneratedElement
- elementId
- sourceKind
- sourceId
- role
- moduleAssetId
- instanceObject
- bounds
- isLocked
- generationGroup
```

---

# 9. ProBuilder依存

Unity版 v1 は **ProBuilder を推奨入力ツール**とする。

ただし生成コアは

```
ShapeSource Interface
```

を通して動作し
将来的な Blender 移植を可能にする。

---

# 10. 初期実装スコープ

### v0.1

* 直方体対応
* Face自動分類
* 壁 / 床 / 屋根生成

### v0.2

* 開口
* 窓 / ドア
* 柱

### v0.3

* 部分再生成
* 手編集保護
* 出力モード

### v0.4

* 三角柱
* 切妻屋根
* アーチ
* 階段

---

# まとめ

本ツールは

* ProBuilderによる形状作成
* Semanticによる意味づけ
* モジュール配置による建物生成

の3段階構造を採用する。

生成は **半自動**とし、

* 自動生成
* 手動編集
* 再生成

を共存させることで
効率と柔軟性を両立する。

さらに
**Unity / Blender 両環境での展開を考慮した可搬設計**を採用する。
