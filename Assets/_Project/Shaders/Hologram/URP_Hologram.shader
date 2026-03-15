// URP_Hologram.shader
// URP 17 対応 ホログラムシェーダー (参考実装)
//
// ※ このシェーダーは URP 用ですが、HDRP プロジェクトでもコンパイルエラーが出ないよう
//    URP パッケージ不要の CGPROGRAM / UnityCG.cginc で実装してあります。
//    URP プロジェクトへ移植する際は HLSLPROGRAM + Core.hlsl に切り替えてください。
//    (HLSL 版の実装例は HDRP_Hologram.shader を参照)
//
// ホログラム3要素:
//   方法A: Fresnel 発光 (エッジ強調)
//   方法B: スキャンライン (UV.y に sin 適用)
//   方法C: グリッチ (ノイズによる頂点変位 + 色ずれ)

Shader "Custom/URP/Hologram"
{
    Properties
    {
        [Header(Base)]
        _HologramColor      ("Hologram Color (HDR)", Color) = (0, 0.8, 1, 1)
        _Alpha              ("Base Alpha",           Range(0, 1)) = 0.75

        [Header(Fresnel)]
        _FresnelPower       ("Fresnel Power",        Range(0.1, 10)) = 3.0
        _FresnelIntensity   ("Fresnel Intensity",    Range(0, 5))    = 2.0

        [Header(Scanline)]
        _ScanlineFrequency  ("Scanline Frequency",   Range(0, 300))  = 80.0
        _ScanlineSpeed      ("Scanline Speed",       Range(0, 20))   = 3.0
        _ScanlineIntensity  ("Scanline Intensity",   Range(0, 1))    = 0.5

        [Header(Glitch)]
        _GlitchIntensity    ("Glitch Intensity",     Range(0, 0.5))  = 0.05
        _GlitchSpeed        ("Glitch Speed",         Range(0, 30))   = 8.0
    }

    SubShader
    {
        // RenderPipeline タグを URP に設定 (HDRP では使われないが参考として残す)
        // URP インストール済みの場合は "UniversalPipeline" タグを追加すると良い
        Tags
        {
            "RenderType" = "Transparent"
            "Queue"      = "Transparent"
        }

        // ─────────────────────────────────────────────
        //  Forward Pass (URP UniversalForward 相当)
        // ─────────────────────────────────────────────
        Pass
        {
            // URP インストール時は以下タグを追加:
            // Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            CGPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   3.0

            #include "UnityCG.cginc"

            // ─── シェーダープロパティ ───
            float4 _HologramColor;
            float  _Alpha;
            float  _FresnelPower;
            float  _FresnelIntensity;
            float  _ScanlineFrequency;
            float  _ScanlineSpeed;
            float  _ScanlineIntensity;
            float  _GlitchIntensity;
            float  _GlitchSpeed;

            // ─────────────────────────────────────────
            //  HologramCore インライン (UnityCG 用)
            //  ※ HLSL 版は HologramCore.hlsl を参照
            // ─────────────────────────────────────────

            float Hologram_Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float Hologram_Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hologram_Hash(i);
                float b = Hologram_Hash(i + float2(1.0, 0.0));
                float c = Hologram_Hash(i + float2(0.0, 1.0));
                float d = Hologram_Hash(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // 方法A: Fresnel
            float Hologram_Fresnel(float3 viewDir, float3 normalWS, float power)
            {
                return pow(1.0 - saturate(dot(viewDir, normalWS)), power);
            }

            // 方法B: スキャンライン
            float Hologram_Scanline(float uvY, float freq, float speed, float time)
            {
                return sin(uvY * freq + time * speed) * 0.5 + 0.5;
            }

            // 方法C: グリッチ頂点変位
            float3 Hologram_GlitchOffset(float uvY, float intensity, float speed, float time)
            {
                float noise = Hologram_Noise(float2(uvY * 10.0, time * speed));
                float gate  = step(0.95, Hologram_Noise(float2(time * speed * 0.31, 7.3)));
                return float3(noise * intensity * gate, 0.0, 0.0);
            }

            // 方法C: グリッチ色ずれ
            float3 Hologram_GlitchColor(float uvY, float intensity, float speed, float time)
            {
                float noise = Hologram_Noise(float2(uvY * 5.0, time * speed));
                float gate  = step(0.97, Hologram_Noise(float2(time * speed * 0.17, 13.1)));
                return float3(noise * gate * intensity * 2.0, 0.0, 0.0);
            }

            // ─── 頂点入力 ───
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // ─── フラグメント入力 ───
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ─── 頂点シェーダー ───
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float time = _Time.y;

                // 方法C: グリッチ頂点変位 (オブジェクト空間 X 方向)
                float3 glitch  = Hologram_GlitchOffset(input.uv.y, _GlitchIntensity, _GlitchSpeed, time);
                float4 posOS   = float4(input.positionOS.xyz + glitch, 1.0);

                output.positionCS = UnityObjectToClipPos(posOS);
                output.positionWS = mul(unity_ObjectToWorld, posOS).xyz;
                output.normalWS   = UnityObjectToWorldNormal(input.normalOS);
                output.uv         = input.uv;

                return output;
            }

            // ─── フラグメントシェーダー ───
            fixed4 Frag(Varyings input) : SV_Target
            {
                float time      = _Time.y;
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir  = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);

                // 方法A: Fresnel
                float  fresnel   = Hologram_Fresnel(viewDir, normalWS, _FresnelPower);
                float  fresnelS  = fresnel * _FresnelIntensity;

                // 方法B: スキャンライン
                float  scanline  = Hologram_Scanline(input.uv.y, _ScanlineFrequency, _ScanlineSpeed, time);
                float  scanFact  = lerp(1.0, scanline, _ScanlineIntensity);

                // 方法C: グリッチ色ずれ
                float3 glitch    = Hologram_GlitchColor(input.uv.y, _GlitchIntensity, _GlitchSpeed, time);

                // 合成
                float3 color = _HologramColor.rgb * (1.0 + fresnelS) * scanFact + glitch;
                float  alpha = _Alpha * (0.4 + fresnel * 0.6) * scanFact;
                alpha = saturate(alpha);

                return fixed4(color, alpha);
            }

            ENDCG
        }

        // ─────────────────────────────────────────────
        //  DepthOnly Pass
        // ─────────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            CGPROGRAM
            #pragma vertex   DepthVert
            #pragma fragment DepthFrag
            #pragma target   3.0

            #include "UnityCG.cginc"

            struct DepthAttributes { float4 positionOS : POSITION; };
            struct DepthVaryings   { float4 positionCS : SV_POSITION; };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                return output;
            }

            fixed4 DepthFrag(DepthVaryings input) : SV_Target { return 0; }

            ENDCG
        }
    }

    FallBack "Diffuse"
}
