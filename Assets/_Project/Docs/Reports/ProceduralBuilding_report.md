# ProceduralBuilding レポート

## 概要

Unity HDRP 環境で建物を効率的に制作するための **半自動プロシージャル建物生成ツール**。
ProBuilder で作成したラフ形状（Shape）から面の建築的意味（Semantic）を自動推定し、
壁・屋根・床などのモジュール Prefab を自動配置する。

仕様バージョン: v1.5 / 実装バージョン: v0.1

---

## 設計

### クラス構成

#### Data 層（`Scripts/Building/Data/`）

| クラス | 責務 |
|--------|------|
| `FaceSemantic` | 面の意味を表す enum（OuterWall, Roof, Floor など） |
| `ModuleRole` | モジュールの役割 enum（PrimaryWall, TrimWall, FlatRoof など） |
| `GeometrySignature` | 再マッピング用の量子化済み幾何特徴セット |
| `FaceSemanticRecord` | SemanticStore が永続化する面 1 枚分のデータ |
| `AnalyzedFace` | 解析実行時の面データ（非シリアライズ） |
| `GeneratedElement` | 生成オブジェクト 1 個のメタ情報 |
| `GenerationSettings` | フロア数・高さ・天井生成フラグなどの生成パラメータ |
| `FloorCeilingSettings` | 床・天井の材質・厚み・コライダー設定 |
| `BuildingModuleEntry` | 1 種類のモジュール Prefab とサイズ情報（ScriptableObject） |
| `BuildingModuleCatalog` | モジュール群を束ねるカタログ（ScriptableObject） |

#### Core 層（`Scripts/Building/Core/`）

| クラス | 責務 |
|--------|------|
| `SemanticStore` | `FaceSemanticRecord` の Scene シリアライズと手動修正の受け付け |
| `GeneratedObjectRegistry` | `elementId ↔ GameObject` の双方向マッピングと生成ルート管理 |
| `BuildingAuthoring` | ProBuilder Shape へアタッチするエントリポイント MonoBehaviour |

#### Service 層（`Scripts/Building/Services/`）

| クラス | 責務 |
|--------|------|
| `FaceSemanticAnalyzer` | ProBuilderMesh を解析して `AnalyzedFace` 群と自動 Semantic を生成 |
| `SemanticRemapper` | Rebuild All 時に旧 SemanticStore の手動修正を新フェイスへ再マッピング |
| `WallGenerator` | OuterWall 面への壁モジュールグリッド配置 |
| `RoofGenerator` | Roof 面への FlatRoof モジュール配置 |
| `FloorCeilingGenerator` | Floor / CeilingCandidate 面の簡易メッシュ生成 |
| `BuildingGenerator` | Rebuild All のオーケストレーター |

#### Editor 層（`Scripts/Editor/Building/`）

| クラス | 責務 |
|--------|------|
| `SemanticVisualizationDrawer` | `[DrawGizmo]` による Scene ビューの Semantic カラーオーバーレイ |
| `BuildingAuthoringEditor` | Inspector UI と Rebuild All ボタン |
| `SemanticEditorWindow` | 面 Semantic 一覧の表示と手動修正 EditorWindow |

### 依存関係・使い方

1. ProBuilder で直方体 Shape を作成する
2. `BuildingAuthoring` コンポーネントを追加する（`SemanticStore`・`GeneratedObjectRegistry` が自動追加される）
3. `BuildingModuleCatalog` アセットを作成し、各モジュール Prefab を登録する
4. Inspector の「Rebuild All」ボタンを押す → 自動分類・モジュール配置が実行される
5. 「Semantic Editor を開く」で面の Semantic を手動修正できる
6. 再度「Rebuild All」を押すと手動修正を保ちながら再生成される

---

## 実装メモ

### Semantic 自動分類（FaceSemanticAnalyzer）

ProBuilderMesh の face インデックスは永続 ID として使わず、
解析サイクルごとに通し番号 (sourceId) を採番する。
再マッピングは `geometrySignature`（量子化幾何特徴）を鍵として行う。

法線の Y 成分で一次分類し、VerticalFace は建物 AABB 中心との Dot 積で OuterWall / InnerWallCandidate を判定する。

### geometrySignature 量子化

| 要素 | 量子化単位 |
|------|-----------|
| quantizedCenter | 0.05 m |
| quantizedNormal | 0.1 |
| quantizedArea | 0.05 m² |
| quantizedBoundsSize | 0.05 m |

再マッピングの主検索は `center + normal + area` の完全一致 → ±1 bin 近傍の順に行う。
`quantizedBoundsSize` は後フィルタ専用。

### 壁グリッド配置（WallGenerator）

- `right = normalize(cross(up, faceNormal))`
- 奥行きは `faceNormal` の XZ 投影 (`forwardXZ`) で Y 汚染を排除
- Pivot = `faceDepthOffset + right * (uMin + col * width + width/2) + up * placementY`
- 余り処理優先順: AdjustableWall → TrimWall（右端揃え、固定幅）→ PrimaryWall 軽微スケール

### Uncertain 可視化

`confidence < 0.75` の面には Semantic 色に加えて Orange のアウトラインを重ね表示する。
Semantic 色は上書きしない（仕様書 4.6）。

---

## 既知の制限・TODO

- [ ] v0.1 対応形状は直方体のみ。L字・凹形状は未検証
- [ ] 部分再生成（v0.3 予定）は未実装。Rebuild All のみ
- [ ] Bake（最終出力、v0.2 予定）は未実装
- [ ] 窓・ドア・開口（OpeningHost の Generate、v0.2 予定）は未実装
- [ ] SlopedFace は可視化のみ、生成対象外
- [ ] SemanticRemapper の近傍スコア計算は quantizedSignature からの逆算値を使用するため誤差がある

---

## 変更履歴

| 日付 | 内容 |
|------|------|
| 2026-04-05 | v0.1 初版作成（仕様書 v1.5 対応） |
