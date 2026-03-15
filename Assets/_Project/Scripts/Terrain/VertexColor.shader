// 頂点カラーをそのまま表示するシンプルな Unlit シェーダ。
// MeshBuilder が生成する頂点カラー付きメッシュに適用する。
Shader "LibraryOfGamecraft/VertexColor"
{
    Properties
    {
        // UV アトラステクスチャ (省略時は白テクスチャで頂点カラーのみ表示)
        _MainTex ("Atlas Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // テクスチャカラー × 頂点カラー
                // アトラスなし (white) の場合は頂点カラーのみが反映される
                fixed4 texColor = tex2D(_MainTex, i.uv);
                return texColor * i.color;
            }
            ENDCG
        }
    }

    // フォールバック: 描画できない環境では Diffuse で代替
    FallBack "Diffuse"
}
