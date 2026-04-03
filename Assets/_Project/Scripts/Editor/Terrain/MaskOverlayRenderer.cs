using UnityEditor;
using UnityEngine;

namespace LibraryOfGamecraft.Terrain.Editor
{
    /// <summary>
    /// マスクデータを Terrain の XZ 平面に半透明カラーとして GL で投影する。
    /// HDRP SceneView でも動作する "Hidden/Internal-Colored" シェーダーを使用。
    /// SceneView の duringSceneGui コールバック内で Draw を呼び出す。
    /// </summary>
    internal class MaskOverlayRenderer : System.IDisposable
    {
        private Material  _material;
        private float[]   _mask;
        private int       _resolution;
        private Color     _color;

        // ----------------------------------------------------------------

        public void SetMask(float[] mask, int resolution, Color color)
        {
            _mask       = mask;
            _resolution = resolution;
            _color      = color;
        }

        public void Clear()
        {
            _mask = null;
        }

        /// <summary>
        /// Terrain の XZ 平面にマスクオーバーレイを描画する。
        /// EventType.Repaint 時のみ実際に描画される。
        /// </summary>
        public void Draw(UnityEngine.Terrain terrain, float tileSize, Vector2 tileOrigin)
        {
            if (_mask == null) return;
            if (Event.current.type != EventType.Repaint) return;

            EnsureMaterial();
            if (_material == null) return;

            // Terrain の Y に少し上乗せしてズファイティングを防ぐ
            float y = terrain.transform.position.y + 0.15f;

            float res1 = _resolution - 1;

            _material.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.QUADS);

            for (int zi = 0; zi < _resolution - 1; zi++)
            {
                for (int xi = 0; xi < _resolution - 1; xi++)
                {
                    // 4頂点の mask 値を取得
                    float m00 = _mask[ zi      * _resolution + xi    ];
                    float m10 = _mask[ zi      * _resolution + xi + 1];
                    float m01 = _mask[(zi + 1) * _resolution + xi    ];
                    float m11 = _mask[(zi + 1) * _resolution + xi + 1];

                    // セルの最大値が閾値未満なら描画スキップ（完全透明セルを間引く）
                    if (Mathf.Max(m00, m10, m01, m11) < 0.01f) continue;

                    float x0 = tileOrigin.x + (xi     / res1) * tileSize;
                    float x1 = tileOrigin.x + ((xi+1) / res1) * tileSize;
                    float z0 = tileOrigin.y + (zi     / res1) * tileSize;
                    float z1 = tileOrigin.y + ((zi+1) / res1) * tileSize;

                    // 最大不透明度 0.65 でブレンド
                    GL.Color(new Color(_color.r, _color.g, _color.b, m00 * 0.65f));
                    GL.Vertex3(x0, y, z0);

                    GL.Color(new Color(_color.r, _color.g, _color.b, m01 * 0.65f));
                    GL.Vertex3(x0, y, z1);

                    GL.Color(new Color(_color.r, _color.g, _color.b, m11 * 0.65f));
                    GL.Vertex3(x1, y, z1);

                    GL.Color(new Color(_color.r, _color.g, _color.b, m10 * 0.65f));
                    GL.Vertex3(x1, y, z0);
                }
            }

            GL.End();
            GL.PopMatrix();
        }

        // ----------------------------------------------------------------

        private void EnsureMaterial()
        {
            if (_material != null) return;

            // "Hidden/Internal-Colored" はエディタ組み込みで HDRP SceneView でも動作する
            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
            {
                Debug.LogWarning("[MaskOverlayRenderer] 'Hidden/Internal-Colored' が見つかりませんでした。");
                return;
            }

            _material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _material.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _material.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _material.SetInt("_Cull",      (int)UnityEngine.Rendering.CullMode.Off);
            _material.SetInt("_ZWrite",    0);
            _material.SetInt("_ZTest",     (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        public void Dispose()
        {
            if (_material != null) Object.DestroyImmediate(_material);
            _material = null;
        }
    }
}
