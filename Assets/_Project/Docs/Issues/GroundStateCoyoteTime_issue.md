# GroundStateCoyoteTime 課題レポート

## ステータス

未解決

## 概要

斜面や段差で少し浮いただけで Fall ステートに遷移してしまい、挙動が不安定になる。

## 詳細

`IdleState` / `MoveState` の Tick で `IsGrounded == false` になった瞬間に `ForceTransition(Fall)` が発火する。

```csharp
if (!context.GroundInfo.IsGrounded)
{
    context.StateMachine.ForceTransition(CharacterStateType.Fall);
    return;
}
```

斜面・段差・地面の微細な凹凸でわずかに浮いただけでも即 Fall に遷移するため、
移動中に不自然な状態変化が頻発する。

`GroundedThreshold` / `GroundCheckDistance` のパラメータ調整では根本解決できない。

## 対応方針（未実装）

**コヨーテタイム**（接地を失ってから一定時間は接地扱いにする猶予）を実装する。

```csharp
// MoveState / IdleState のイメージ
if (!context.GroundInfo.IsGrounded)
{
    _coyoteTimer += deltaTime;
    if (_coyoteTimer >= coyoteTime)  // 0.1f 程度が目安
        context.StateMachine.ForceTransition(CharacterStateType.Fall);
    return;
}
_coyoteTimer = 0f;
```

- `coyoteTime` は ScriptableObject（`JumpSettings` など）に外出しする
- MoveState / IdleState の両方に適用が必要

## 解決

（未解決）

## 変更履歴

| 日付       | 内容       |
|------------|------------|
| 2026-03-31 | 課題起票。コヨーテタイムによる解決方針を整理 |
