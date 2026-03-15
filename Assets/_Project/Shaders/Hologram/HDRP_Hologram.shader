// HDRP_Hologram.shader
// Unity 6 / HDRP 17 対応 ホログラムシェーダー
// Fresnel発光 + スキャンライン + グリッチ を組み合わせた半透明 Unlit シェーダー

Shader "Custom/HDRP/Hologram"
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

        [HideInInspector] _ZWrite ("ZWrite", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
        }

        // ─────────────────────────────────────────────
        //  Forward Pass (HDRP Unlit Transparent)
        // ─────────────────────────────────────────────
        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex   Vert
            #pragma fragment Frag

            // HDRP コア
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            // 共通ホログラム関数
            #include "HologramCore.hlsl"

            // ─── 定数バッファ ───
            CBUFFER_START(UnityPerMaterial)
                float4 _HologramColor;
                float  _Alpha;
                float  _FresnelPower;
                float  _FresnelIntensity;
                float  _ScanlineFrequency;
                float  _ScanlineSpeed;
                float  _ScanlineIntensity;
                float  _GlitchIntensity;
                float  _GlitchSpeed;
            CBUFFER_END

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

                // 方法C: グリッチ頂点変位 (オブジェクト空間)
                float3 glitch  = Hologram_GlitchOffset(input.uv.y, _GlitchIntensity, _GlitchSpeed, time);
                float3 posOS   = input.positionOS.xyz + glitch;

                float3 posWS   = TransformObjectToWorld(posOS);
                output.positionCS = TransformWorldToHClip(posWS);
                output.positionWS = posWS;
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.uv         = input.uv;

                return output;
            }

            // ─── フラグメントシェーダー ───
            float4 Frag(Varyings input) : SV_Target
            {
                float time     = _Time.y;
                float3 normalWS = normalize(input.normalWS);

                // HDRP: GetCurrentViewPosition() でカメラのWS位置を取得
                float3 viewDir  = SafeNormalize(GetCurrentViewPosition() - input.positionWS);

                // 方法A: Fresnel
                float  fresnel  = Hologram_Fresnel(viewDir, normalWS, _FresnelPower);

                // 方法B: スキャンライン
                float  scanline = Hologram_Scanline(input.uv.y, _ScanlineFrequency, _ScanlineSpeed, time);

                // 方法C: グリッチ色ずれ
                float3 glitch   = Hologram_GlitchColor(input.uv.y, _GlitchIntensity, _GlitchSpeed, time);

                // 合成
                HologramOutput ho = Hologram_Combine(
                    _HologramColor.rgb,
                    _Alpha,
                    fresnel,       _FresnelIntensity,
                    scanline,      _ScanlineIntensity,
                    glitch
                );

                return float4(ho.color, ho.alpha);
            }

            ENDHLSL
        }

        // ─────────────────────────────────────────────
        //  DepthOnly Pass (シャドウ・被写界深度用)
        //  半透明なので ColorMask 0 で深度のみ書き込む
        // ─────────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex   DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct DepthAttributes { float4 positionOS : POSITION; };
            struct DepthVaryings   { float4 positionCS : SV_POSITION; };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            float4 DepthFrag(DepthVaryings input) : SV_Target { return 0; }

            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
