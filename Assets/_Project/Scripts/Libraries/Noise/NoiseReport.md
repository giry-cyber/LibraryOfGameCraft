# シンプレックスノイズ ライブラリ 実装レポート

**作成日**: 2026-03-15
**名前空間**: `LibraryOfGamecraft.Noise`

---

## 概要

Ken Perlin が 2001 年に発表したシンプレックスノイズアルゴリズムを Unity (C#) 向けに実装したライブラリ。
標準の Perlin ノイズと比較して以下の利点がある。

| 比較項目 | Perlin ノイズ | シンプレックスノイズ |
|---|---|---|
| 計算量 (nD) | O(2ⁿ) | O(n²) |
| 方向性アーティファクト | 格子軸方向に目立つ | 少ない |
| 高次元への拡張 | コスト増大 | スムーズに拡張可能 |

---

## ファイル構成

```
Assets/_Project/Scripts/Libraries/Noise/
├── SimplexNoise.cs    # コアアルゴリズム (2D/3D/4D)
├── NoiseSettings.cs   # パラメータ設定 (ScriptableObject)
├── FractalNoise.cs    # fBm・各種派生ノイズ、テクスチャ生成
└── NoiseReport.md     # 本ドキュメント
```

---

## 各クラスの詳細

### 1. `SimplexNoise` (静的クラス)

シンプレックスノイズの純粋な計算を行うコアクラス。

#### 主要メソッド

| シグネチャ | 説明 |
|---|---|
| `Evaluate(float x, float y)` | 2D ノイズ |
| `Evaluate(float x, float y, float z)` | 3D ノイズ |
| `Evaluate(float x, float y, float z, float w)` | 4D ノイズ |
| `Evaluate(Vector2/3/4 pos)` | Vector 型オーバーロード |
| `Evaluate01(...)` | [0,1] 正規化版 |
| `SetSeed(int seed)` | パーミュテーションテーブルを再初期化 |

- 戻り値はすべて **[-1, 1]**
- `SetSeed` はフィッシャー‐イェーツシャッフルでパーミュテーションテーブルを構築
- 内部の計算に `AggressiveInlining` を適用しパフォーマンスを最適化

#### アルゴリズムの要点

```
1. 入力座標をスキュー変換 → 超立方体の格子に写像
2. 最も近い単体 (simplex) を特定
3. 各頂点の寄与をグラジェントとの内積 × 減衰カーネルで計算
4. 全頂点の寄与を合算してスケーリング
```

#### グラジェントテーブル

- **2D/3D**: 12 方向の整数グラジェント (`Grad3`)
- **4D**: 32 方向の整数グラジェント (`Grad4`)

---

### 2. `NoiseSettings` (ScriptableObject)

ノイズ生成のパラメータを一元管理する ScriptableObject。

| フィールド | 型 | デフォルト | 説明 |
|---|---|---|---|
| `seed` | int | 0 | 乱数シード |
| `frequency` | float | 1.0 | サンプリング周波数 |
| `octaves` | int | 4 | オクターブ数 (1‥8) |
| `amplitude` | float | 1.0 | 初期振幅 |
| `persistence` | float | 0.5 | オクターブ間の振幅減衰率 |
| `lacunarity` | float | 2.0 | オクターブ間の周波数倍率 |
| `offset` | Vector3 | (0,0,0) | ノイズ空間のオフセット |

**使い方 (ScriptableObject として)**

1. `Assets > Create > LibraryOfGamecraft > Noise > NoiseSettings` でアセットを作成
2. インスペクタ上でパラメータを調整
3. スクリプトから参照して `FractalNoise.Evaluate` に渡す

---

### 3. `FractalNoise` (静的クラス)

`SimplexNoise` を複数オクターブ重ね合わせ、地形・雲などに適したノイズを生成する。

#### 提供するノイズ種別

| メソッド | 出力範囲 | 用途例 |
|---|---|---|
| `Evaluate` | [-1, 1] | 汎用 fBm |
| `Evaluate01` | [0, 1] | テクスチャ、高さマップ |
| `Ridged` | [0, 1] | 山の稜線、岩肌 |
| `Turbulence` | [0, 1] | 炎、雲、水面 |
| `GenerateTexture` | — | Texture2D を直接生成 |

#### fBm の計算式

```
value = Σ(i=0..octaves-1) [ noise(pos * freq * lacunarity^i) * amplitude * persistence^i ]
value /= Σ amplitude_i  ← 正規化
```

#### `GenerateTexture` の使い方

```csharp
var settings = ScriptableObject.CreateInstance<NoiseSettings>();
settings.octaves   = 6;
settings.frequency = 3f;

Texture2D tex = FractalNoise.GenerateTexture(512, 512, settings, NoiseTextureMode.Ridged);
GetComponent<Renderer>().material.mainTexture = tex;
```

---

## 使用例

### 地形の高さを取得する

```csharp
using LibraryOfGamecraft.Noise;

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] private NoiseSettings noiseSettings;
    [SerializeField] private float heightScale = 20f;

    private void GenerateMesh(Mesh mesh)
    {
        var vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            float h = FractalNoise.Evaluate01(
                vertices[i].x, vertices[i].z, noiseSettings);
            vertices[i].y = h * heightScale;
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }
}
```

### 2D ノイズマップをテクスチャに焼く

```csharp
var tex = FractalNoise.GenerateTexture(256, 256, noiseSettings, NoiseTextureMode.FBm);
rawImage.texture = tex;
```

### シードを切り替えてランダム地形を生成する

```csharp
noiseSettings.seed = Random.Range(0, int.MaxValue);
// 次の Evaluate 呼び出しから新しいパターンが適用される
```

---

## パフォーマンス考慮事項

| 事項 | 詳細 |
|---|---|
| `SetSeed` のコスト | パーミュテーションテーブル (512 要素) を再構築するため、毎フレーム呼ぶのは避けること |
| 高オクターブ数 | `octaves=8` は `octaves=1` の約 8 倍の計算コスト。リアルタイム用途では 4‥5 推奨 |
| Job System 対応 | 現状は `static` クラスのため `Burst` / `IJob` に直接渡すことはできない。並列化が必要な場合は純粋関数に切り出してバーストコンパイル可能な構造体に変換すること |
| `GenerateTexture` | テクスチャ生成は重い処理のため、起動時や非同期コルーチン内で行うことを推奨 |

---

## 今後の拡張候補

- [ ] Burst/Jobs 対応版 (`NativeArray` 出力)
- [ ] Worley (Cellular) ノイズの追加
- [ ] Domain Warping (ノイズ座標をノイズで歪める) ヘルパー
- [ ] ComputeShader によるテクスチャ生成の GPU 実装
- [ ] アニメーション用タイムパラメータ付きオーバーロード

---

## 参考文献

- Ken Perlin, "Noise hardware", 2001. (SIGGRAPH Course Notes)
- Stefan Gustavson, "Simplex noise demystified", 2005.
- [https://weber.itn.liu.se/~stegu/simplexnoise/simplexnoise.pdf](https://weber.itn.liu.se/~stegu/simplexnoise/simplexnoise.pdf)
