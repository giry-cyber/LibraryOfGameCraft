# AnimationTransitionLoop 課題レポート

## ステータス

解決済み

## 概要

`PlayerAnimatorAdapter` が毎フレーム `ActionState` を `SetInteger` していたため、
Animator の Any State トランジションが毎フレーム発火し、アニメーションが常に先頭フレームにリセットされ続けていた。

## 詳細

- **再現手順**: Play モードでキャラクターを動かす。Idle・Move など全アニメーションが最初のフレームしか再生されない。
- **影響範囲**: 全ステートのアニメーション再生
- **原因**: `Update()` 内で毎フレーム `_animator.SetInteger(ActionStateParam, ...)` を呼ぶと、Any State → 各ステートのトランジション条件が毎フレーム成立し、遷移が繰り返される。`Can Transition To Self = false` でも、ブレンド中は「そのステートにいる」と見なされないため防げない。

## 解決

`ActionState`（Int）を廃止し、ステートごとの **Trigger**（`ToIdle` 〜 `ToStun`）に切り替えた。

- Int 条件は「条件が true のまま残る」ため、Any State が毎フレーム遷移を発火し続ける根本原因だった。
- Trigger は消費後に自動リセットされるため、遷移が一度だけ発火することを保証できる。
- `_lastStateType` によるガードも維持し、ステート変化時のみ Trigger を発火する。

**Animator Controller 側の対応も必要：**

- `ActionState`（Int）パラメータを削除
- `ToIdle` / `ToMove` / `ToJump` / `ToFall` / `ToLanding` / `ToAttack` / `ToDodge` / `ToStun` の 8 つの Trigger パラメータを追加
- Any State → 各ステートのトランジション条件を対応する Trigger に変更

変更ファイル: `Assets/_Project/Scripts/MoveableObjects/Player/Animator/PlayerAnimatorAdapter.cs`

## 変更履歴

| 日付       | 内容                                           |
|------------|------------------------------------------------|
| 2026-03-30 | 課題起票。Int による毎フレーム遷移バグを確認   |
| 2026-03-30 | ActionState を Trigger に切り替えて解決        |
