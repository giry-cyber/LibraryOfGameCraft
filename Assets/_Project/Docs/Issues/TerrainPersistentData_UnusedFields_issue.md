# TerrainPersistentData の width / height フィールド未使用問題

## ステータス

未解決

## 概要

`TerrainPersistentData` に `width` と `height` フィールドが存在するが、どこからも参照されていない。

## 詳細

`Assets/_Project/Scripts/Terrain/Data/TerrainPersistentData.cs` の `width` / `height` フィールドは、保存した `.bytes` ファイルのサイズを記録する目的で設計されたと思われるが、現在の実装では使用されていない。

- heightmap の解像度は `TerrainGenerationProfile.heightmapResolution` が保持している
- `HeightMapIO.Load` / `Save` はサイズ検証を行っていない
- Inspector に表示されユーザーを混乱させる

## 解決

（未記入）

対応候補:
- `width` / `height` を削除し、解像度は Profile に一元管理する
- または Load 時のサイズ検証に活用するよう実装する

## 変更履歴

| 日付 | 内容 |
|---|---|
| 2026-04-02 | 課題起票 |
