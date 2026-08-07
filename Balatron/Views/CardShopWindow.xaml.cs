using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Balatron.Models;
using Balatron.Services;
using Balatron.Services.Cards;

namespace Balatron.Views
{
    public partial class CardShopWindow : Window
    {
        private readonly LuaNodeTreeWindow _editor;
        public ObservableCollection<JokerViewModel> ShopCards { get; set; }

        public CardShopWindow(LuaNodeTreeWindow editor)
        {
            InitializeComponent();
            _editor = editor;
            ShopCards = _editor.GetShopCardViewModels(ImportCard, ExportCard);
            DataContext = this;
            EmptyText.Visibility = ShopCards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AddCardButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new AddCardWindow(new[] { "Joker", "Tarot", "Planet", "Spectral", "Voucher" }) { Owner = this };
            if (picker.ShowDialog() != true || picker.SelectedCenter == null)
                return;

            var center = picker.SelectedCenter;
            var area = LuaNodeTreeWindow.AreaForShopCard(center);

            if (!_editor.AddCardToArea(area, CardFactory.Create(center), replaceExisting: center.IsVoucher))
            {
                MessageBox.Show($"Could not find the '{area}' card area in this save.",
                    "Add Card", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (center.IsVoucher)
            {
                MessageBox.Show($"{center.Name} is now the shop's voucher.",
                    "Add Card", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var refreshed = _editor.GetShopCardViewModels(ImportCard, ExportCard);
            ShopCards.Clear();
            foreach (var card in refreshed)
                ShopCards.Add(card);
            EmptyText.Visibility = Visibility.Collapsed;
        }

        private void ExportCard(JokerViewModel card)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Card JSON (*.json)|*.json",
                FileName = $"{LuaNodeTreeWindow.SanitizeFileName(card.Label)}.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                JokerFileService.ExportJoker(card.CardNode, saveFileDialog.FileName);
                MessageBox.Show("Card exported successfully.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ImportCard(JokerViewModel card)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Card JSON (*.json)|*.json"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            if (!File.Exists(openFileDialog.FileName))
                return;

            var imported = JokerFileService.ImportJoker(openFileDialog.FileName);
            if (imported == null)
            {
                MessageBox.Show("Unable to read card data from the selected file.", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _editor.ReplaceJoker(card.CardNode, imported);
            card.CardNode = imported;
            _editor.RefreshJokerMetadata(card);
            MessageBox.Show("Card imported into the shop slot.", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
                return;

            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}
