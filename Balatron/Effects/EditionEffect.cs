using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace Balatron.Effects
{
    /// <summary>
    /// GPU pixel-shader effect running Balatro's actual edition shaders
    /// (foil/holo/polychrome/negative), ported from the game's GLSL to
    /// HLSL ps_3_0. The shader receives the rendered card as its input,
    /// applies the edition pass and composites it — same two-pass model
    /// the game uses.
    /// </summary>
    public sealed class EditionEffect : ShaderEffect
    {
        private static readonly Dictionary<string, PixelShader> ShaderCache = new(StringComparer.OrdinalIgnoreCase);

        public static readonly DependencyProperty InputProperty =
            RegisterPixelShaderSamplerProperty("Input", typeof(EditionEffect), 0);

        public static readonly DependencyProperty TimeProperty =
            DependencyProperty.Register(nameof(Time), typeof(double), typeof(EditionEffect),
                new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0)));

        public static readonly DependencyProperty SeedProperty =
            DependencyProperty.Register(nameof(Seed), typeof(double), typeof(EditionEffect),
                new UIPropertyMetadata(0.0, PixelShaderConstantCallback(1)));

        public static readonly DependencyProperty TexelCountProperty =
            DependencyProperty.Register(nameof(TexelCount), typeof(Point), typeof(EditionEffect),
                new UIPropertyMetadata(new Point(71, 95), PixelShaderConstantCallback(2)));

        public Brush Input
        {
            get => (Brush)GetValue(InputProperty);
            set => SetValue(InputProperty, value);
        }

        public double Time
        {
            get => (double)GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }

        public double Seed
        {
            get => (double)GetValue(SeedProperty);
            set => SetValue(SeedProperty, value);
        }

        public Point TexelCount
        {
            get => (Point)GetValue(TexelCountProperty);
            set => SetValue(TexelCountProperty, value);
        }

        private readonly bool _animated;

        private EditionEffect(PixelShader shader, bool animated, double seed)
        {
            PixelShader = shader;
            _animated = animated;
            Seed = seed;
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(TimeProperty);
            UpdateShaderValue(SeedProperty);
            UpdateShaderValue(TexelCountProperty);
        }

        public void Start()
        {
            if (!_animated)
                return;
            // One linear ramp per effect instance; shader math is aperiodic so
            // the wrap after an hour is imperceptible.
            var animation = new DoubleAnimation(0, 3600, TimeSpan.FromHours(1))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            BeginAnimation(TimeProperty, animation);
        }

        /// <summary>
        /// Creates the effect for an edition name, or null when the edition has
        /// no shader or the GPU cannot run ps_3_0 (no software fallback exists).
        /// </summary>
        public static EditionEffect Create(string edition, string seedSource)
        {
            if (string.IsNullOrEmpty(edition))
                return null;
            if (!RenderCapability.IsPixelShaderVersionSupported(3, 0))
                return null;

            var shaderName = edition switch
            {
                "Foil" => "foil",
                "Holographic" => "holo",
                "Polychrome" => "polychrome",
                "Negative" => "negative",
                _ => null
            };
            if (shaderName == null)
                return null;

            var shader = GetShader(shaderName);
            if (shader == null)
                return null;

            var seed = Math.Abs((seedSource ?? string.Empty).GetHashCode() % 1000) / 100.0;
            return new EditionEffect(shader, animated: shaderName != "negative", seed);
        }

        private static PixelShader GetShader(string name)
        {
            if (ShaderCache.TryGetValue(name, out var cached))
                return cached;

            try
            {
                var shader = new PixelShader
                {
                    UriSource = new Uri($"pack://application:,,,/Balatron;component/Shaders/{name}.ps", UriKind.Absolute)
                };
                shader.Freeze();
                ShaderCache[name] = shader;
                return shader;
            }
            catch
            {
                ShaderCache[name] = null;
                return null;
            }
        }
    }
}
