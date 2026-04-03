# Phase4 未実装・凍結 課題レポート

## ステータス

未解決

## 概要

Phase 4A（テクスチャ + 草）および Phase 4B（木配置）が未実装のため凍結中。
Vegetation タブは "Coming Soon（凍結中）" に封印している。

## 詳細

### Phase 4A — テクスチャ + 草（仕様書 Section 14）

| 項目 | 内容 |
|------|------|
| TextureRule | minHeight / maxHeight / minSlope / maxSlope / blendStrength |
| alphamap 生成 | 各ルールの weight を正規化して `TerrainData.SetAlphamaps()` |
| 傾斜計算 | 中央差分 `atan(sqrt(dx² + dz²))` [degree] |
| Grass | detail layer density map / noVegetationMask 参照 |

### Phase 4B — 木配置（仕様書 Section 15・23）

| 項目 | 内容 |
|------|------|
| TreePrototypeRule | `TerrainGenerationProfile` に定義済み |
| 配置方法 | グリッド cellSize = sqrt(100 / densityPer100m2) |
| 条件 | height / slope / noVegetationMask |
| 将来拡張 | Poisson Disk |

### 前提依存

- `noVegetationMask` の正常動作確認（Phase 3 完了が前提）
- Phase 4A の TextureRule 設計（`TerrainGenerationProfile` への追加が必要）

## 解決

（解決前は空欄）

## 変更履歴

| 日付 | 内容 |
|------|------|
| 2026-04-03 | 課題起票・凍結 |
