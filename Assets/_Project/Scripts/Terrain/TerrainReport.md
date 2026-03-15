# マインクラフト風プロシージャル地形 実装レポート

**作成日**: 2026-03-15
**名前空間**: `LibraryOfGamecraft.Terrain`
**依存ライブラリ**: `LibraryOfGamecraft.Noise` (SimplexNoise / FractalNoise)

---

## 概要

シンプレックスノイズライブラリを基盤に、ボクセルベースのプロシージャル地形を生成するシステム。
チャンク単位のデータ管理・可視面カリング・コルーチンによる非同期生成の3本柱で構成される。

---

## ファイル構成

```
Assets/_Project/Scripts/Terrain/
├── BlockType.cs          # ブロック種別 enum
├── BlockRegistry.cs      # ブロック定義 ScriptableObject
├── ChunkData.cs          # ボクセルデータ (純粋データクラス)
├── TerrainSettings.cs    # 地形生成パラメータ ScriptableObject
├── TerrainGenerator.cs   # ノイズ → ボクセル変換 (静的)
├── MeshBuilder.cs        # ボクセル → メッシュ変換 (静的)
├── Chunk.cs              # 1チャンク描画 MonoBehaviour
├── ChunkManager.cs       # チャンクライフサイクル管理 MonoBehaviour
└── VertexColor.shader    # 頂点カラー対応 Unlit シェーダ
```

---

## アーキテクチャ図

```
ChunkManager
  │  Update()
  │  ├─ EnqueueVisibleChunks()   プレイヤー周辺の未ロードチャンクをキューに積む
  │  └─ UnloadDistantChunks()    遠いチャンクをプールに返す
  │
  │  Coroutine: GenerationLoop() (1フレーム = chunksPerFrame チャンク)
  │    │
  │    ├─ TerrainGenerator.Generate(chunkPos, settings)
  │    │    └─ ChunkData  ← ノイズで各 (x,y,z) のブロック種別を決定
  │    │
  │    └─ Chunk.Apply(data, worldBlockAt)
  │         └─ MeshBuilder.Build(data, worldBlockAt)
  │              └─ Mesh (頂点カラー + UV + 可視面カリング)
  │
  └─ Object Pool  Queue<Chunk>  再利用でインスタンス生成コストを削減
```

---

## 各ファイルの詳細

### 1. `BlockType.cs`

ブロック種別を `byte` enum で定義。1ブロック = 1バイトに収めてメモリを節約する。

| 値 | 名前    | 用途              |
|----|---------|-------------------|
| 0  | Air     | 空気 (描画なし)   |
| 1  | Grass   | 草地の表面        |
| 2  | Dirt    | 土 (地表から3層)  |
| 3  | Stone   | 岩盤              |
| 4  | Sand    | 砂漠バイオーム    |
| 5  | Gravel  | 砂利 (石との境界) |
| 6  | Bedrock | 最下層 (y=0固定)  |
| 7  | Snow    | 雪バイオームの表面|
| 8  | Log     | 木の幹 (拡張用)   |
| 9  | Leaves  | 葉 (拡張用)       |
| 10 | Water   | 水 (海面充填)     |

---

### 2. `BlockRegistry.cs`

ブロック種別に色・UV タイル位置・solid フラグを対応づける ScriptableObject。

```
Assets > Create > LibraryOfGamecraft > Terrain > BlockRegistry
```

#### `BlockDefinition` 構造体

| フィールド   | 型          | 説明                              |
|------------|-------------|----------------------------------|
| `name`     | string      | デバッグ用の表示名                |
| `isSolid`  | bool        | 隣接面を隠すかどうか              |
| `color`    | Color       | 頂点カラーモードの基本色          |
| `topTile`  | Vector2Int  | UV アトラス上のタイル座標 (上面)  |
| `sideTile` | Vector2Int  | UV アトラス上のタイル座標 (側面)  |
| `bottomTile`| Vector2Int | UV アトラス上のタイル座標 (底面)  |

#### フォールバック

ScriptableObject が未設定の場合は `GetDefault(BlockType)` のハードコード値を使用する。
デフォルト状態でも頂点カラーのみで正常に動作する。

---

### 3. `ChunkData.cs`

1チャンク分のボクセルデータを保持する純粋データクラス。

| 定数          | 値  | 説明               |
|--------------|-----|--------------------|
| `Width`      | 16  | X 方向のブロック数 |
| `Height`     | 128 | Y 方向のブロック数 |
| `Depth`      | 16  | Z 方向のブロック数 |

**メモリ使用量**: `16 × 128 × 16 × 1 byte = 32,768 byte ≒ 32 KB / チャンク`

#### 内部配列のレイアウト

```
index = x + Width * (y + Height * z)
```

X → Y → Z の順でイテレートするとキャッシュヒット率が高くなる。

---

### 4. `TerrainSettings.cs`

地形生成に関わるパラメータをまとめた ScriptableObject。

```
Assets > Create > LibraryOfGamecraft > Terrain > TerrainSettings
```

| フィールド          | デフォルト | 説明                              |
|--------------------|-----------|----------------------------------|
| `worldSeed`        | 42        | 乱数シード                        |
| `seaLevel`         | 48        | 海面高さ (ブロック)               |
| `baseHeight`       | 52        | 地表基準高さ                      |
| `heightAmplitude`  | 28        | 地表高さの最大振れ幅              |
| `mountainAmplitude`| 32        | 山岳ノイズの振れ幅                |
| `surfaceFrequency` | 0.004     | 地表ノイズのサンプリング周波数    |
| `mountainFrequency`| 0.002     | 山岳ノイズのサンプリング周波数    |
| `caveFrequency`    | 0.05      | 洞窟ノイズのサンプリング周波数    |
| `caveThreshold`    | 0.72      | 洞窟化するノイズ閾値 (0‥1)       |
| `caveMaxHeight`    | 50        | 洞窟が生成される最大高さ          |
| `biomeFrequency`   | 0.0015    | バイオーム気温ノイズの周波数      |
| `desertThreshold`  | 0.65      | この気温以上 → 砂漠              |
| `snowThreshold`    | 0.28      | この気温以下 + 高地 → 雪         |

各ノイズには `NoiseSettings` の参照を持ち、オクターブや persistence を個別に調整できる。

---

### 5. `TerrainGenerator.cs`

チャンク座標を受け取り `ChunkData` を生成する静的クラス。

#### 生成パイプライン

```
入力: Vector2Int chunkPos
        │
        ▼
[1] SampleSurfaceHeight(wx, wz)
        │  地表fBm + 山岳リッジドを合算
        │  surfaceY = baseHeight + fBm×heightAmplitude + ridged×mountainAmplitude
        │
        ▼
[2] バイオーム判定 (SimplexNoise 1サンプル)
        │  temp > desertThreshold → 砂漠
        │  temp < snowThreshold  → 雪
        │
        ▼
[3] 縦方向ループ (y=0..127)
        │  DetermineBlock(wx, y, wz, surfaceY, biome)
        │    y == 0              → Bedrock
        │    y < surfaceY-3      → 洞窟判定 → Air or Stone
        │    y < surfaceY        → Dirt / Sand (バイオーム)
        │    y == surfaceY       → Grass / Sand / Snow (バイオーム)
        │    y > surfaceY        → Air
        │
        ▼
[4] 海水充填
        │  surfaceY < seaLevel の列に Water を充填
        │
        ▼
出力: ChunkData
```

#### 洞窟生成

3D シンプレックスノイズを密度場として使用する。
`noise(x, y, z) > caveThreshold` の領域を Air に置き換える。

```
// caveFrequency=0.05, caveThreshold=0.72 が推奨値
// 閾値を下げると洞窟が増え、上げると減る
float cave = SimplexNoise.Evaluate01(wx * freq, y * freq, wz * freq);
if (cave > caveThreshold) → Air
```

地表付近 (`y >= surfaceY - 3`) は洞窟カービングをスキップして崩落を防ぐ。

---

### 6. `MeshBuilder.cs`

`ChunkData` から Unity `Mesh` を構築する静的クラス。
**可視面カリング**により、隣接ブロックに隠れた面は追加しない。

#### 面定義

各ブロックは 6 面を持つ。各面は 4 頂点 + 2 三角形 (6 インデックス) で構成される。

| 面インデックス | 方向    | 法線          | 明るさ係数 |
|-------------|---------|--------------|-----------|
| 0           | Top     | +Y           | 1.0       |
| 1           | Bottom  | -Y           | 0.5       |
| 2           | North   | +Z           | 0.8       |
| 3           | South   | -Z           | 0.8       |
| 4           | East    | +X           | 0.9       |
| 5           | West    | -X           | 0.9       |

面ごとに明るさ係数を乗算することで、シェーダなしでも立体感を表現する疑似 AO を実現している。

#### 可視面カリングのロジック

```
for each block (lx, ly, lz):
    if block == Air: skip
    for each face (6方向):
        neighbor = 隣接ブロック (チャンク外は worldBlockAt コールバックで取得)
        if neighbor.isSolid: skip (面は見えないので追加しない)
        → 面を追加 (4頂点 / 6インデックス / 4UV / 4頂点カラー)
```

チャンク境界では `worldBlockAt(lx, ly, lz)` が呼ばれ、`ChunkManager` が隣チャンクのデータを返す。
隣チャンクが未ロードの場合は `Air` 扱いとなり、境界面が描画される。

#### 65535 頂点超え対応

```csharp
if (verts.Count > 65535)
    mesh.indexFormat = IndexFormat.UInt32;
```

大きなチャンクでも自動的に 32bit インデックスに切り替わる。

---

### 7. `Chunk.cs`

1チャンクの描画を担う MonoBehaviour。
`MeshFilter` / `MeshRenderer` / `MeshCollider` を自動取得する。

| メソッド                     | 説明                                          |
|-----------------------------|---------------------------------------------|
| `Initialize(pos, material)` | チャンク座標・マテリアルをセットし座標を移動  |
| `Apply(data, worldBlockAt)` | ChunkData をセットしメッシュを構築            |
| `RebuildMesh(worldBlockAt)` | 既存 Data からメッシュだけ再構築              |

**メッシュのライフサイクル管理**: `DestroyCurrentMesh()` で古い Mesh を明示的に破棄し、メモリリークを防ぐ。

---

### 8. `ChunkManager.cs`

プレイヤーの移動に合わせてチャンクをロード / アンロードする MonoBehaviour。

#### ロード処理フロー

```
Update()
  └─ プレイヤーが別チャンクに移動したとき:
       ├─ EnqueueVisibleChunks()
       │    viewDistance 内の未ロードチャンクを距離順にキューに追加
       └─ UnloadDistantChunks()
            viewDistance+1 を超えたチャンクをプールに返す

Coroutine: GenerationLoop()
  └─ 毎フレーム chunksPerFrame 個のチャンクを生成
       ├─ TerrainGenerator.Generate()  → ChunkData
       ├─ Chunk.Apply()                → Mesh
       └─ RemeshNeighbors()            → 隣接チャンクの境界面を再構築
```

#### オブジェクトプール

`GameObject` の生成・破棄コストを削減するため `Queue<Chunk>` でプールを管理する。

```
GetOrCreateChunk():  pool.Count > 0 → Dequeue & SetActive(true)
                     pool.Count = 0 → new GameObject + AddComponent<Chunk>

ReturnToPool():      pool.Count < poolSize → SetActive(false) & Enqueue
                     pool.Count >= poolSize → Destroy
```

#### チャンク境界の世界座標変換

```csharp
// ローカル座標 (lx, lz) が境界を越えた場合:
int wx = chunkPos.x * ChunkData.Width + lx;
int targetCx = Mathf.FloorToInt((float)wx / ChunkData.Width);
// → 隣チャンクの Dictionary キーを計算して BlockType を返す
```

負の座標でも `FloorToInt` が正しく動作する。

#### Gizmos (エディタ可視化)

`OnDrawGizmosSelected` で viewDistance 範囲のチャンク境界を緑のワイヤーフレームで表示する。

---

### 9. `VertexColor.shader`

頂点カラーと UV アトラステクスチャを乗算して出力するシンプルな Unlit シェーダ。

```hlsl
fixed4 frag(v2f i) : SV_Target
{
    fixed4 texColor = tex2D(_MainTex, i.uv);
    return texColor * i.color;  // テクスチャ × 頂点カラー
}
```

- **アトラスなし (white テクスチャ)**: 頂点カラーのみで色付き地形が表示される
- **アトラスあり**: テクスチャとカラーが乗算されブロックテクスチャが描画される

---

## セットアップ手順

### 最小構成 (即動作)

1. `Assets > Create > Material` → Shader: `LibraryOfGamecraft/VertexColor`
2. 空の GameObject を作成 → `ChunkManager` をアタッチ
3. インスペクタ設定:
   - **Player**: `Camera.main` の Transform (省略時は自動取得)
   - **Chunk Material**: 手順1で作ったマテリアル
   - **Terrain Settings**: 空欄でも動作 (デフォルト値を使用)
4. Play → 自動で地形が生成される

### 推奨構成 (パラメータ調整あり)

```
Assets/
  _Project/
    Settings/
      TerrainSettings.asset    ← TerrainSettings ScriptableObject
      SurfaceNoise.asset        ← NoiseSettings (地表用)
      MountainNoise.asset       ← NoiseSettings (山岳用)
      CaveNoise.asset           ← NoiseSettings (洞窟用)
      BlockRegistry.asset       ← BlockRegistry ScriptableObject
```

TerrainSettings の `surfaceNoise / mountainNoise / caveNoise` に各 NoiseSettings を設定することで、Inspector からリアルタイムにパラメータを調整できる。

---

## パフォーマンス指標 (目安)

| 設定                          | 1フレームの生成数 | 推奨用途           |
|------------------------------|------------------|--------------------|
| `viewDistance=4, chunksPerFrame=1` | 1           | モバイル・低スペック |
| `viewDistance=6, chunksPerFrame=2` | 2           | 標準 PC            |
| `viewDistance=8, chunksPerFrame=4` | 4           | 高スペック PC      |

- **メモリ**: `viewDistance=6` のとき最大 (2×6+1)² ≒ 169 チャンク × 32 KB ≒ **5.4 MB**
- **頂点数**: 地表のみのチャンクで最大約 **16,000 頂点 / チャンク** (可視面カリング後)
- **DrawCall**: 1チャンク = 1 DrawCall (サブメッシュ分割なし)

---

## 既知の制限と今後の拡張候補

| 制限                             | 対応策 (拡張候補)                              |
|---------------------------------|--------------------------------------------|
| 水が不透明 (solid 扱い)          | 半透明サブメッシュの追加 + 描画順の制御       |
| メッシュ生成がメインスレッド      | C# Job System + Burst コンパイラで並列化     |
| 動的なブロック変更非対応          | `Chunk.RebuildMesh()` 呼び出しで対応可能     |
| LOD なし                        | 遠距離チャンクを低解像度メッシュに差し替え   |
| バイオームが気温1軸のみ           | 湿度ノイズを追加して 2D バイオームマップに   |
| 木・構造物の生成なし             | 地表高さが決まった後にオブジェクト配置を追加 |

---

## 関連ドキュメント

- [ノイズライブラリ実装レポート](../Libraries/Noise/NoiseReport.md)
