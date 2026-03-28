using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// 将来的な完全自前モーター移行に備えた抽象化インターフェース。
    /// </summary>
    public interface ICharacterMotor
    {
        GroundInfo GroundInfo { get; }
        Vector3 HorizontalVelocity { get; set; }
        float VerticalVelocity { get; set; }
        void RefreshGroundInfo();
        void Tick(float deltaTime);
    }
}
