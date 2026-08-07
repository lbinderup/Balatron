// Port of Balatro's holo.fs.
#include "common.hlsli"

float4 main(float2 tc : TEXCOORD) : COLOR
{
    float4 tex = sample_straight(tc);
    float4 base = tex;
    float alpha = tex.a;

    float2 holo = float2(time / 28.0 + seed, time + seed * 97.0);
    float2 uv = tc;

    float4 hsl = HSL(0.5 * tex + 0.5 * float4(0.0, 0.0, 1.0, alpha));

    float t = holo.y * 7.221 + time;
    float2 floored_uv = floor(uv * texel) / texel;
    float2 uv_scaled_centered = (floored_uv - 0.5) * 250.0;

    float field = field_at(uv_scaled_centered, t);
    float res = 0.5 + 0.5 * cos(holo.x * 2.612 + (field - 0.5) * 3.14);

    float low = min(tex.r, min(tex.g, tex.b));
    float high = max(tex.r, max(tex.g, tex.b));
    float delta = 0.2 + 0.3 * (high - low) + 0.1 * high;

    float gridsize = 0.79;
    float fac = 0.5 * max(max(max(0.0, 7.0 * abs(cos(uv.x * gridsize * 20.0)) - 6.0),
        max(0.0, 7.0 * cos(uv.y * gridsize * 45.0 + uv.x * gridsize * 20.0) - 6.0)),
        max(0.0, 7.0 * cos(uv.y * gridsize * 45.0 - uv.x * gridsize * 20.0) - 6.0));

    hsl.x = hsl.x + res + fac;
    hsl.y = hsl.y * 1.3;
    hsl.z = hsl.z * 0.6 + 0.4;

    float4 fx = (1.0 - delta) * float4(tex.rgb, alpha) + delta * RGB(hsl) * float4(0.9, 0.8, 1.2, alpha);
    if (fx.a < 0.7)
        fx.a = fx.a / 3.0;

    return composite_over(fx, base);
}
