using Unity.Cinemachine;
using LibraryOfGamecraft.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LibraryOfGamecraft.CameraSystem
{
    /// <summary>
    /// TPS カメラコントローラー（Cinemachine 3.x 対応）。
    ///
    /// 役割：
    ///   1. CinemachineOrbitalFollow の水平・垂直軸を InputReader のルック入力で回転させる
    ///   2. カーソルのロック／解放を管理する
    ///
    /// Cinemachine 3.x での変更点（2.x との違い）：
    ///   ・名前空間 : Cinemachine → Unity.Cinemachine
    ///   ・CinemachineFreeLook が廃止 → CinemachineCamera + CinemachineOrbitalFollow に分離
    ///   ・軸の駆動 : m_XAxis.Value → CinemachineOrbitalFollow.HorizontalAxis.Value
    ///   ・入力競合 : InputAxisController コンポーネントを追加しなければ自動入力は無効
    ///
    /// Inspector セットアップ手順：
    ///   1. 空の GameObject "CameraController" にこのスクリプトをアタッチ
    ///   2. シーンに CinemachineCamera を作成し CinemachineOrbitalFollow を追加する
    ///   3. [Virtual Camera] フィールドにその CinemachineCamera を設定
    ///   4. CinemachineCamera の Follow / Look At に Player の Transform を設定
    ///   5. Main Camera をシーンに残したまま（CinemachineBrain が自動アタッチされる）
    ///   ※ InputAxisController は追加しない（スクリプト側で軸を直接駆動するため）
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraController : MonoBehaviour
    {
        [Header("Cinemachine")]
        [Tooltip("CinemachineOrbitalFollow を持つ CinemachineCamera をアサイン")]
        [SerializeField] private CinemachineCamera _virtualCamera;

        [Header("感度")]
        [Tooltip("水平（左右）の回転感度。Player/Look アクションの X 出力に掛ける係数。")]
        [Range(0.05f, 1.0f)]
        [SerializeField] private float _horizontalSensitivity = 0.3f;

        [Tooltip("垂直（上下）の回転感度。Player/Look アクションの Y 出力に掛ける係数。")]
        [Range(0.001f, 0.02f)]
        [SerializeField] private float _verticalSensitivity = 0.005f;

        [Header("ズーム")]
        [Tooltip("スクロール 1 ノッチあたりの距離変化量")]
        [Range(0.1f, 5.0f)]
        [SerializeField] private float _zoomSpeed = 1.0f;

        [Tooltip("カメラの最小距離（一番近い位置）")]
        [SerializeField] private float _zoomMin = 2.0f;

        [Tooltip("カメラの最大距離（一番遠い位置）")]
        [SerializeField] private float _zoomMax = 15.0f;

        [Header("カーソル")]
        [Tooltip("Play 開始時にカーソルをロックするか")]
        [SerializeField] private bool _lockCursorOnStart = true;

        private InputReader              _inputReader;
        private CinemachineOrbitalFollow _orbitalFollow;

        // ── ライフサイクル ─────────────────────────────────────────────────

        private void Awake()
        {
            _inputReader = FindFirstObjectByType<InputReader>();

            if (_virtualCamera != null)
                _orbitalFollow = _virtualCamera.GetComponent<CinemachineOrbitalFollow>();

            if (_inputReader == null)
                Debug.LogError("[CameraController] InputReader が見つかりません。Player GameObject が Hierarchy にあるか確認してください。", this);
            if (_virtualCamera == null)
                Debug.LogError("[CameraController] Virtual Camera が設定されていません。Inspector で CinemachineCamera を設定してください。", this);
            if (_orbitalFollow == null)
                Debug.LogError("[CameraController] CinemachineOrbitalFollow が Virtual Camera に見つかりません。Add Component から追加してください。", this);
        }

        private void Start()
        {
            if (_lockCursorOnStart)
                SetCursorLocked(true);
        }

        private void Update()
        {
            HandleCursorToggle();
            DriveCameraAxes();
            DriveZoom();
        }

        // ── プライベート ──────────────────────────────────────────────────

        /// <summary>
        /// InputReader のルック入力を CinemachineOrbitalFollow の軸に直接書き込む。
        ///
        ///   HorizontalAxis : 水平旋回。Wrap = true なので Clamp 不要。
        ///   VerticalAxis   : 垂直旋回。Range.x〜Range.y にクランプする。
        ///                    Y の正方向は「カメラが上がる = 値が下がる」なので反転する。
        ///
        /// 感度の調整：
        ///   Player/Look アクションが返す値は新 Input System のピクセルデルタです。
        ///   _horizontalSensitivity / _verticalSensitivity で実用的な回転速度に変換します。
        ///   Look アクションに Scale プロセッサを追加してマウス DPI の差を吸収することもできます。
        /// </summary>
        private void DriveCameraAxes()
        {
            if (_orbitalFollow == null || _inputReader == null) return;
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 look = _inputReader.LookInput;

            // 水平軸：Wrap = true なのでそのまま加算（Cinemachine が 0〜360 でラップする）
            _orbitalFollow.HorizontalAxis.Value += look.x * _horizontalSensitivity;

            // 垂直軸：Range 内にクランプ。Y 正方向（マウス上）でカメラが上昇するため反転
            _orbitalFollow.VerticalAxis.Value = Mathf.Clamp(
                _orbitalFollow.VerticalAxis.Value - look.y * _verticalSensitivity,
                _orbitalFollow.VerticalAxis.Range.x,
                _orbitalFollow.VerticalAxis.Range.y
            );
        }

        /// <summary>
        /// マウスホイールで OrbitalFollow の Radius を変化させる。
        /// スクロール上 → 距離を縮める / スクロール下 → 距離を広げる。
        /// </summary>
        private void DriveZoom()
        {
            if (_orbitalFollow == null) return;

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f)) return;

            _orbitalFollow.Radius = Mathf.Clamp(
                _orbitalFollow.Radius - scroll * _zoomSpeed,
                _zoomMin,
                _zoomMax
            );
        }

        /// <summary>Escape でカーソル解放、左クリックで再ロック。</summary>
        private void HandleCursorToggle()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                SetCursorLocked(false);
            else if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.None)
                SetCursorLocked(true);
        }

        private void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible   = !locked;
        }
    }
}
