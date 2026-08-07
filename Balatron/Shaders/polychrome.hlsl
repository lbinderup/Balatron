// Port of Balatro's polychrome.fs.
#include "common.hlsli"

float4 main(float2 tc : TEXCOORD) : COLOR
{
    float4 tex = sample_straight(tc);
    float4 base = tex;
    float alpha = tex.a;

    float2 polychrome = float2(time / 28.0 + seed, time + seed * 97.0);
    float2 uv = tc;

    float low = min(tex.r, min(tex.g, tex.b));
    float high = max(tex.r, max(tex.g, tex.b));
    float delta = high - low;

    float saturation_fac = 1.0 - max(0.0, 0.05 * (1.1 - delta));
    float4 hsl = HSL(float4(tex.r * saturation_fac, tex.g * saturation_fac, tex.b, alpha));

    float t = polychrome.y * 2.221 + time;
    float2 floored_uv = floor(uv * texel) / texel;
    float2 uv_scaled_centered = (floored_uv - 0.5) * 50.0;

    float field = field_at(uv_scaled_centered, t);
    float res = 0.5 + 0.5 * cos(polychrome.x * 2.612 + (field - 0.5) * 3.14);

    hsl.x = hsl.x + res + polychrome.y * 0.04;
    hsl.y = min(0.6, hsl.y + 0.5);

    float4 fx = float4(RGB(hsl).rgb, alpha);
    if (fx.a < 0.7)
        fx.a = fx.a / 3.0;

    return composite_over(fx, base);
}
