using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Balatron.Models;

namespace Balatron.Views
{
    public sealed class PackHeaderViewModel
    {
        public string Name { get; init; }
        public ImageSource Sprite { get; init; }
        public int CardCount { get; init; }
        public string Label { get; init; }

        /// <summary>
        /// The full sequence as it plays out when this pack is opened first:
        /// the later pack's duplicates get resampled into different cards.
        /// </summary>
        public IReadOnlyList<PeekCardViewModel> SequenceWhenOpenedFirst { get; init; }
    }

    public sealed class SequenceCardViewModel : INotifyPropertyChanged
    {
        public PeekCardViewModel Card { get; init; }

        private bool _dimmed;
        public bool Dimmed
        {
            get => _dimmed;
            set
            {
                if (_dimmed == value)
                    return;
                _dimmed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Dimmed)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public partial class PackPeekWindow : Window
    {
        private readonly System.Collections.ObjectModel.ObservableCollection<SequenceCardViewModel> _cards = new();
        private IReadOnlyList<PeekCardViewModel> _defaultSequence;

        public PackPeekWindow()
        {
            InitializeComponent();
            CardsList.ItemsSource = _cards;
        }

        /// <summary>Single pack: plain contents view.</summary>
        public void ShowPack(string title, IReadOnlyList<PeekCardViewModel> cards)
        {
            TitleText.Text = title;
            PacksHeaderList.ItemsSource = null;
            PacksHeaderList.Visibility = Visibility.Collapsed;
            _defaultSequence = cards;
            SetCards(cards, null);
        }

        /// <summary>
        /// Shared-sequence view for multiple packs of the same kind: the packs
        /// sit on top, the combined card sequence underneath. Hovering a pack
        /// shows the sequence for opening that pack first and highlights its
        /// share of the cards.
        /// </summary>
        public void ShowSequence(string title, IReadOnlyList<PackHeaderViewModel> packs,
            IReadOnlyList<PeekCardViewModel> sequence)
        {
            TitleText.Text = title;
            PacksHeaderList.ItemsSource = packs;
            PacksHeaderList.Visibility = Visibility.Visible;
            _defaultSequence = sequence;
            SetCards(sequence, null);
        }

        private void SetCards(IReadOnlyList<PeekCardViewModel> cards, int? dimFrom)
        {
            _cards.Clear();
            for (var i = 0; i < cards.Count; i++)
            {
                _cards.Add(new SequenceCardViewModel
                {
                    Card = cards[i],
                    Dimmed = dimFrom.HasValue && i >= dimFrom.Value
                });
            }
        }

        private void PackHeader_MouseEnter(object sender, MouseEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not PackHeaderViewModel pack)
                return;

            SetCards(pack.SequenceWhenOpenedFirst ?? _defaultSequence, pack.CardCount);
        }

        private void PackHeader_MouseLeave(object sender, MouseEventArgs e)
        {
            SetCards(_defaultSequence, null);
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
