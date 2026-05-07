# GameEvent システム レポート

## 概要

シーン・システム間の疎結合な通知を実現する汎用イベントチャンネル。  
Ryan Hipple の ScriptableObject Architecture（Unite Austin 2017）をベースにしたパターン。  
会話システムの EventNode から発火するトリガーとして導入したが、任意のシステム間で使用可能。

## 設計

### クラス構成

| クラス | 責務 |
|--------|------|
| `GameEvent` | イベントチャンネル本体。ScriptableObject アセットとして作成し、送受信の仲介をする |
| `GameEventListener` | MonoBehaviour。GameEvent を購読し、発火時に UnityEvent を呼び出す |

### 依存関係・使い方

**送信側（会話システム等）:**
```csharp
// EventNode の GameEvents フィールドに GameEvent アセットを登録するだけ
// コード側では Raise() を呼ぶだけ。何が起きるかは知らない
gameEvent.Raise();
```

**受信側（カメラ・アニメーション等）:**
```
GameObject に GameEventListener をアタッチ
  └── Event:    OnDialogueEvent.asset  ← 同じ GameEvent SO を参照
  └── Response: CameraShake()          ← Inspector で自由に配線
```

### スキップ時の挙動

会話スキップ中でも `Raise()` は実行される。  
視覚演出の省略はリスナー側（受け取るコンポーネント）の責務とする。

## 実装メモ

- `Raise()` は逆順イテレートで実行する。Raise 中にリスナーが自己解除しても安全。
- `OnEnable` / `OnDisable` で購読管理するため、GameObject が非アクティブな間は通知されない。これは仕様（非表示 UI への誤通知を防ぐ）。
- `GameEvent` はシステムをまたいで使える汎用ライブラリであり、会話システムに依存しない。

## 既知の制限・TODO

- [ ] ペイロード（引数）なしのため、パラメータが必要な場合は受け取り側の SO や設定値で対応する
- [ ] `GameEvent<T>` のジェネリック版（ペイロードあり）は将来拡張として検討

## 変更履歴

| 日付 | 内容 |
|------|------|
| 2026-05-04 | 初版作成。会話システムの EventNode トリガーとして導入 |
