# プロシージャル建物生成ツール マニュアル — v0.1

## 概要

ProBuilder で作成した建物のラフ形状から、壁・屋根・床などのモジュール Prefab を自動配置するエディタ拡張ツールです。

面の建築的意味（Semantic）を自動推定しつつ、手動で修正・再生成できる **半自動ワークフロー** を提供します。

---

## 事前準備

### ステップ 1 — 壁モジュール Prefab を作る

壁 1 枚に対応する Prefab を用意します。

**構造:**
```
WallModule_Primary  (Empty GameObject) ← Prefab ルート
└─ Cube
```

**Cube の Transform:**

| 項目 | 値（幅 2m × 高さ 3m × 厚み 0.2m の場合） |
|---|---|
| Position | (0, **1.5**, 0) ← 高さの半分を Y にオフセット |
| Rotation | (0, 0, 0) |
| Scale | (**2**, **3**, **0.2**) |

> **ポイント:** ルート GameObject の原点が「底辺中央」になるようにする。
> Scene ビューで Prefab を開いたとき白い点が Cube の底面中央に来ていれば OK。

**ルートの Transform は必ず (0,0,0) / (0,0,0) / (1,1,1) にする。**
WallGenerator がルートをスケールするため、子で調整する。

---

### ステップ 2 — 屋根モジュール Prefab を作る

壁と同じ構造で作成します。Pivot は**底面中心**。

**Cube の Transform（幅 4m × 厚み 0.2m × 奥行き 4m の場合）:**

| 項目 | 値 |
|---|---|
| Position | (0, **0.1**, 0) ← 厚みの半分を Y にオフセット |
| Rotation | (0, 0, 0) |
| Scale | (**4**, **0.2**, **4**) |

屋根は Rebuild All 時に面サイズに合わせて XZ 方向へスケールされます。

---

### ステップ 3 — BuildingModuleEntry を作る

> **構造の確認:** Catalog に直接 Prefab を入れるのではありません。
> `Prefab → ModuleEntry → Catalog` という 3 段階の参照構造になっています。

プロジェクトウィンドウで右クリック →
`Create > LibraryOfGamecraft > Building > Module Entry`

Prefab の種類ごとに 1 つ作成します。

**壁（PrimaryWall）の設定例:**

| フィールド | 値 |
|---|---|
| moduleId | `wall_primary` |
| role | `PrimaryWall` |
| **prefab** | **ステップ 1 の Prefab をここにセット** |
| nominalSize | `(2, 3, 0.2)` ← Cube の Scale と一致させる |
| allowScale | false |
| scaleRange | (0.8, 1.2)（デフォルトのまま） |
| priority | 0 |

**屋根（FlatRoof）の設定例:**

| フィールド | 値 |
|---|---|
| moduleId | `roof_flat` |
| role | `FlatRoof` |
| **prefab** | **ステップ 2 の Prefab をここにセット** |
| nominalSize | `(4, 0.2, 4)` ← Cube の Scale と一致させる |
| allowScale | **true** |
| priority | 0 |

---

### ステップ 4 — BuildingModuleCatalog を作る

プロジェクトウィンドウで右クリック →
`Create > LibraryOfGamecraft > Building > Module Catalog`

Catalog には **ModuleEntry アセット**をセットします（Prefab を直接入れる場所ではありません）。

| フィールド | 設定内容 |
|---|---|
| Primary Wall Module | **ステップ 3 で作った壁の ModuleEntry** をセット |
| Wall Modules | AdjustableWall・TrimWall の ModuleEntry をセット（任意） |
| Roof Modules | **ステップ 3 で作った屋根の ModuleEntry** をセット |
| Floor Settings > Material | 床のマテリアル（None でも動く） |
| Floor Settings > Generate Collider | 床に MeshCollider を付けるか |

---

## Scene セットアップ

### 1. ProBuilder で Shape を作る

ProBuilder ウィンドウから **Shape Tool** で直方体を作成します（v0.1 は直方体のみ対応）。

### 2. BuildingAuthoring をアタッチする

作成した GameObject を選択し、Inspector で
`Add Component > LibraryOfGamecraft > Building > Building Authoring`

`SemanticStore` と `GeneratedObjectRegistry` が自動で追加されます。

### 3. Catalog をセットする

Inspector の `Module Catalog` フィールドにステップ 4 のカタログをセットします。

### 4. 生成設定を入力する

| フィールド | 説明 | デフォルト |
|---|---|---|
| フロア数 | 壁を何階分グリッド分割するか | 1 |
| フロア高さ | 1 フロアの高さ（m） | 3 |
| baseElevation 自動算出 | ON のとき Shape の最低 Y を基準にする | ON |
| baseElevation | 自動算出 OFF のときの手動値 | 0 |
| Roof 判定 Epsilon | Roof と Floor の境界判定幅 | 0.05 |
| 天井を生成する | CeilingCandidate 面にメッシュを生成するか | OFF |
| TrimWall 最大被覆率 | 余りスペースの何割まで TrimWall で隠すか | 0.25 |

---

## 基本ワークフロー

```
ProBuilder で Shape 作成
       ↓
BuildingAuthoring をアタッチ
       ↓
Catalog・生成設定を入力
       ↓
[Rebuild All] ボタンを押す
       ↓
Semantic 自動分類 → モジュール配置
       ↓
Scene ビューで確認
       ↓
（必要なら）Semantic Editor で手動修正
       ↓
[Rebuild All] で再生成（手動修正が引き継がれる）
```

---

## Rebuild All

Inspector 下部の **Rebuild All** ボタンを押すと以下が実行されます。

1. 既存の生成オブジェクトを全削除
2. ProBuilder フェイスを再解析して Semantic を自動分類
3. 前回の手動修正を新フェイスへ再マッピング
4. 壁・屋根・床のモジュールを配置
5. Hierarchy に `GeneratedObjects` 子が生成される

> `Module Catalog` が未設定の場合はボタンが無効になります。

---

## Semantic オーバーレイ

`BuildingAuthoring` を選択中、Scene ビューに面の色が表示されます。

| 色 | Semantic |
|---|---|
| 赤 | OuterWall（外壁） |
| マゼンタ | InnerWallCandidate（内壁候補） |
| 緑 | Floor（床） |
| 青 | Roof（屋根） |
| シアン | CeilingCandidate（天井候補） |
| グレー | SlopedFace（斜め面・生成対象外） |
| 黄 | OpeningHost（開口候補） |

**オレンジのアウトライン**が付いている面は推定精度が低い（confidence < 0.75）ことを示します。Semantic Editor で確認・修正してください。

`Semantic オーバーレイ表示` チェックを外すと非表示にできます。

---

## Semantic Editor

Inspector の **「Semantic Editor を開く」** ボタンで起動します。

### 画面構成

```
[リフレッシュ]

ID:1  N:(0.0, 0.0, 1.0)  conf:1.00    [Semantic ドロップダウン]
ID:2  N:(1.0, 0.0, 0.0)  conf:1.00    [Semantic ドロップダウン]
ID:3  N:(0.0, 1.0, 0.0)  conf:1.00    [Semantic ドロップダウン]
...

[変更を適用 (Rebuild なし)]
```

### 操作手順

1. 変更したい面の右端ドロップダウンから Semantic を選ぶ
2. Scene ビューの色がリアルタイムで変わることを確認
3. `[変更を適用]` ボタンで保存（Scene が Dirty 状態になるので保存を忘れずに）
4. 必要なら Inspector から `Rebuild All` を押して再配置

| 表示 | 意味 |
|---|---|
| `手動` ラベル（黄色） | この面は手動修正済み。Rebuild All 後も引き継がれる |
| `⚠` マーク | confidence < 0.75。推定が不確かな面 |

### 対象の切り替え

Hierarchy で別の `BuildingAuthoring` オブジェクトを選択すると、ウィンドウが自動で切り替わります。

---

## 余り処理の仕組み

壁面の幅がモジュール幅で割り切れない場合、右端の余りを以下の順で処理します。

```
Step 1: AdjustableWall があり、スケール範囲内なら採用
Step 2: TrimWall があり、余り ≤ maxTrimCoverageRatio × moduleWidth なら右端揃えで配置
Step 3: PrimaryWall の allowScale = true で、スケール範囲内なら軽微スケール
失敗:   Warning を出して該当区画は未生成
```

> `TrimWall` はサイズを余りに合わせず固定幅で配置します（余りを視覚的に隠す目的）。
> TrimWall の幅が余りより大きい場合は採用されません。

---

## フォルダ構成（参考）

```
Assets/
├─ _Project/
│   ├─ Scripts/Building/
│   │   ├─ Core/          BuildingAuthoring, SemanticStore, GeneratedObjectRegistry
│   │   ├─ Data/          FaceSemantic, ModuleRole, GeometrySignature ...
│   │   └─ Services/      FaceSemanticAnalyzer, WallGenerator ...
│   └─ Scripts/Editor/Building/
│       ├─ BuildingAuthoringEditor.cs
│       ├─ SemanticEditorWindow.cs
│       └─ SemanticVisualizationDrawer.cs
└─ (任意のフォルダ)/
    ├─ BuildingModuleCatalog.asset
    ├─ ModuleEntry_Wall.asset
    ├─ ModuleEntry_Roof.asset
    └─ Prefabs/
        ├─ WallModule_Primary.prefab
        └─ RoofModule_Flat.prefab
```

---

## よくあるトラブル

| 症状 | 確認箇所 |
|---|---|
| Scene に色が表示されない | Rebuild All をまず押す / `Semantic オーバーレイ表示` が ON か確認 |
| 壁が 1 点に重なって配置される | Prefab ルートの Scale が (1,1,1) か確認 / Pivot が底辺中央か確認 |
| 壁が配置されない（Warningが出る） | `Primary Wall Module` に ModuleEntry がセットされているか確認 |
| 屋根が巨大 or ゼロスケール | ModuleEntry の `nominalSize X/Z` が 0 になっていないか確認 |
| 手動修正が Rebuild 後に消える | `[変更を適用]` を押してから Scene を保存 → Rebuild All の順にする |
| `GeneratedObjects` が空のまま | Console の Warning を確認。Catalog や Prefab の設定漏れが多い |

---

## v0.1 の制限事項

- 対応形状は**直方体のみ**（L字・凹形状は未検証）
- 再生成は **Rebuild All のみ**（部分再生成は v0.3 予定）
- SlopedFace（斜め面）は可視化のみ、モジュール配置対象外
- 窓・ドア・開口の配置は v0.2 予定
- Bake（静的メッシュ結合）は v0.2 予定
