# Figma Importer for Unity

Figma のデザインデータを Unity の uGUI 階層に変換するエディタ拡張です。

---

## 必要環境

- Unity 2021.3 以降
- TextMeshPro パッケージ（Package Manager からインストール済みであること）

---

## インストール

スクリプトはプロジェクトに同梱済みです。Unity でプロジェクトを開くと自動的にコンパイルされます。

---

## 使い方

### 1. Figma から JSON を取得する

Figma MCP または Figma REST API を使ってファイルデータを取得します。

**Figma MCP を使う場合（Claude Code）:**

Claude Code のチャットで以下のように依頼します。

```
Figma の <ファイルURL> のデザインデータを JSON で取得して
```

取得した JSON をコピーしておきます。

**Figma REST API を直接使う場合:**

```
GET https://api.figma.com/v1/files/{file_key}
Header: X-Figma-Token: <Personal Access Token>
```

Personal Access Token は Figma の「Settings > Account > Personal access tokens」から発行できます。

---

### 2. ウィンドウを開く

Unity のメニューバーから **Window > Figma Importer** を選択します。

---

### 3. Canvas を用意する

インポート先の Canvas をシーンに用意します。なければ **GameObject > UI > Canvas** で作成してください。

ウィンドウの「Target Canvas」欄に、シーン上の Canvas をドラッグ＆ドロップします。

---

### 4. JSON を貼り付けてインポートする

テキストエリアに手順 1 で取得した JSON を貼り付け、「インポート」ボタンを押します。

インポートが完了すると Canvas の子として uGUI の GameObject 階層が生成されます。

> インポート操作は **Undo 対応**です。失敗した場合は `Ctrl+Z` で元に戻せます。

---

## 対応ノードタイプ

| Figma ノード | 生成される Unity オブジェクト | 備考 |
|---|---|---|
| FRAME | RectTransform + Image | 背景色あり |
| GROUP | RectTransform + Image | 背景色あり |
| COMPONENT | RectTransform + Image | Prefab 化はしない |
| INSTANCE | RectTransform + Image | Prefab 化はしない |
| SECTION | RectTransform + Image | |
| RECTANGLE | RectTransform + Image | Fill の最初の SOLID 色を適用 |
| TEXT | RectTransform + TextMeshProUGUI | フォントサイズ・配置・色を適用 |

---

## スキップされるノードタイプ

以下のノードは警告なしにスキップされます（Console にログが出ます）。

- VECTOR
- ELLIPSE
- LINE
- STAR
- POLYGON
- BOOLEAN_OPERATION
- SHAPE_WITH_HOLES
- SLICE
- CONNECTOR
- `visible: false` のすべてのノード

---

## 既知の制限

| 項目 | 状況 |
|---|---|
| フォント | Figma 側のフォントは再現されません。プロジェクトのデフォルト TMP フォントが使用されます |
| 画像素材 | Figma の Image Fill はスキップされます。素材は別途 Unity にインポートして差し替えてください |
| Auto Layout | 再現されません。サイズと位置のみ反映されます |
| 複数ページ | ファイル内の最初のページのみが対象です |
| エフェクト | Shadow / Blur / Overlay 等は無視されます |
| グラデーション | GRADIENT 系の Fill はスキップされ、白で代替されます |

---

## ファイル構成

```
Assets/_Project/Scripts/Editor/FigmaImporter/
├── FigmaData.cs            Figma JSON のデータモデル定義
├── FigmaNodeParser.cs      JSON パース & ノード判定ユーティリティ
├── FigmaToUnity.cs         FigmaNode → uGUI GameObject 変換ロジック
├── FigmaImporterWindow.cs  エディタウィンドウ本体
└── README.md               本ドキュメント
```

---

## トラブルシューティング

**「JSON のパースに失敗しました」と表示される**
- Figma API のレスポンス全体ではなく、`document` を含むトップレベルの JSON を貼り付けているか確認してください。
- Unity の Console に詳細なエラーが出力されます。

**TextMeshPro が見つからないエラーが出る**
- Package Manager から `TextMeshPro` をインポートしてください。
- `Window > TextMeshPro > Import TMP Essential Resources` も実行してください。

**Canvas に何も生成されない**
- Figma ファイルにページが存在するか確認してください。
- ページの子ノードがすべてスキップ対象（VECTOR 等）になっていないか Console ログを確認してください。
