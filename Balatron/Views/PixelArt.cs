using System.Windows;
using System.Windows.Media;

namespace Balatron.Views
{
    /// <summary>
    /// Sizes a card-art container to an exact integer multiple of Balatro's
    /// 71x95 base sprite, measured in physical device pixels.
    ///
    /// Fractional scaling is what makes nearest-neighbour pixel art look
    /// uneven (some source rows duplicated, others not), so the box is derived
    /// from the display DPI rather than assuming 96. Scale 2 (142x190) is the
    /// useful one: joker sprites ship at 142x190 and land 1:1, while the 71x95
    /// sheets (tarots, enhancers, stickers, boosters, vouchers) land on a clean
    /// 2x.
    /// </summary>
    public static class PixelArt
    {
        public const double BaseWidth = 71;
        public const double BaseHeight = 95;

        public static readonly DependencyProperty ScaleProperty = DependencyProperty.RegisterAttached(
            "Scale", typeof(int), typeof(PixelArt), new PropertyMetadata(0, OnScaleChanged));

        public static int GetScale(DependencyObject element) => (int)element.GetValue(ScaleProperty);

        public static void SetScale(DependencyObject element, int value) => element.SetValue(ScaleProperty, value);

        private static readonly DependencyProperty HookedProperty = DependencyProperty.RegisterAttached(
            "Hooked", typeof(bool), typeof(PixelArt), new PropertyMetadata(false));

        private static void OnScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
                return;

            element.UseLayoutRounding = true;

            if (!(bool)element.GetValue(HookedProperty))
            {
                element.SetValue(HookedProperty, true);
                element.Loaded += (_, _) => Apply(element);
                element.DataContextChanged += (_, _) => Apply(element);
            }

            if (element.IsLoaded)
                Apply(element);
        }

        private static void Apply(FrameworkElement element)
        {
            var scale = GetScale(element);
            if (scale <= 0)
            {
                element.ClearValue(FrameworkElement.WidthProperty);
                element.ClearValue(FrameworkElement.HeightProperty);
                return;
            }

            // Width in DIPs that maps to exactly (BaseWidth * scale) device pixels.
            var dpi = VisualTreeHelper.GetDpi(element);
            element.Width = BaseWidth * scale / dpi.DpiScaleX;
            element.Height = BaseHeight * scale / dpi.DpiScaleY;
        }
    }
}
