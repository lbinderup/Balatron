// Port of Balatro's negative.fs: hue flip + lightness inversion + teal tint.
#include "common.hlsli"

float4 main(float2 tc : TEXCOORD) : COLOR
{
    float4 tex = sample_straight(tc);
    float4 base = tex;
    float alpha = tex.a;

    float4 sat = HSL(tex);
    sat.z = 1.0 - sat.z;
    sat.x = -sat.x + 0.2;

    float4 fx = RGB(sat) + 0.8 * float4(79.0 / 255.0, 99.0 / 255.0, 103.0 / 255.0, 0.0);
    fx.a = alpha;
    if (fx.a < 0.7)
        fx.a = fx.a / 3.0;

    return composite_over(fx, base);
}
