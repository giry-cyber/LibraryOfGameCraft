# FlagSystem マニュアル

## セットアップ

### 1. FlagContainer アセットを作成する

Project ウィンドウで右クリック →
`Create > LibraryOfGamecraft > Flag > FlagContainer`

作成した `.asset` ファイルを `Assets/_Project/Data/` 等に配置する。

### 2. MonoBehaviour にアサインする

```csharp
[SerializeField] private FlagContainerSO _flags;
```

インスペクタで作成したアセットをアサインする。

---

## フラグを追加する

`Assets/_Project/Scripts/Flag/GameFlag.cs` を開き、enum に1行追加する。

```csharp
public enum GameFlag
{
    Example,
    BossDefeated,  // ← 追加
    TutorialDone,  // ← 追加
}
```

Unity がコンパイルを完了すると、`FlagContainerSO` のインスペクタに新しいエントリが自動で現れる。
`DefaultValue` を設定する場合はここで行う（デフォルトは `false`）。

## フラグを削除する

`GameFlag.cs` から該当の行を削除する。

⚠ `FlagContainerSO` のリストからも自動で除去される。
ただし、削除したフラグを参照しているコードはコンパイルエラーになるため、合わせて修正する。

---

## 基本的な使い方

```csharp
// フラグを立てる
_flags.Set(GameFlag.BossDefeated, true);

// フラグを読む
if (_flags.Get(GameFlag.BossDefeated))
{
    OpenNextArea();
}

// フラグを初期値に戻す
_flags.Reset(GameFlag.BossDefeated);

// 全フラグを初期値に戻す
_flags.ResetAll();
```

## 変化イベントを購読する

フラグの値が変わった瞬間に処理を実行したい場合は `OnFlagChanged` を購読する。

```csharp
private void OnEnable()
{
    _flags.OnFlagChanged += HandleFlagChanged;
}

private void OnDisable()
{
    _flags.OnFlagChanged -= HandleFlagChanged;
}

private void HandleFlagChanged(GameFlag flag, bool value)
{
    if (flag == GameFlag.BossDefeated && value)
        ShowVictoryUI();
}
```

---

## PlayMode のリセット挙動

| タイミング | 挙動 |
|---|---|
| PlayMode 開始時 | 全フラグが `DefaultValue` にリセットされる |
| PlayMode 中 | `Set()` で変更した値はその PlayMode セッション内で保持される |
| PlayMode 終了後 | SO アセットに値が残るが、次回 PlayMode 開始時に上書きされる |

PlayMode 終了後にインスペクタで `RuntimeValue` が残って見えることがあるが、
次の PlayMode 開始時には必ずリセットされるため動作に影響はない。

---

## ⚠ 運用上の注意点

### GameFlag enum はライブラリフォルダにある

`Scripts/Flag/GameFlag.cs` はライブラリフォルダ（`Scripts/Flag/`）に置かれているが、
**内容はゲーム固有**（フラグ名はゲームごとに異なる）。

このプロジェクト内では利便性のためこの配置を採用している。
**別プロジェクトへ移植する際の注意点：**

1. `Scripts/Flag/GameFlag.cs` だけゲーム側のアセンブリに移動する
2. `FlagContainerSO.cs` と `FlagEntry.cs` はライブラリアセンブリのまま
3. ライブラリアセンブリからゲームアセンブリへの依存を逆転させるため、
   `GameFlag` の代わりに `System.Enum` 制約付きジェネリクスへの書き換えが必要になる

移植が確定したタイミングで対応すること（今は割り切って同一アセンブリで管理する）。

### フラグ名は変更しない

稼働中のゲームで `GameFlag` の enum 値を**リネーム・並び替え**すると、
既存の `FlagContainerSO` アセットで値がズレる可能性がある（int値ベースのシリアライズ）。

変更が必要な場合は古い値を残したまま新しい値を追加し、移行期間後に削除する。

---

## ファイル構成

```
Assets/_Project/
├── Scripts/Flag/
│   ├── GameFlag.cs          # フラグ定義（ここを編集してフラグを追加）
│   ├── FlagEntry.cs         # シリアライズ用データクラス
│   └── FlagContainerSO.cs   # フラグ管理 ScriptableObject
└── Docs/
    ├── Reports/FlagSystem_report.md
    └── Manuals/FlagSystem_マニュアル.md（このファイル）
```
