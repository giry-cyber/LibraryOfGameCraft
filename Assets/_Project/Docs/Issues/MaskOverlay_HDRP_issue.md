# MaskOverlay HDRP 非対応 課題レポート

## ステータス

未解決

## 概要

Mask タブのマスクオーバーレイ（塗った領域の色付け）が HDRP の SceneView で表示されない。

## 詳細

### 試みた実装

1. **DrawMeshNow + Unlit/Transparent**
   - Legacy シェーダーのため HDRP SceneView でレンダリングされない

2. **GL + Hidden/Internal-Colored**
   - HDRP SceneView で動作するはずだが、実際には表示されなかった
   - `_ZTest = Always` / `_ZWrite = 0` を明示設定しても変化なし

### 影響範囲

- `MaskOverlayRenderer.cs` — 実装済みだが機能しない
- `TerrainToolWindow` の Mask タブ — オーバーレイなしで UI は動作する
- マスクの編集自体（`MaskEditor` / `.bytes` 保存）は正常動作

### 暫定対処

- Mask タブ / Vegetation タブを "Coming Soon（凍結中）" に封印
- `DrawMaskTab` / `OnMaskSceneGUI` は `#pragma warning disable IDE0051` で保管
- `OnMaskSceneGUI` の `duringSceneGui` 登録を停止

## 解決

（解決前は空欄）

## 変更履歴

| 日付 | 内容 |
|------|------|
| 2026-04-03 | 課題起票 |
