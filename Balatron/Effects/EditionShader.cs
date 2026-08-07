using System.Windows;

namespace Balatron.Effects
{
    /// <summary>
    /// Attached behavior: bind an edition name ("Foil", "Holographic",
    /// "Polychrome", "Negative") to any element to run the matching Balatro
    /// edition shader over it. "None"/null clears the effect.
    /// </summary>
    public static class EditionShader
    {
        public static readonly DependencyProperty EditionProperty = DependencyProperty.RegisterAttached(
            "Edition", typeof(string), typeof(EditionShader), new PropertyMetadata(null, OnEditionChanged));

        public static string GetEdition(DependencyObject obj) => (string)obj.GetValue(EditionProperty);

        public static void SetEdition(DependencyObject obj, string value) => obj.SetValue(EditionProperty, value);

        private static void OnEditionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element)
                return;

            var effect = EditionEffect.Create(e.NewValue as string, d.GetHashCode().ToString());
            element.Effect = effect;
            effect?.Start();
        }
    }
}
