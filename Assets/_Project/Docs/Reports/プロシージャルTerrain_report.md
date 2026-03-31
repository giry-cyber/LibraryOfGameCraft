# プロシージャルTerrain レポート

## 概要

Unity Terrain コンポーネントを対象としたプロシージャルハイトマップ生成システム（Phase 1）。
シンプレックスノイズの fBm 合成と任意のドメインワープを組み合わせて地形起伏を生成し、
バイナリファイルへのキャッシュ保存・エディタ GUI からの操作に対応する。

---

## 設計

### クラス構成

| クラス / インターフェース | 責務 |
|--------------------------|------|
| `INoise2D` | 2D ノイズサンプラーの抽象インターフェース |
| `INoiseFactory` | `INoise2D` のファクトリ抽象インターフェース |
| `IDomainWarp2D` | 2D ドメインワープの抽象インターフェース |
| `IDomainWarp2DFactory` | `IDomainWarp2D` のファクトリ抽象インターフェース |
| `FractalNoise2D` | fBm（フラクタルブラウン運動）ノイズ実装 |
| `FractalNoise2DFactory` | `FractalNoise2D` の生成ファクトリ |
| `SimplexDomainWarp2D` | シンプレックスノイズを使ったドメインワープ実装 |
| `SimplexDomainWarp2DFactory` | `SimplexDomainWarp2D` の生成ファクトリ |
| `TerrainGenerationProfile` | 地形生成パラメータ ScriptableObject |
| `TerrainPersistentData` | 生成済みバイナリのパス管理 ScriptableObject |
| `HeightMapIO` | float 配列の float32 バイナリ読み書き（静的） |
| `TerrainGenerator` | ノイズ → float[] ハイトマップ変換 |
| `TerrainApplier` | float[] → Unity Terrain 適用（静的） |
| `TerrainBuildService` | Phase 1 フロー全体のオーケストレーション |
| `TerrainToolWindow` | EditorWindow（Generate タブ） |
| `TreePrototypeRule` | 植生配置ルール（Phase 2 用定義、Phase 1 では未使用） |

### ファイル構成

```
Assets/_Project/Scripts/
├── Terrain/
│   ├── Noise/
│   │   ├── INoise2D.cs
│   │   ├── INoiseFactory.cs
│   │   ├── IDomainWarp2D.cs
│   │   ├── IDomainWarp2DFactory.cs
│   │   ├── FractalNoise2D.cs
│   │   ├── FractalNoise2DFactory.cs
│   │   ├── SimplexDomainWarp2D.cs
│   │   └── SimplexDomainWarp2DFactory.cs
│   ├── Data/
│   │   ├── TerrainGenerationProfile.cs
│   │   └── TerrainPersistentData.cs
│   ├── IO/
│   │   └── HeightMapIO.cs
│   └── Services/
│       ├── TerrainGenerator.cs
│       ├── TerrainApplier.cs
│       └── TerrainBuildService.cs
└── Editor/
    └── Terrain/
        └── TerrainToolWindow.cs
```

### 依存関係・使い方

```
TerrainToolWindow
  └─ TerrainBuildService.Build(terrain, profile, persistentData, tileOrigin)
       ├─ FractalNoise2DFactory.Create(seed)  → FractalNoise2D  (INoise2D)
       ├─ SimplexDomainWarp2DFactory.Create(seed)  → SimplexDomainWarp2D  (IDomainWarp2D)
       ├─ TerrainGenerator.Generate(profile, tileOrigin)  → float[]
       ├─ HeightMapIO.Load(manualDeltaPath)  → float[]
       ├─ TerrainApplier.Apply(terrain, generated, delta, ...)
       └─ HeightMapIO.Save(generated, generatedHeightPath)
```

既存ノイズライブラリへの依存:
- `LibraryOfGamecraft.Noise.SimplexNoise` — `SetSeed(int)` / `Evaluate(float, float)`

---

## 実装メモ

### ノイズ正規化
`SimplexNoise.Evaluate` の戻り値は [-1, 1]。fBm ループ内で各オクターブを
`(value * 0.5f + 0.5f)` で [0, 1] に変換してから振幅を掛け、最後に maxValue で除算することで
出力を [0, 1] に保証している。

### タイル境界の一致
`TerrainGenerator.Generate` はワールド座標基準でサンプリングする。
隣接タイルが同じノイズ関数・同じシードであれば境界高さが一致し、継ぎ目のない地形が得られる。

### ドメインワープの独立性近似
`SimplexDomainWarp2D` の X 軸・Z 軸ワープは同一ノイズ関数を使うが、
Z 軸サンプルに `(+3.7, +1.3)` のオフセットを加えることで統計的に独立した2つのノイズ場を近似する。

### HeightMapIO の AssetDatabase ガード
エディタ外ビルド（ランタイム）では `AssetDatabase` / `EditorUtility` は使用不可のため、
関連呼び出しを `#if UNITY_EDITOR` ブロックで囲んでいる。
ファイル IO 自体（`File.WriteAllBytes` 等）はランタイムでも動作する。

---

## 既知の制限・TODO

- [ ] Phase 2: 手動デルタ編集ツール（Paint タブ）の実装
- [ ] Phase 3: 植生配置（Vegetation タブ）の実装
- [ ] マルチスレッド生成（C# Job System）への対応
- [ ] ランタイム（非エディタ）での `TerrainBuildService` 動作確認
- [ ] `SimplexNoise.SetSeed` はグローバル状態を変更するため、並列呼び出しでレースコンディションが起きる可能性がある

---

## 変更履歴

| 日付 | 内容 |
|------|------|
| 2026-03-31 | Phase 1 初版作成（ノイズ抽象層・ハイトマップ生成・エディタ GUI） |
