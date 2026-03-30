# SlopeSlideRotation 課題レポート

## ステータス

サスペンド

## 概要

急斜面でスライドが発生すると、キャラクターが斜面の最大傾斜方向（真下）を向いてしまう。

## 詳細

`ApplySlopeSlide()` がスライド中の水平速度を斜面下向きに強制セットする。

```csharp
var slideDir = Vector3.ProjectOnPlane(Vector3.down, _groundInfo.GroundNormal).normalized;
_horizontalVelocity = slideDir * _tuning.MoveSpeed;
```

`RotateTowardsMoveDirection()` がその速度方向にキャラクターを向かせるため、
スライド開始と同時にキャラクターが斜面下方向へ回転する。

## 対応方針（未実装）

以下の選択肢があり、どの挙動が正しいか決まり次第実装する。

| 方針 | 実装方法 |
|------|----------|
| スライド中は向きを変えない | `PlayerController` でスライド中は `RotateTowardsMoveDirection` をスキップ |
| スライド中もプレイヤー入力で向きを制御する | スライド中の回転ロジックを別途追加 |
| 現状のまま（斜面下向き）とする | 変更不要 |

## 解決

（未解決）

## 変更履歴

| 日付       | 内容       |
|------------|------------|
| 2026-03-31 | 課題起票。対応方針を整理してサスペンド |
