using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using Balatron.Models;
using Balatron.Services;

namespace Balatron.Views
{
    public partial class JokerListWindow : Window
    {
        private readonly LuaNodeTreeWindow _editor;
        public ObservableCollection<JokerViewModel> Jokers { get; set; }

        public JokerListWindow(LuaNodeTreeWindow editor)
        {
            InitializeComponent();
            _editor = editor;
            Jokers = _editor.GetJokerViewModels(ImportJoker, ExportJoker, ToggleNegativeEdition);
            DataContext = this;
        }

        private void ExportJoker(JokerViewModel joker)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Joker JSON (*.json)|*.json",
                FileName = $"joker_slot_{joker.SlotIndex}.json"
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

            var imported = JokerFileService.ImportJoker(openFileDialog.FileName);
            if (imported == null)
            {
                MessageBox.Show("Unable to read joker data from the selected file.", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _editor.ReplaceJoker(joker.CardNode, imported);
            joker.CardNode = imported;
            UpdateJokerMetadata(joker);
            MessageBox.Show("Joker imported into the slot.", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateJokerMetadata(JokerViewModel joker)
        {
            if (joker.CardNode == null)
                return;

            var labelNode = joker.CardNode.Children.FirstOrDefault(n => n.Key == "label");
            var abilityNode = joker.CardNode.Children.FirstOrDefault(n => n.Key == "ability");
            var effectNode = abilityNode?.Children.FirstOrDefault(n => n.Key == "effect");
            var sortIdNode = joker.CardNode.Children.FirstOrDefault(n => n.Key == "sort_id");
            var rankNode = joker.CardNode.Children.FirstOrDefault(n => n.Key == "rank");

            joker.Label = labelNode?.Value ?? "Unknown";
            joker.Effect = effectNode?.Value ?? string.Empty;
            joker.SortId = sortIdNode != null && int.TryParse(sortIdNode.Value, out int sid) ? sid : 0;
            joker.Rank = rankNode != null && int.TryParse(rankNode.Value, out int r) ? r : 0;
            joker.IsNegativeEdition = LuaNodeTreeWindow.HasNegativeEdition(joker.CardNode);
        }

        private void ToggleNegativeEdition(JokerViewModel joker)
        {
            if (joker?.CardNode == null)
                return;

            var editionNode = joker.CardNode.Children.FirstOrDefault(n => n.Key == "edition");

            if (editionNode != null)
            {
                joker.CardNode.Children.Remove(editionNode);
            }
            else
            {
                editionNode = new LuaNode
                {
                    Key = "edition",
                    Parent = joker.CardNode,
                    IsTable = true
                };

                var negativeNode = new LuaNode
                {
                    Key = "negative",
                    Parent = editionNode,
                    Value = "true"
                };

                var typeNode = new LuaNode
                {
                    Key = "type",
                    Parent = editionNode,
                    Value = "\"negative\""
                };

                editionNode.Children.Add(negativeNode);
                editionNode.Children.Add(typeNode);
                joker.CardNode.Children.Add(editionNode);
            }

            joker.IsNegativeEdition = LuaNodeTreeWindow.HasNegativeEdition(joker.CardNode);
            _editor.PersistChanges();
        }
    }
}