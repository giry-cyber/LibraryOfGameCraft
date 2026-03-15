// HologramCore.hlsl
// ホログラムシェーダー共通関数ライブラリ
// HDRP / URP 両方から #include して使用する
//
// 依存: Common.hlsl (CoreRP) がインクルード済みであること

#ifndef HOLOGRAM_CORE_HLSL
#define HOLOGRAM_CORE_HLSL

// ─────────────────────────────────────────────
//  ノイズ系ユーティリティ
// ─────────────────────────────────────────────

// 疑似乱数 (2D → スカラー)
float Hologram_Hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
}

// バリューノイズ (2D → [0,1])
float Hologram_Noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f); // Smoothstep

    float a = Hologram_Hash(i);
    float b = Hologram_Hash(i + float2(1.0, 0.0));
    float c = Hologram_Hash(i + float2(0.0, 1.0));
    float d = Hologram_Hash(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// ─────────────────────────────────────────────
//  方法A: Fresnel 発光
//  viewDir, normalWS は normalize 済みであること
// ─────────────────────────────────────────────

// fresnel: エッジに近づくほど 1 に近づく係数 [0,1]
float Hologram_Fresnel(float3 viewDir, float3 normalWS, float power)
{
    float vDotN = saturate(dot(viewDir, normalWS));
    return pow(1.0 - vDotN, power);
}

// ─────────────────────────────────────────────
//  方法B: スキャンライン
//  uvY    : UV.y 座標
//  freq   : 周波数 (走査線の密度)
//  speed  : スクロール速度
//  time   : 経過時間 (_Time.y)
//  戻り値: [0,1] 明暗パターン
// ─────────────────────────────────────────────

float Hologram_Scanline(float uvY, float freq, float speed, float time)
{
    float raw = sin(uvY * freq + time * speed);
    return raw * 0.5 + 0.5; // [-1,1] → [0,1]
}

// ─────────────────────────────────────────────
//  方法C: グリッチ（頂点変位用）
//  uvY       : UV.y (行ごとに異なるランダムシードとして利用)
//  intensity : 変位の最大量
//  speed     : グリッチ速度
//  time      : 経過時間 (_Time.y)
//  戻り値: オブジェクト空間での X 方向オフセット float3
// ─────────────────────────────────────────────

float3 Hologram_GlitchOffset(float uvY, float intensity, float speed, float time)
{
    // 横方向のランダムノイズ
    float noise = Hologram_Noise(float2(uvY * 10.0, time * speed));
    // グリッチが発生する瞬間だけ有効にするゲートマスク
    float gate  = step(0.95, Hologram_Noise(float2(time * speed * 0.31, 7.3)));
    return float3(noise * intensity * gate, 0.0, 0.0);
}

// ─────────────────────────────────────────────
//  方法C: グリッチ（色ずれ用）
//  戻り値: RGB色ずれ量 (Rチャンネルが主に動く)
// ─────────────────────────────────────────────

float3 Hologram_GlitchColor(float uvY, float intensity, float speed, float time)
{
    float noise = Hologram_Noise(float2(uvY * 5.0, time * speed));
    float gate  = step(0.97, Hologram_Noise(float2(time * speed * 0.17, 13.1)));
    return float3(noise * gate * intensity * 2.0, 0.0, 0.0);
}

// ─────────────────────────────────────────────
//  3要素を合成して最終カラーとアルファを返す
//  baseColor       : ホログラムの基本色 (HDR可)
//  baseAlpha       : ベースアルファ
//  fresnel         : Hologram_Fresnel の戻り値
//  fresnelIntensity: Fresnel の強さスケール
//  scanline        : Hologram_Scanline の戻り値
//  scanlineIntensity: スキャンライン効果の強さ [0,1]
//  glitchColor     : Hologram_GlitchColor の戻り値
// ─────────────────────────────────────────────

struct HologramOutput
{
    float3 color;
    float  alpha;
};

HologramOutput Hologram_Combine(
    float3 baseColor,
    float  baseAlpha,
    float  fresnel,
    float  fresnelIntensity,
    float  scanline,
    float  scanlineIntensity,
    float3 glitchColor)
{
    HologramOutput o;

    // スキャンラインは [0,1] → [1-intensity, 1] の範囲に再マップして暗くなりすぎを防ぐ
    float scanlineFactor = lerp(1.0, scanline, scanlineIntensity);

    // 発光強度 = ベース + Fresnel 補正
    float3 emissive = baseColor * (1.0 + fresnel * fresnelIntensity);

    // スキャンライン適用
    o.color = emissive * scanlineFactor + glitchColor;

    // アルファ: 正面でも baseAlpha の 70% を最低保証し、エッジでさらに濃くなる
    // (0.4 → 0.7 に引き上げて正面でも視認できるようにした)
    o.alpha  = baseAlpha * (0.7 + fresnel * 0.3) * scanlineFactor;
    o.alpha  = saturate(o.alpha);

    return o;
}

#endif // HOLOGRAM_CORE_HLSL
