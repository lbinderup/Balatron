using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Balatron.Models;
using Balatron.Services.Cards;
using Balatron.Services.Prediction;

namespace Balatron.Views
{
    public sealed class CardFilter : INotifyPropertyChanged
    {
        public string Label { get; init; }
        public IReadOnlyList<string> Sets { get; init; }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive))); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public sealed class CardPickerEntry
    {
        public CenterDef Center { get; init; }
        public PeekCardViewModel Card { get; init; }
        public string SearchText { get; init; }
    }

    /// <summary>
    /// Pick any joker, consumable or voucher the game defines. The chosen
    /// center is built into a card by CardFactory, so nothing has to have been
    /// encountered in a run first.
    /// </summary>
    public partial class AddCardWindow : Window
    {
        private readonly List<CardPickerEntry> _all = new();
        private readonly ObservableCollection<CardPickerEntry> _shown = new();
        private readonly ObservableCollection<CardFilter> _filters = new();

        /// <summary>The center the user picked, or null if they closed the window.</summary>
        public CenterDef SelectedCenter { get; private set; }

        public AddCardWindow(IReadOnlyList<string> allowedSets)
        {
            InitializeComponent();
            ResultsList.ItemsSource = _shown;
            FilterButtons.ItemsSource = _filters;

            foreach (var set in allowedSets)
            {
                foreach (var center in CenterRegistry.All.Where(c => c.Set == set).OrderBy(c => c.Order))
                {
                    var card = BuildCard(center);
                    if (card == null)
                        continue;
                    _all.Add(new CardPickerEntry
                    {
                        Center = center,
                        Card = card,
                        SearchText = $"{center.Name} {center.Set}".ToLowerInvariant()
                    });
                }
            }

            // Consumables share one filter button; jokers and vouchers get their own.
            AddFilter("Jokers", allowedSets, "Joker");
            AddFilter("Consumables", allowedSets, "Tarot", "Planet", "Spectral");
            AddFilter("Vouchers", allowedSets, "Voucher");

            ApplyFilter();
            Loaded += (_, _) => SearchBox.Focus();
        }

        private void AddFilter(string label, IReadOnlyList<string> allowedSets, params string[] sets)
        {
            var usable = sets.Where(allowedSets.Contains).ToList();
            if (usable.Count > 0)
                _filters.Add(new CardFilter { Label = label, Sets = usable });
        }

        private static PeekCardViewModel BuildCard(CenterDef center)
        {
            if (center.IsVoucher)
                return PeekCardViewModel.FromVoucher(center.Key, null);

            var kind = center.Set switch
            {
                "Joker" => PredictedKind.Joker,
                "Tarot" => PredictedKind.Tarot,
                "Planet" => PredictedKind.Planet,
                "Spectral" => PredictedKind.Spectral,
                _ => (PredictedKind?)null
            };
            if (kind == null)
                return null;

            BalatroItems.JokersByKey.TryGetValue(center.Key, out var jokerDef);
            BalatroItems.ConsumablesByKey.TryGetValue(center.Key, out var consumableDef);

            return PeekCardViewModel.FromPrediction(new PredictedCard
            {
                Kind = kind.Value,
                CenterKey = center.Key,
                Name = center.Name,
                Text = jokerDef?.Text ?? consumableDef?.Text,
                Rarity = jokerDef?.Rarity ?? center.Rarity ?? 1
            });
        }

        private void ApplyFilter()
        {
            var query = SearchBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var active = _filters.Where(f => f.IsActive).SelectMany(f => f.Sets).ToHashSet(StringComparer.Ordinal);

            _shown.Clear();
            foreach (var entry in _all)
            {
                if (!active.Contains(entry.Center.Set))
                    continue;
                if (query.Length > 0 && !entry.SearchText.Contains(query, StringComparison.Ordinal))
                    continue;
                _shown.Add(entry);
            }
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ApplyFilter();

        private void FilterButton_Click(object sender, RoutedEventArgs e) => ApplyFilter();

        private void Entry_Click(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not CardPickerEntry entry)
                return;

            SelectedCenter = entry.Center;
            DialogResult = true;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
                return;
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
