using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace Balatron.Views
{
    /// <summary>
    /// Hover popups that can be layered. Moving the pointer into a popup keeps
    /// it open, and hovering a card inside it opens a further popup on top —
    /// so you can go from a tag, to the jokers it grants, to what one of those
    /// jokers does.
    ///
    /// This replaces ToolTipService rather than extending it: WPF tracks a
    /// single live tooltip, so opening a nested one would close its parent.
    /// </summary>
    public static class HoverPopup
    {
        /// <summary>Content to show on hover. Rendered with the usual DataTemplates.</summary>
        public static readonly DependencyProperty ContentProperty = DependencyProperty.RegisterAttached(
            "Content", typeof(object), typeof(HoverPopup), new PropertyMetadata(null, OnContentChanged));

        public static object GetContent(DependencyObject element) => element.GetValue(ContentProperty);

        public static void SetContent(DependencyObject element, object value) => element.SetValue(ContentProperty, value);

        /// <summary>Stamped on each popup's root so nested hovers can find their depth.</summary>
        private static readonly DependencyProperty DepthProperty = DependencyProperty.RegisterAttached(
            "Depth", typeof(int), typeof(HoverPopup), new PropertyMetadata(-1));

        private sealed class Layer
        {
            public Popup Popup;
            public FrameworkElement Anchor;
            public int Depth;
        }

        // Ordered shallowest-first.
        private static readonly List<Layer> Layers = new();
        private static readonly DispatcherTimer PruneTimer =
            new() { Interval = TimeSpan.FromMilliseconds(280) };

        static HoverPopup()
        {
            PruneTimer.Tick += (_, _) =>
            {
                PruneTimer.Stop();
                Prune();
            };
        }

        /// <summary>
        /// Drops every layer the pointer has left, deepest first, stopping at
        /// the first one still hovered — that layer and its ancestors stay up.
        /// </summary>
        private static void Prune()
        {
            for (var i = Layers.Count - 1; i >= 0; i--)
            {
                var layer = Layers[i];
                var overPopup = layer.Popup.Child is FrameworkElement child && child.IsMouseOver;
                if (overPopup || layer.Anchor.IsMouseOver)
                    return;
                Close(layer);
            }
        }

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
                return;

            element.MouseEnter -= OnAnchorEnter;
            element.MouseLeave -= OnAnchorLeave;
            element.Unloaded -= OnAnchorUnloaded;

            if (e.NewValue != null)
            {
                element.MouseEnter += OnAnchorEnter;
                element.MouseLeave += OnAnchorLeave;
                element.Unloaded += OnAnchorUnloaded;
            }
        }

        private static void OnAnchorEnter(object sender, RoutedEventArgs e)
        {
            var anchor = (FrameworkElement)sender;
            var content = GetContent(anchor);
            if (content == null)
                return;

            CancelPendingClose();

            var depth = ContainingDepth(anchor) + 1;

            // Re-entering the anchor of an already-open popup: just keep it.
            var existing = Layers.FirstOrDefault(l => l.Depth == depth);
            if (existing != null && ReferenceEquals(existing.Anchor, anchor))
                return;

            CloseFrom(depth);
            Show(anchor, content, depth);
        }

        private static void OnAnchorLeave(object sender, RoutedEventArgs e) => SchedulePrune();

        private static void OnAnchorUnloaded(object sender, RoutedEventArgs e)
        {
            var anchor = (FrameworkElement)sender;
            var layer = Layers.FirstOrDefault(l => ReferenceEquals(l.Anchor, anchor));
            if (layer != null)
                CloseFrom(layer.Depth);
        }

        private static void Show(FrameworkElement anchor, object content, int depth)
        {
            var root = new Border
            {
                Background = Brush("BalatroDark", "#1B1A22"),
                BorderBrush = Brush("BalatroShadow", "#14131A"),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(6),
                Effect = new DropShadowEffect { BlurRadius = 12, ShadowDepth = 2, Opacity = 0.5 },
                Child = new ContentPresenter { Content = content }
            };
            root.SetValue(DepthProperty, depth);

            var popup = new Popup
            {
                Child = root,
                PlacementTarget = anchor,
                // First level drops below the card; deeper levels fan out sideways.
                Placement = depth == 0 ? PlacementMode.Bottom : PlacementMode.Right,
                AllowsTransparency = true,
                StaysOpen = true,
                Focusable = false,
                PopupAnimation = PopupAnimation.Fade
            };

            var layer = new Layer { Popup = popup, Anchor = anchor, Depth = depth };

            root.MouseEnter += (_, _) => CancelPendingClose();
            root.MouseLeave += (_, _) => SchedulePrune();

            Layers.Add(layer);
            popup.IsOpen = true;
        }

        /// <summary>Depth of the popup containing this element, or -1 when it sits in a window.</summary>
        private static int ContainingDepth(DependencyObject element)
        {
            for (var node = element; node != null; node = VisualTreeHelper.GetParent(node))
            {
                if (node is FrameworkElement fe)
                {
                    var depth = (int)fe.GetValue(DepthProperty);
                    if (depth >= 0)
                        return depth;
                }
            }
            return -1;
        }

        private static void SchedulePrune()
        {
            PruneTimer.Stop();
            PruneTimer.Start();
        }

        private static void CancelPendingClose() => PruneTimer.Stop();

        private static void CloseFrom(int depth)
        {
            foreach (var layer in Layers.Where(l => l.Depth >= depth).ToList())
                Close(layer);
        }

        private static void Close(Layer layer)
        {
            layer.Popup.IsOpen = false;
            layer.Popup.Child = null;
            Layers.Remove(layer);
        }

        private static Brush Brush(string resourceKey, string fallback)
        {
            if (Application.Current?.TryFindResource(resourceKey) is Brush found)
                return found;
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(fallback));
        }
    }
}
