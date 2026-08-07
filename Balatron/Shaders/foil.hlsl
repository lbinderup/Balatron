// Port of Balatro's foil.fs. The game sends foil = (slow drift, real time);
// we derive both from the animated time + per-card seed.
#include "common.hlsli"

float4 main(float2 tc : TEXCOORD) : COLOR
{
    float4 tex = sample_straight(tc);
    float4 base = tex;
    float alpha = tex.a;

    float2 foil = float2(time / 28.0 + seed, time + seed * 97.0);

    float2 uv = tc;
    float2 adjusted_uv = uv - 0.5;
    adjusted_uv.x = adjusted_uv.x * texel.x / texel.y;

    float low = min(tex.r, min(tex.g, tex.b));
    float high = max(tex.r, max(tex.g, tex.b));
    float delta = min(high, max(0.5, 1.0 - low));

    float len90 = length(90.0 * adjusted_uv);
    float fac = max(min(2.0 * sin((len90 + foil.x * 2.0) + 3.0 * (1.0 + 0.8 * cos(length(113.1121 * adjusted_uv) - foil.x * 3.121))) - 1.0 - max(5.0 - len90, 0.0), 1.0), 0.0);

    float2 rotater = float2(cos(foil.x * 0.1221), sin(foil.x * 0.3512));
    float angle = dot(rotater, adjusted_uv) / (length(rotater) * max(length(adjusted_uv), 0.00001));
    float fac2 = max(min(5.0 * cos(foil.y * 0.3 + angle * 3.14 * (2.2 + 0.9 * sin(foil.x * 1.65 + 0.2 * foil.y))) - 4.0 - max(2.0 - length(20.0 * adjusted_uv), 0.0), 1.0), 0.0);

    float fac3 = 0.3 * max(min(2.0 * sin(foil.x * 5.0 + uv.x * 3.0 + 3.0 * (1.0 + 0.5 * cos(foil.x * 7.0))) - 1.0, 1.0), -1.0);
    float fac4 = 0.3 * max(min(2.0 * sin(foil.x * 6.66 + uv.y * 3.8 + 3.0 * (1.0 + 0.5 * cos(foil.x * 3.414))) - 1.0, 1.0), -1.0);

    float maxfac = max(max(fac, max(fac2, max(fac3, max(fac4, 0.0)))) + 2.2 * (fac + fac2 + fac3 + fac4), 0.0);

    float4 fx;
    fx.r = tex.r - delta + delta * maxfac * 0.3;
    fx.g = tex.g - delta + delta * maxfac * 0.3;
    fx.b = tex.b + delta * maxfac * 1.9;
    fx.a = min(alpha, 0.3 * alpha + 0.9 * min(0.5, maxfac * 0.1));

    return composite_over(fx, base);
}
