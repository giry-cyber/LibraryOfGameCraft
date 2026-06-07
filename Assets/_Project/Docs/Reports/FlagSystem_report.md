# FlagSystem レポート

## 概要

ゲームの進行状態・イベント発生状況を `bool` フラグで管理するシステム。
ScriptableObject に全フラグを集約し、インスペクタから状態確認・初期値設定ができる。
フラグ定義は `GameFlag` enum に一元化されており、タイポによるバグを防ぐ。

## 設計

### クラス構成

| クラス | 責務 |
|--------|------|
| `GameFlag` | フラグの種類を列挙する enum。フラグを追加するときはここを編集する |
| `FlagEntry` | 1フラグ分のデータ（フラグ種別・デフォルト値・ランタイム値） |
| `FlagContainerSO` | 全フラグの値を保持・操作する ScriptableObject |

### 依存関係・使い方

```csharp
// MonoBehaviour 側
[SerializeField] private FlagContainerSO _flags;

// フラグを立てる
_flags.Set(GameFlag.BossDefeated, true);

// フラグを読む
if (_flags.Get(GameFlag.BossDefeated)) { ... }

// 変化を購読する
_flags.OnFlagChanged += (flag, value) => Debug.Log($"{flag} -> {value}");
```

### フラグの追加手順

1. `Scripts/Flag/GameFlag.cs` を開く
2. enum に1行追加する（例: `BossDefeated,`）
3. Unity がコンパイルすると `FlagContainerSO` のリストに自動でエントリが追加される
4. インスペクタで `DefaultValue` を設定する（必要な場合）

## 実装メモ

### enumとリストの自動同期

`OnEnable`（ランタイム）と `OnValidate`（エディタ）の両方で `SyncWithEnum()` を呼ぶことで、
enumへの追加・削除がそのままリストに反映される。

### PlayMode リセット

`OnEnable` は PlayMode 開始時のドメインリロードで呼ばれる。
この際 `ResetToDefault()` が走るため、ランタイム値は常に `DefaultValue` から開始される。
PlayMode 終了後もエディタ上で値を変更した場合、次の PlayMode 開始時に上書きされる。

## 既知の制限・TODO

- [ ] フラグのセーブ/ロード（PlayerPrefs・JSON連携）は未対応
- [ ] フラグ間の条件組み合わせ（AND/OR）は未対応
- [ ] int / float / string 型のフラグは未対応（bool のみ）
- [ ] `GameFlag` enum がライブラリフォルダに存在するため、別プロジェクト移植時は分離が必要

## 変更履歴

| 日付 | 内容 |
|------|------|
| 2026-06-02 | 初版作成 |
