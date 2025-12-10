using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Balatron.Models;
using Balatron.Services;

namespace Balatron.Views
{
    public partial class ShopJokerWindow : Window
    {
        private readonly LuaNodeTreeWindow _editor;
        public ObservableCollection<JokerViewModel> ShopJokers { get; set; }

        public ShopJokerWindow(LuaNodeTreeWindow editor)
        {
            InitializeComponent();
            _editor = editor;
            ShopJokers = _editor.GetShopJokerViewModels(ImportJoker, ExportJoker);
            DataContext = this;
        }

        private void ExportJoker(JokerViewModel joker)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Joker JSON (*.json)|*.json",
                FileName = $"{LuaNodeTreeWindow.SanitizeFileName(joker.Label)}.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                JokerFileService.ExportJoker(joker.CardNode, saveFileDialog.FileName);
                MessageBox.Show("Joker exported successfully.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ImportJoker(JokerViewModel joker)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Joker JSON (*.json)|*.json"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            if (!File.Exists(openFileDialog.FileName))
                return;

            var imported = JokerFileService.ImportJoker(openFileDialog.FileName);
            if (imported == null)
            {
                MessageBox.Show("Unable to read joker data from the selected file.", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _editor.ReplaceJoker(joker.CardNode, imported);
            joker.CardNode = imported;
            _editor.RefreshJokerMetadata(joker);
            MessageBox.Show("Joker imported into the shop slot.", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
