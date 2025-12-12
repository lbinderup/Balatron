using System;
using System.Collections.ObjectModel;
using System.Globalization;
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
                ToggleEternal,
                ToggleRental,
                TogglePerishable,
                EditPerishTally,
                EditSellCost,
                SetEdition);
            DataContext = this;
        }

        private static void ExportJoker(JokerViewModel joker)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Joker JSON (*.json)|*.json",
                FileName = $"{LuaNodeTreeWindow.SanitizeFileName(joker.Label)}.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                JokerFileService.ExportJoker(joker.CardNode, saveFileDialog.FileName);
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
            _editor.RefreshJokerMetadata(joker);
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

        private void SetEdition(JokerViewModel joker, string edition)
        {
            if (joker?.CardNode == null)
                return;

            ApplyEdition(joker.CardNode, edition);
            joker.SetSelectedEditionSilently(LuaNodeTreeWindow.GetEditionType(joker.CardNode));
            _editor.PersistChanges();
        }

        private static void ApplyEdition(LuaNode cardNode, string edition)
        {
            if (cardNode == null)
                return;

            var currentEdition = cardNode.Children.FirstOrDefault(n => n.Key == "edition");
            if (currentEdition != null)
            {
                cardNode.Children.Remove(currentEdition);
            }

            if (string.Equals(edition, "None", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(edition))
                return;

            var editionNode = new LuaNode
            {
                Key = "edition",
                Parent = cardNode,
                IsTable = true
            };

            void AddChild(string key, string value)
            {
                editionNode.Children.Add(new LuaNode
                {
                    Key = key,
                    Parent = editionNode,
                    Value = value
                });
            }

            switch (edition)
            {
                case "Negative":
                    AddChild("negative", "true");
                    AddChild("type", "\"negative\"");
                    break;
                case "Foil":
                    AddChild("type", "\"foil\"");
                    AddChild("chips", "50");
                    AddChild("foil", "true");
                    break;
                case "Holographic":
                    AddChild("type", "\"holo\"");
                    AddChild("holo", "true");
                    AddChild("mult", "10");
                    break;
                case "Polychrome":
                    AddChild("type", "\"polychrome\"");
                    AddChild("x_mult", 1.5.ToString(CultureInfo.InvariantCulture));
                    AddChild("polychrome", "true");
                    break;
                default:
                    return;
            }

            cardNode.Children.Add(editionNode);
        }
    }
}