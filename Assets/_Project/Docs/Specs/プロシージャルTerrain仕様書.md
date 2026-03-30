# Unity Terrain プロシージャル地形生成ツール

## 修正版仕様書 v1.2

---

## 1. 文書概要

### 1.1 目的

本仕様書は、Unity Terrain を対象とした**プロシージャル地形生成・編集支援ツール**の要件、設計方針、データ構造、フェーズ別機能、および実装前提を定義するものである。

本ツールは、完全自動生成を目指すものではなく、

* プロシージャル生成で自然な叩き台を高速に作る
* その後に人が編集できる
* 再生成しても人の修正を壊しにくい

というワークフローを支えることを目的とする。

---

### 1.2 対象

* Unity の Terrain システム
* 500m × 500m を 1 タイルとする地形
* エディタ上での生成・編集
* 将来的な複数タイル対応を見据えた単一タイル実装

---

### 1.3 非対象

本仕様書の対象外は以下とする。

* ランタイム生成
* ボクセル地形
* 洞窟、オーバーハング等の 3D 形状
* 独自レンダラによる地形描画
* GPU ベース生成
* 道・川の自動生成
* 高度な侵食シミュレーション
* 独自 Undo/Redo システム

---

## 2. 基本設計方針

### 2.1 基本思想

本ツールは、地形を単なる 1 枚の最終 Heightmap として扱わない。
内部では以下を分離して扱う。

* `generatedHeightMap`
  プロシージャル生成された基礎地形
* `manualDeltaMap`
  ユーザーが後から加えた手修正差分
* `protectedMask`
  再生成時に元の generated を維持する重みマスク
* `noVegetationMask`
  植生配置を禁止するマスク
* `flattenMask`
  将来の平坦化領域用マスク

最終的な Terrain 高さは次で求める。

```text
finalHeightMap = clamp01(generatedHeightMap + manualDeltaMap)
```

---

### 2.2 再生成方針

再生成では、既存の `generatedHeightMap` を上書きするのではなく、新候補 `newGeneratedCandidate` と旧 `generatedHeightMap` を `protectedMask` で合成して更新する。

```text
generatedHeightMap[x,z] =
    lerp(newGeneratedCandidate[x,z], oldGeneratedHeightMap[x,z], protectedMask[x,z])
```

その後、`manualDeltaMap` を再加算して最終地形を再構成する。

---

### 2.3 永続化方針

本ツールでは、以下を**永続化必須**とする。

* `generatedHeightMap`
* `manualDeltaMap`
* `protectedMask`
* `noVegetationMask`
* `TerrainGenerationProfile`

`finalHeightMap` は再計算可能であるため永続化しない。

---

## 3. Unity 固有制約

### 3.1 Heightmap 解像度制約

`heightmapResolution` は Unity Terrain の制約に従い、以下のみ許容する。

* 33
* 65
* 129
* 257
* 513
* 1025
* 2049
* 4097

UI では自由入力を禁止し、選択式とする。

---

### 3.2 高さの内部表現

Unity Terrain の高さは 0.0〜1.0 の正規化値で扱う。
本仕様でも以下を採用する。

* `generatedHeightMap` は 0.0〜1.0
* `manualDeltaMap` は正負を許可
* `finalHeightMap` は Terrain 適用前に 0.0〜1.0 に clamp

---

### 3.3 Heightmap と Alphamap の解像度差

Unity Terrain では以下の解像度が一致しない可能性がある。

* heightmapResolution
* alphamapResolution
* detailResolution

そのため、Phase 4 でテクスチャ塗布に使う高度・傾斜情報は**heightmap 解像度基準**で計算し、alphamap に適用する際は**双線形補間で再サンプリング**する。

---

### 3.4 タイル境界サンプリング

タイル境界の一致は、**同じワールド座標を同じノイズ入力で評価すること**で保証する。

500m タイル、heightmapResolution = R の場合、サンプル点 `(ix, iz)` のワールド座標は次で定義する。

```text
worldX = tileOriginX + (ix / (R - 1)) * tileSizeMeters
worldZ = tileOriginZ + (iz / (R - 1)) * tileSizeMeters
```

ここで `ix, iz` は `0 ～ R-1`。

---

## 4. ノイズ設計

### 4.1 ノイズ抽象化

ノイズ実装はツール本体に内包せず、以下のインターフェースに依存する。

```csharp
public interface INoise2D
{
    float Sample(float x, float z);
}
```

この `INoise2D` は、既存の Simplex Noise 実装、Perlin 実装、外部ライブラリなどをラップして利用できるものとする。

---

### 4.2 Domain Warp の扱い

Domain Warp は単一ノイズサンプルではなく、**座標を別ノイズで歪めてから本体ノイズを評価する処理**である。
このため `INoise2D` だけで完全には表現しない。

v1.2 では以下を採用する。

#### 方針

* 基本ノイズ評価は `INoise2D`
* Domain Warp は専用コンポーネント `IDomainWarp2D` で表現する

```csharp
public interface IDomainWarp2D
{
    Vector2 Warp(float x, float z);
}
```

#### 挙動

```text
warped = domainWarp.Warp(x, z)
height = baseNoise.Sample(warped.x, warped.y)
```

#### 備考

Phase 1 の必須機能ではない。
オプション機能として設計するが、将来実装者が迷わないように責務を分離しておく。

---

## 5. 用語定義

### 5.1 Terrain Tile

500m × 500m の 1 枚の Terrain。

### 5.2 terrainMaxHeightMeters

Terrain の Y 高さ上限。
旧仕様で混在していた `terrainMaxHeight` / `terrainHeightMeters` は、本仕様では **terrainMaxHeightMeters** に統一する。

### 5.3 generatedHeightMap

ノイズやルールで生成された基礎高さ。

### 5.4 manualDeltaMap

人の編集差分。

### 5.5 protectedMask

再生成時に旧 generated を残す重み。

### 5.6 finalHeightMap

`generatedHeightMap + manualDeltaMap` を clamp した最終高さ。

---

## 6. 永続化仕様

### 6.1 永続化対象

以下を保存する。

* `TerrainGenerationProfile`
* `TerrainPersistentData`

---

### 6.2 TerrainPersistentData

永続化対象の本体データ。

#### 保持項目

* `generatedHeightMap`
* `manualDeltaMap`
* `protectedMask`
* `noVegetationMask`
* `flattenMask`

#### 備考

`flattenMask` は Phase 1〜3 では未使用でも永続化データ構造に残す。
理由は、後から平坦化ワークフローを追加しても保存形式を壊しにくくするため。
ただし機能としては**将来拡張**であり、初期フェーズでは未使用扱いとする。

---

### 6.3 保存形式

保存方式は以下に固定する。

#### メタデータ

* ScriptableObject で保持する

#### 実データ

* `.bytes` または独自拡張子の**バイナリファイル**として保存する
* 中身は `float[]` を一次元化した配列

#### 非採用

* Texture2D(R8/RGBA32) 保存
* 8bit 精度保存

---

### 6.4 保存先

保存先は Unity プロジェクト内のアセットとして扱う。

```text
Assets/TerrainToolData/{TerrainName}/
```

#### 例

```text
Assets/TerrainToolData/Terrain_00/
    Terrain_00_Profile.asset
    Terrain_00_PersistentData.asset
    generatedHeightMap.bytes
    manualDeltaMap.bytes
    protectedMask.bytes
    noVegetationMask.bytes
    flattenMask.bytes
```

---

### 6.5 ファイル名規則

* `generatedHeightMap.bytes`
* `manualDeltaMap.bytes`
* `protectedMask.bytes`
* `noVegetationMask.bytes`
* `flattenMask.bytes`

Terrain 名ごとにフォルダを分ける。

---

### 6.6 保存データ構造

各 `.bytes` は以下の順で保存する。

1. width (int)
2. height (int)
3. float 値列

一次元配列化は row-major とする。

```text
index = z * width + x
```

---

## 7. クラス構成

### 7.1 データ系

#### TerrainGenerationProfile

生成パラメータを保持する ScriptableObject。

保持例:

* seed
* tileSizeMeters
* heightmapResolution
* terrainMaxHeightMeters
* baseNoiseScale
* baseAmplitude
* detailNoiseScale
* detailAmplitude
* ridgeNoiseScale
* ridgeStrength
* useRidgedNoise
* useDomainWarp
* domainWarpStrength
* domainWarpScale

---

#### TerrainPersistentData

永続化対象の map 群とファイル参照を保持する ScriptableObject。

---

### 7.2 処理系

#### TerrainGenerator

責務:

* `generatedHeightMap` の生成
* base/detail/ridge 合成
* domain warp 適用

---

#### TerrainComposer

責務:

* `generatedHeightMap + manualDeltaMap`
* clamp
* `finalHeightMap` の生成

---

#### TerrainEditManager

責務:

* manualDelta の編集
* 数値指定範囲編集
* 将来的なブラシ編集

---

#### TerrainMaskManager

責務:

* protectedMask 編集
* noVegetationMask 編集
* flattenMask 編集

---

#### TerrainPersistenceService

責務:

* `.bytes` 読み書き
* TerrainPersistentData との対応管理

---

#### TerrainApplier

責務:

* TerrainData.SetHeights
* TerrainData.SetAlphamaps
* 木、草の適用

---

#### TexturePainter

責務:

* 高度、傾斜から alphamap を生成

---

#### VegetationScatter

責務:

* 木、草のルール配置

---

## 8. マップ座標と単位系

### 8.1 内部単位

`generatedHeightMap` と `manualDeltaMap` は正規化値で保持する。

### 8.2 UI 上のブラシ強度

ユーザーにはメートル単位で見せる。
内部では以下で変換する。

```text
deltaNormalized = deltaMeters / terrainMaxHeightMeters
```

---

## 9. フェーズ別仕様

---

# Phase 1

## 基礎地形生成と永続化

### 9.1 目的

1 タイル分の基礎地形を生成し、Terrain へ反映し、かつ `generatedHeightMap` を永続化できるようにする。

---

### 9.2 機能

* Terrain 指定
* Profile 指定
* seed 指定
* base/detail ノイズ設定
* Generate All
* Save / Load
* 再起動後の復元

---

### 9.3 必須仕様

* `generatedHeightMap` を生成する
* `generatedHeightMap` を保存できる
* 保存済みデータから再読込できる
* 継ぎ目のないサンプリング式を使う
* `heightmapResolution` は選択式

---

### 9.4 Domain Warp

Phase 1 では optional。
使う場合は `IDomainWarp2D` を経由する。
使わない場合は通常の `INoise2D` サンプルのみ。

---

### 9.5 UI

* 対象 Terrain
* Profile
* Generate
* Save
* Load
* Clear Generated
* Seed
* Resolution
* Tile Size
* terrainMaxHeightMeters
* Base Noise
* Detail Noise
* Ridge 設定
* Domain Warp 設定

---

### 9.6 受け入れ条件

* 500m × 500m、513 解像度で生成できる
* Generate All が 2 秒以内
* Unity 再起動後に再読込できる
* 隣接タイル境界が一致する

---

# Phase 2A

## 数値指定による手修正差分編集

### 10.1 目的

SceneView ブラシに進む前に、`manualDeltaMap` を使った差分編集基盤を低コストで構築する。

---

### 10.2 中心点指定方式

Phase 2A では**SceneView クリック取得を行わない**。
中心点は以下のいずれかで入力する。

* Terrain ローカル座標 X,Z を数値入力
* ワールド座標 X,Z を数値入力
* プリセット矩形範囲指定

これにより Phase 2B と境界を明確に分ける。

---

### 10.3 編集機能

* Raise
* Lower
* Smooth
* Flatten

---

### 10.4 範囲指定

* 円形
* 矩形

指定項目:

* centerX
* centerZ
* radius または width/height
* strengthMeters
* falloff

---

### 10.5 Preview 定義

Preview は**編集対象範囲の数値確認と SceneView 上の簡易ガイド表示**とする。
仮反映は行わない。

---

### 10.6 永続化

* `manualDeltaMap` を保存する
* 読み込み後に `finalHeightMap` を再構成できる

---

### 10.7 受け入れ条件

* 数値指定で terrain を上下編集できる
* manualDelta を保存・再読込できる
* finalHeight を再計算できる
* 1 操作あたり 200ms 以内

---

# Phase 2B

## SceneView ブラシ編集

### 11.1 目的

Phase 2A の差分編集基盤を利用し、SceneView 上で直感的に manualDelta を編集できるようにする。

---

### 11.2 機能

* マウス位置から Terrain へ raycast
* ブラシ中心取得
* ブラシ円表示
* ストローク反映
* Raise / Lower / Smooth / Flatten

---

### 11.3 Preview 定義

Preview は以下のみ。

* ブラシ半径の円表示
* 影響減衰範囲の表示

仮反映プレビューは対象外。

---

### 11.4 備考

実装コストが高いため、Phase 2A 完了後に着手する。

---

# Phase 3

## 保護マスクと部分再生成

### 12.1 目的

再生成時に壊してほしくない generated 領域を守る。

---

### 12.2 機能

* protectedMask 編集
* Rebuild All Except Protected
* Rebuild Region
* oldGenerated と newGenerated のマスク合成

---

### 12.3 protectedMask

* 値域 0.0〜1.0
* 0.0 = 保護なし
* 1.0 = 完全保護

---

### 12.4 Overlay 表示方式

Mask Overlay は **セル単位 Gizmos 描画ではなく**、
**低解像度プレビュー用 Texture2D を SceneView に重ねる方式**を標準とする。

#### 理由

513×513 全セルを Handles/Gizmos で描画すると重くなるため。

#### 実装方針

* mask 配列からプレビュー Texture2D を生成
* SceneView 上に半透明オーバーレイ表示
* opacity 調整可能

これにより、前版の「Handles/Gizmos ベース」曖昧さを解消する。

---

### 12.5 部分再生成

対象は次のいずれか。

* Height Only
* Texture Only
* Vegetation Only
* All

範囲指定は Phase 2A と同様に、まずは数値指定を基本とする。
SceneView 指定は将来拡張。

---

### 12.6 受け入れ条件

* protectedMask 保存可能
* 再生成時に oldGenerated を参照できる
* 保護領域が維持される
* オーバーレイ表示が実用的速度で動作する

---

# Phase 4

## テクスチャ自動塗布と植生配置

### 13.1 目的

最終地形に対して Terrain Layer と植生を自動配置する。

---

### 13.2 テクスチャ塗布条件

初期実装では以下のみを利用する。

* 高度
* 傾斜
* ノイズ変調

**曲率は対象外**とする。

---

### 13.3 テクスチャルール

各ルールは以下を持つ。

* 対象 Layer
* minHeight
* maxHeight
* minSlope
* maxSlope
* blendStrength
* noiseVariation

---

### 13.4 傾斜計算

`slopeMap` は heightmap 上で隣接差分から計算する。

---

### 13.5 alphamap 生成

各 alphamap セルに対し、

1. 正規化座標 `(u,v)` を求める
2. heightmap 由来の `height` / `slope` を双線形補間
3. テクスチャルールを評価
4. weight を正規化して alphamap に書き込む

---

### 13.6 植生配置

条件:

* 高度範囲
* 傾斜範囲
* `noVegetationMask == 0`
* 必要なら protected 領域も除外可

---

### 13.7 受け入れ条件

* Texture Only 再構築可能
* Vegetation Only 再構築可能
* 高さ再生成なしで見た目更新可能

---

## 14. flattenMask の扱い

### 14.1 現在の位置づけ

`flattenMask` は永続化データに含めるが、Phase 1〜4 の必須機能ではない。

### 14.2 将来用途

* 拠点用平坦地の予約
* 再生成しても平坦維持
* Flatten 機能の対象領域定義

### 14.3 実装ルール

初期実装では UI に表示しなくてもよい。
ただし保存形式には含める。

---

## 15. パフォーマンス要件

### 15.1 Phase 1

条件:

* 500m × 500m
* heightmapResolution = 513

目標:

* 1 秒以内

必須:

* 2 秒以内

---

### 15.2 Phase 2

* 1 操作 100ms 以内目標
* 200ms 以内必須

---

### 15.3 Phase 4

* 1 タイルの Texture/Vegetation 再構築 2 秒以内目標
* 4 秒以内必須

---

## 16. UI 構成

### 16.1 EditorWindow タブ

* Generate
* Edit
* Mask
* Texture
* Vegetation
* Persistence
* Debug

---

### 16.2 Generate タブ

* Terrain
* Profile
* Seed
* Resolution
* Tile Size
* terrainMaxHeightMeters
* Base/Detail/Ridge/Domain Warp
* Generate
* Save
* Load

---

### 16.3 Edit タブ

Phase 2A:

* centerX
* centerZ
* radius / width / height
* strengthMeters
* falloff
* mode
* Apply
* Preview

Phase 2B:

* ブラシ半径
* 強さ
* falloff
* mode

---

### 16.4 Mask タブ

* Mask 種別
* opacity
* Apply
* Clear
* Rebuild Selected
* Rebuild All Except Protected

---

## 17. 実装優先順位

### Phase 1 前に必須決定

* 保存先パス
* バイナリ保存形式
* Terrain 名とデータフォルダ名の対応
* 既存 Simplex 実装を `INoise2D` に接続する方法

---

### 実装順

1. Phase 1
2. Phase 2A
3. Phase 2B
4. Phase 3
5. Phase 4

---

## 18. 受け入れ条件まとめ

### 必須成立条件

* generatedHeightMap が保存・再読込できる
* oldGenerated を再生成時に参照できる
* finalHeight = generated + manualDelta が維持される
* Unity 再起動後も編集状態が復元できる
* タイル境界に継ぎ目がない

---

## 19. 将来拡張候補

* 侵食処理
* flattenMask の本格運用
* SceneView からの範囲指定
* road / river 用マスク復活
* 曲率ベーステクスチャ
* 複数タイル一括生成
* ノードベース UI

---

## 20. 総括

本仕様 v1.2 は、以下を明確化したことで、実装可能なレベルの設計として成立する。

* generatedHeightMap の永続化必須化
* oldGenerated の取得元固定
* 保存先と保存形式の明示
* Domain Warp の責務分離
* Phase 2A と 2B の境界明確化
* Mask Overlay の現実的実装方式への修正
* 用語統一
* flattenMask の位置づけ明確化

この仕様により、Phase 1 の実装へ安全に着手できる。

---

