using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
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
            Jokers = _editor.GetJokerViewModels(
                ImportJoker,
                ExportJoker,
                ToggleNegativeEdition,
                ToggleEternal,
                ToggleRental,
                TogglePerishable,
                EditPerishTally,
                EditSellCost);
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
            var eternalNode = abilityNode?.Children.FirstOrDefault(n => n.Key == "eternal");
            var rentalNode = abilityNode?.Children.FirstOrDefault(n => n.Key == "rental");
            var perishableNode = abilityNode?.Children.FirstOrDefault(n => n.Key == "perishable");
            var perishTallyNode = abilityNode?.Children.FirstOrDefault(n => n.Key == "perish_tally");
            var sellCostNode = joker.CardNode.Children.FirstOrDefault(n => n.Key == "sell_cost");

            joker.Label = labelNode?.Value ?? "Unknown";
            joker.Effect = effectNode?.Value ?? string.Empty;
            joker.SortId = sortIdNode != null && int.TryParse(sortIdNode.Value, out int sid) ? sid : 0;
            joker.Rank = rankNode != null && int.TryParse(rankNode.Value, out int r) ? r : 0;
            joker.IsNegativeEdition = LuaNodeTreeWindow.HasNegativeEdition(joker.CardNode);
            joker.IsEternal = eternalNode != null && string.Equals(eternalNode.Value, "true", System.StringComparison.OrdinalIgnoreCase);
            joker.IsRental = rentalNode != null && string.Equals(rentalNode.Value, "true", System.StringComparison.OrdinalIgnoreCase);
            joker.IsPerishable = perishableNode != null && string.Equals(perishableNode.Value, "true", System.StringComparison.OrdinalIgnoreCase);
            joker.PerishTally = perishTallyNode != null && int.TryParse(perishTallyNode.Value, out int pt) ? pt : 0;
            joker.SellCost = sellCostNode != null && int.TryParse(sellCostNode.Value, out int sc) ? sc : 0;
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

        private static LuaNode GetAbilityNode(JokerViewModel joker)
        {
            return joker?.CardNode?.Children.FirstOrDefault(n => n.Key == "ability");
        }

        private static void SetBooleanAbilityFlag(LuaNode abilityNode, string key, bool enabled)
        {
            if (abilityNode == null)
                return;

            var targetNode = abilityNode.Children.FirstOrDefault(n => n.Key == key);
            if (!enabled)
            {
                if (targetNode != null)
                {
                    abilityNode.Children.Remove(targetNode);
                }
                return;
            }

            if (targetNode == null)
            {
                targetNode = new LuaNode
                {
                    Key = key,
                    Parent = abilityNode
                };
                abilityNode.Children.Add(targetNode);
            }

            targetNode.Value = "true";
        }

        private void ToggleEternal(JokerViewModel joker)
        {
            var abilityNode = GetAbilityNode(joker);
            if (abilityNode == null)
                return;

            var newValue = !joker.IsEternal;
            SetBooleanAbilityFlag(abilityNode, "eternal", newValue);
            joker.IsEternal = newValue;
            _editor.PersistChanges();
        }

        private void ToggleRental(JokerViewModel joker)
        {
            var abilityNode = GetAbilityNode(joker);
            if (abilityNode == null)
                return;

            var newValue = !joker.IsRental;
            SetBooleanAbilityFlag(abilityNode, "rental", newValue);
            joker.IsRental = newValue;
            _editor.PersistChanges();
        }

        private void TogglePerishable(JokerViewModel joker)
        {
            var abilityNode = GetAbilityNode(joker);
            if (abilityNode == null)
                return;

            if (joker.IsPerishable)
            {
                RemovePerishable(abilityNode);
                joker.IsPerishable = false;
                joker.PerishTally = 0;
                _editor.PersistChanges();
                return;
            }

            if (RequestPerishTally(joker, out var newTally))
            {
                ApplyPerishable(abilityNode, joker, newTally);
            }
        }

        private void EditPerishTally(JokerViewModel joker)
        {
            var abilityNode = GetAbilityNode(joker);
            if (abilityNode == null)
                return;

            if (!RequestPerishTally(joker, out var newTally))
                return;

            ApplyPerishable(abilityNode, joker, newTally);
        }

        private void ApplyPerishable(LuaNode abilityNode, JokerViewModel joker, int tally)
        {
            SetBooleanAbilityFlag(abilityNode, "perishable", true);

            var tallyNode = abilityNode.Children.FirstOrDefault(n => n.Key == "perish_tally");
            if (tallyNode == null)
            {
                tallyNode = new LuaNode
                {
                    Key = "perish_tally",
                    Parent = abilityNode
                };
                abilityNode.Children.Add(tallyNode);
            }

            tallyNode.Value = tally.ToString();
            joker.IsPerishable = true;
            joker.PerishTally = tally;
            _editor.PersistChanges();
        }

        private static void RemovePerishable(LuaNode abilityNode)
        {
            var perishableNode = abilityNode.Children.FirstOrDefault(n => n.Key == "perishable");
            if (perishableNode != null)
            {
                abilityNode.Children.Remove(perishableNode);
            }

            var tallyNode = abilityNode.Children.FirstOrDefault(n => n.Key == "perish_tally");
            if (tallyNode != null)
            {
                abilityNode.Children.Remove(tallyNode);
            }
        }

        private bool RequestPerishTally(JokerViewModel joker, out int newTally)
        {
            var modifyWindow = new ModifyValuePopup($"Perish Tally for {joker.Label}", joker.PerishTally.ToString())
            {
                Owner = this
            };

            var mousePosition = Mouse.GetPosition(Application.Current.MainWindow);
            var windowPosition = Application.Current.MainWindow.PointToScreen(mousePosition);
            modifyWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            modifyWindow.Left = windowPosition.X;
            modifyWindow.Top = windowPosition.Y;

            newTally = 0;
            if (modifyWindow.ShowDialog() == true && int.TryParse(modifyWindow.NewValue, out var parsed))
            {
                newTally = parsed;
                return true;
            }

            return false;
        }

        private void EditSellCost(JokerViewModel joker)
        {
            if (joker?.CardNode == null)
                return;

            var sellCostNode = joker.CardNode.Children.FirstOrDefault(n => n.Key == "sell_cost");
            if (sellCostNode == null)
            {
                sellCostNode = new LuaNode
                {
                    Key = "sell_cost",
                    Parent = joker.CardNode
                };
                joker.CardNode.Children.Add(sellCostNode);
            }

            var modifyWindow = new ModifyValuePopup($"Sell Cost for {joker.Label}", sellCostNode.Value)
            {
                Owner = this
            };

            var mousePosition = Mouse.GetPosition(Application.Current.MainWindow);
            var windowPosition = Application.Current.MainWindow.PointToScreen(mousePosition);
            modifyWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            modifyWindow.Left = windowPosition.X;
            modifyWindow.Top = windowPosition.Y;

            if (modifyWindow.ShowDialog() == true && int.TryParse(modifyWindow.NewValue, out var newSellCost))
            {
                sellCostNode.Value = newSellCost.ToString();
                joker.SellCost = newSellCost;
                _editor.PersistChanges();
            }
        }
    }
}