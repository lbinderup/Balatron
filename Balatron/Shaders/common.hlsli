// Shared helpers for the Balatro edition shader ports (HLSL ps_3_0).
// Ported from the game's GLSL (resources/shaders/*.fs).
//
// Register layout (set by EditionEffect):
//   s0 = element rendering (premultiplied alpha)
//   c0 = time (seconds, animated)
//   c1 = per-card seed
//   c2 = sprite texel counts (71, 95)

sampler2D input : register(s0);
float time : register(c0);
float seed : register(c1);
float2 texel : register(c2);

// GLSL mod(): floored, unlike HLSL fmod().
float glmod(float x, float y)
{
    return x - y * floor(x / y);
}

float hue(float s, float t, float h)
{
    float hs = glmod(h, 1.0) * 6.0;
    if (hs < 1.0) return (t - s) * hs + s;
    if (hs < 3.0) return t;
    if (hs < 4.0) return (t - s) * (4.0 - hs) + s;
    return s;
}

float4 RGB(float4 c)
{
    if (c.y < 0.0001)
        return float4(c.z, c.z, c.z, c.a);

    float t = (c.z < 0.5) ? c.y * c.z + c.z : -c.y * c.z + (c.y + c.z);
    float s = 2.0 * c.z - t;
    return float4(hue(s, t, c.x + 1.0 / 3.0), hue(s, t, c.x), hue(s, t, c.x - 1.0 / 3.0), c.w);
}

float4 HSL(float4 c)
{
    float low = min(c.r, min(c.g, c.b));
    float high = max(c.r, max(c.g, c.b));
    float delta = high - low;
    float sum = high + low;

    float4 hsl = float4(0.0, 0.0, 0.5 * sum, c.a);
    if (delta == 0.0)
        return hsl;

    hsl.y = (hsl.z < 0.5) ? delta / sum : delta / (2.0 - sum);

    if (high == c.r)
        hsl.x = (c.g - c.b) / delta;
    else if (high == c.g)
        hsl.x = (c.b - c.r) / delta + 2.0;
    else
        hsl.x = (c.r - c.g) / delta + 4.0;

    hsl.x = glmod(hsl.x / 6.0, 1.0);
    return hsl;
}

// The trig interference field shared by holo/polychrome (and foil's dissolve).
float field_at(float2 uv_scaled_centered, float t)
{
    float2 p1 = uv_scaled_centered + 50.0 * float2(sin(-t / 143.6340), cos(-t / 99.4324));
    float2 p2 = uv_scaled_centered + 50.0 * float2(cos(t / 53.1532), cos(t / 61.4532));
    float2 p3 = uv_scaled_centered + 50.0 * float2(sin(-t / 87.53218), sin(-t / 49.0000));

    return (1.0 + (
        cos(length(p1) / 19.483) + sin(length(p2) / 33.155) * cos(p2.y / 15.73) +
        cos(length(p3) / 27.193) * sin(p3.x / 21.92))) / 2.0;
}

// WPF hands us premultiplied alpha; the GLSL works in straight alpha.
float4 sample_straight(float2 tc)
{
    float4 tex = tex2D(input, tc);
    if (tex.a > 0.0001)
        tex.rgb /= tex.a;
    return tex;
}

// The game renders the card sprite, then the edition shader as a second pass
// on top. Composite fx (straight alpha) over base (straight alpha) and return
// premultiplied for WPF.
float4 composite_over(float4 fx, float4 base)
{
    float outA = fx.a + base.a * (1.0 - fx.a);
    float3 outRGB = fx.rgb * fx.a + base.rgb * base.a * (1.0 - fx.a);
    return float4(outRGB, outA);
}
