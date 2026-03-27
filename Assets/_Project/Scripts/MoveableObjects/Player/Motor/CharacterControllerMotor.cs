using UnityEngine;

namespace LibraryOfGamecraft.Player
{
    /// <summary>
    /// Unity標準 CharacterController を移動実行基盤として使用するモーター実装。
    /// Ground Detection・Slope・GroundSnap・Gravity・WallSliding は自前実装。
    /// </summary>
    public class CharacterControllerMotor : ICharacterMotor
    {
        private readonly CharacterController _cc;
        private readonly MovementTuning _tuning;
        private readonly GravitySettings _gravity;

        private GroundInfo _groundInfo;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;

        public GroundInfo GroundInfo => _groundInfo;
        public Vector3 HorizontalVelocity { get => _horizontalVelocity; set => _horizontalVelocity = value; }
        public float VerticalVelocity { get => _verticalVelocity; set => _verticalVelocity = value; }

        public CharacterControllerMotor(CharacterController cc, MovementTuning tuning, GravitySettings gravity)
        {
            _cc = cc;
            _tuning = tuning;
            _gravity = gravity;
        }

        public void Tick(float deltaTime)
        {
            UpdateGroundInfo();
            ApplyGravity(deltaTime);
            ApplySlopeSlide();
            ApplyGroundSnap();

            var motion = (_horizontalVelocity + Vector3.up * _verticalVelocity) * deltaTime;
            _cc.Move(motion);
        }

        private void UpdateGroundInfo()
        {
            // カプセル底面の球体中心を起点にSphereCastを下向きに行う
            var capsuleBottomCenter = _cc.transform.position
                + _cc.center
                - Vector3.up * (_cc.height / 2f - _cc.radius);

            float checkDist = _tuning.GroundCheckDistance;

            if (Physics.SphereCast(
                    capsuleBottomCenter,
                    _cc.radius * 0.9f, // 壁への誤検出を避けるため少し小さく
                    Vector3.down,
                    out var hitInfo,
                    checkDist,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Ignore))
            {
                float dot = Vector3.Dot(hitInfo.normal, Vector3.up);
                bool validGround = dot >= _tuning.MinGroundDotProduct
                    && hitInfo.distance <= _tuning.GroundedThreshold;

                _groundInfo = new GroundInfo
                {
                    IsGrounded = validGround,
                    GroundNormal = hitInfo.normal,
                    GroundPoint = hitInfo.point,
                    GroundDistance = hitInfo.distance,
                    SlopeAngle = Vector3.Angle(hitInfo.normal, Vector3.up),
                    GroundCollider = hitInfo.collider,
                };
            }
            else
            {
                _groundInfo = GroundInfo.Airborne;
            }
        }

        private void ApplyGravity(float deltaTime)
        {
            if (_groundInfo.IsGrounded && _verticalVelocity <= 0f)
            {
                // 地面に押し付けておく小さな値（GroundSnapを確実に機能させるため）
                _verticalVelocity = -2f;
                return;
            }

            float gravityStrength = Physics.gravity.y * _gravity.GravityScale;
            if (_verticalVelocity < 0f)
                gravityStrength *= _gravity.FallMultiplier;

            _verticalVelocity += gravityStrength * deltaTime;
            _verticalVelocity = Mathf.Max(_verticalVelocity, -_gravity.MaxFallSpeed);
        }

        private void ApplySlopeSlide()
        {
            if (!_groundInfo.IsGrounded) return;

            // SlideAngle超の急斜面では横方向に強制スライド
            if (_groundInfo.SlopeAngle > _tuning.SlideAngle)
            {
                var slideDir = Vector3.ProjectOnPlane(Vector3.down, _groundInfo.GroundNormal).normalized;
                _horizontalVelocity = slideDir * _tuning.MoveSpeed;
            }
        }

        private void ApplyGroundSnap()
        {
            // 上昇中・接地中はスナップ不要
            if (_groundInfo.IsGrounded || _verticalVelocity > 0f) return;

            // 下り坂でキャラクターが浮くのを防ぐためにレイキャストで地面を検出
            if (Physics.Raycast(
                    _cc.transform.position + _cc.center,
                    Vector3.down,
                    out var hit,
                    _cc.height / 2f + _tuning.GroundSnapDistance,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Ignore))
            {
                float snapDot = Vector3.Dot(hit.normal, Vector3.up);
                if (snapDot >= _tuning.MinGroundDotProduct)
                    _verticalVelocity = Mathf.Min(_verticalVelocity, -1f);
            }
        }

        /// <summary>OnControllerColliderHit から呼ばれ、壁への衝突時に速度を壁面に投影する。</summary>
        public void HandleControllerCollision(ControllerColliderHit hit)
        {
            if (!_tuning.EnableWallSliding) return;

            var normal = hit.normal;
            // 地面法線は無視（斜面処理は別途行うため）
            if (Vector3.Dot(normal, Vector3.up) > 0.5f) return;

            _horizontalVelocity = Vector3.ProjectOnPlane(_horizontalVelocity, normal);
        }
    }
}
