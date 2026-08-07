using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Balatron.Models;
using Balatron.Services;
using Balatron.Services.Prediction;

namespace Balatron.Views
{
    public partial class LuaNodeTreeWindow : Window
    {
        private static LuaNodeTreeWindow _instance;
        public static LuaNodeTreeWindow Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LuaNodeTreeWindow();
                    _instance.Closed += (s, e) => _instance = null;
                }
                return _instance;
            }
        }

        private LuaNode _rootNode;
        private LuaNode _selectedNode;
        private readonly string _tempFilePath = Path.Combine(Path.GetTempPath(), "save.txt");

        private LuaNodeTreeWindow()
        {
            InitializeComponent();
            LoadAndParseLuaFile();
        }

        private void LoadAndParseLuaFile()
        {
            if (File.Exists(_tempFilePath))
            {
                string luaText = File.ReadAllText(_tempFilePath, Encoding.ASCII);
                _rootNode = LuaParser.Parse(luaText);
                LuaTreeView.ItemsSource = _rootNode.Children;
            }
            else
            {
                MessageBox.Show("Temporary save file not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ReloadFromTempFile()
        {
            LoadAndParseLuaFile();
        }

        private void LuaTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _selectedNode = e.NewValue as LuaNode;
            if (_selectedNode != null && _selectedNode.IsLeaf)
            {
                AddressTextBox.Text = GetAddress(_selectedNode);
                ModifyValueButton.IsEnabled = true;
            }
            else
            {
                AddressTextBox.Text = "";
                ModifyValueButton.IsEnabled = false;
            }
        }
        
        private void LuaTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is not LuaNode { IsLeaf: true })
                return;

            ModifyValueButton_Click(sender, e);
        }

        private static string GetAddress(LuaNode node)
        {
            var address = node.Key;
            var current = node.Parent;
            while (current != null && current.Key != "Root")
            {
                address = current.Key + "." + address;
                current = current.Parent;
            }
            return address;
        }

        private void ModifyValueButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode is not { IsLeaf: true })
                return;

            var modifyWindow = new ModifyValuePopup(GetAddress(_selectedNode), _selectedNode.Value);

            var mousePosition = Mouse.GetPosition(this);
            var windowPosition = PointToScreen(mousePosition);

            modifyWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            modifyWindow.Left = windowPosition.X;
            modifyWindow.Top = windowPosition.Y;

            if (modifyWindow.ShowDialog() != true)
                return;

            _selectedNode.Value = modifyWindow.NewValue;
            var newLuaText = LuaSerializer.Serialize(_rootNode);
            File.WriteAllText(_tempFilePath, newLuaText, Encoding.ASCII);

            SavegameEditorWindow.Current?.RePopulateTextEditor();
        }

        public string GetValueByAddress(string address)
        {
            if (_rootNode == null)
                return "";
            var parts = address.Split('.');
            var current = _rootNode;
            foreach (var part in parts)
            {
                current = current.Children.FirstOrDefault(n => n.Key == part);
                if (current == null)
                    return "";
            }
            return current.Value;
        }

        public void SetValueByAddress(string address, string newValue)
        {
            if (_rootNode == null)
                return;
            var parts = address.Split('.');
            var current = _rootNode;
            foreach (var part in parts)
            {
                current = current.Children.FirstOrDefault(n => n.Key == part);
                if (current == null)
                    return;
            }
            current.Value = newValue;
            // Re-serialize the entire Lua tree and write to the temp file.
            var newLuaText = LuaSerializer.Serialize(_rootNode);
            File.WriteAllText(_tempFilePath, newLuaText, Encoding.ASCII);
        }
        
        public ObservableCollection<JokerViewModel> GetJokerViewModels(
            Action<JokerViewModel> importAction,
            Action<JokerViewModel> exportAction,
            Action<JokerViewModel> toggleEternalAction,
            Action<JokerViewModel> toggleRentalAction,
            Action<JokerViewModel> togglePerishableAction,
            Action<JokerViewModel> editPerishTallyAction,
            Action<JokerViewModel> editSellCostAction,
            Action<JokerViewModel, string> setEditionAction)
        {
            var jokers = new ObservableCollection<JokerViewModel>();

            var cardsNode = GetCardsNode("jokers");
            if (cardsNode == null) return jokers;

            foreach (var card in cardsNode.Children)
            {
                var slotIndex = int.TryParse(card.Key, out var keyIndex) ? keyIndex : jokers.Count + 1;
                var joker = CreateJokerViewModel(card, slotIndex, importAction, exportAction, toggleEternalAction,
                    toggleRentalAction, togglePerishableAction, editPerishTallyAction, editSellCostAction, setEditionAction);
                jokers.Add(joker);
            }
            return jokers;
        }

        /// <summary>Every card in the shop's card slots — jokers, consumables and playing cards.</summary>
        public ObservableCollection<JokerViewModel> GetShopCardViewModels(
            Action<JokerViewModel> importAction,
            Action<JokerViewModel> exportAction)
        {
            var shopCards = new ObservableCollection<JokerViewModel>();

            var cardsNode = GetCardsNode("shop_jokers");
            if (cardsNode == null) return shopCards;

            foreach (var card in cardsNode.Children.OrderBy(c => int.TryParse(c.Key, out var i) ? i : int.MaxValue))
            {
                var slotIndex = int.TryParse(card.Key, out var keyIndex) ? keyIndex : shopCards.Count + 1;
                var joker = CreateJokerViewModel(card, slotIndex, importAction, exportAction, null, null, null, null, null, null);
                shopCards.Add(joker);
            }

            return shopCards;
        }

        public void ReplaceJoker(LuaNode originalJoker, LuaNode newJoker)
        {
            if (originalJoker?.Parent == null || newJoker == null)
                return;

            var parent = originalJoker.Parent;
            var index = parent.Children.IndexOf(originalJoker);
            if (index < 0)
                return;

            newJoker.Key = originalJoker.Key;
            newJoker.Parent = parent;
            parent.Children[index] = newJoker;

            PersistChanges();
        }

        public void PersistChanges()
        {
            var newLuaText = LuaSerializer.Serialize(_rootNode);
            File.WriteAllText(_tempFilePath, newLuaText, Encoding.ASCII);

            SavegameEditorWindow.Current?.RePopulateTextEditor();
        }

        public void RefreshJokerMetadata(JokerViewModel joker)
        {
            PopulateJokerMetadata(joker?.CardNode, joker);
        }

        public static bool HasNegativeEdition(LuaNode cardNode)
        {
            return string.Equals(GetEditionType(cardNode), "Negative", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetEditionType(LuaNode cardNode)
        {
            if (cardNode == null)
                return "None";

            var editionNode = cardNode.Children.FirstOrDefault(n => n.Key == "edition");
            if (editionNode == null)
                return "None";

            var typeNode = editionNode.Children.FirstOrDefault(n => n.Key == "type");
            var typeValue = typeNode?.Value?.Trim('"').ToLowerInvariant();

            return typeValue switch
            {
                "negative" => "Negative",
                "foil" => "Foil",
                "holo" or "holographic" => "Holographic",
                "polychrome" => "Polychrome",
                _ => InferEditionFromFlags(editionNode)
            };
        }

        private static string InferEditionFromFlags(LuaNode editionNode)
        {
            if (editionNode.Children.Any(c => c.Key == "negative"))
                return "Negative";
            if (editionNode.Children.Any(c => c.Key == "foil"))
                return "Foil";
            if (editionNode.Children.Any(c => c.Key == "holo"))
                return "Holographic";
            if (editionNode.Children.Any(c => c.Key == "polychrome"))
                return "Polychrome";

            return "None";
        }

        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "joker";

            var invalidChars = Path.GetInvalidFileNameChars();
            var cleaned = string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? "joker" : cleaned;
        }

        private LuaNode GetCardsNode(string areaKey)
        {
            var cardAreas = _rootNode?.Children.FirstOrDefault(n => n.Key == "cardAreas");
            if (cardAreas == null)
                return null;

            var targetArea = cardAreas.Children.FirstOrDefault(n => n.Key == areaKey);
            return targetArea?.Children.FirstOrDefault(n => n.Key == "cards");
        }

        private JokerViewModel CreateJokerViewModel(
            LuaNode card,
            int slotIndex,
            Action<JokerViewModel> importAction,
            Action<JokerViewModel> exportAction,
            Action<JokerViewModel> toggleEternalAction,
            Action<JokerViewModel> toggleRentalAction,
            Action<JokerViewModel> togglePerishableAction,
            Action<JokerViewModel> editPerishTallyAction,
            Action<JokerViewModel> editSellCostAction,
            Action<JokerViewModel, string> setEditionAction)
        {
            var joker = new JokerViewModel(
                importAction,
                exportAction,
                toggleEternalAction,
                toggleRentalAction,
                togglePerishableAction,
                editPerishTallyAction,
                editSellCostAction,
                setEditionAction)
            {
                CardNode = card,
                SlotIndex = slotIndex
            };

            PopulateJokerMetadata(card, joker);
            return joker;
        }

        private static void PopulateJokerMetadata(LuaNode cardNode, JokerViewModel joker)
        {
            if (cardNode == null || joker == null)
                return;

            var labelNode = cardNode.Children.FirstOrDefault(n => n.Key == "label");
            var abilityNode = cardNode.Children.FirstOrDefault(n => n.Key == "ability");
            var effectNode = abilityNode?.Children.FirstOrDefault(n => n.Key == "effect");
            var sortIdNode = cardNode.Children.FirstOrDefault(n => n.Key == "sort_id");
            var rankNode = cardNode.Children.FirstOrDefault(n => n.Key == "rank");
            var saveFieldsNode = cardNode.Children.FirstOrDefault(n => n.Key == "save_fields");
            var centerNode = saveFieldsNode?.Children.FirstOrDefault(n => n.Key == "center");
            var eternalNode = abilityNode?.Children.FirstOrDefault(n => n.Key == "eternal");
            var rentalNode = abilityNode?.Children.FirstOrDefault(n => n.Key == "rental");
            var perishableNode = abilityNode?.Children.FirstOrDefault(n => n.Key == "perishable");
            var perishTallyNode = abilityNode?.Children.FirstOrDefault(n => n.Key == "perish_tally");
            var sellCostNode = cardNode.Children.FirstOrDefault(n => n.Key == "sell_cost");
            var costNode = cardNode.Children.FirstOrDefault(n => n.Key == "cost");
            var baseCostNode = cardNode.Children.FirstOrDefault(n => n.Key == "base_cost");
            var extraCostNode = cardNode.Children.FirstOrDefault(n => n.Key == "extra_cost");

            // Values arrive straight from the Lua source, so strings are quoted.
            joker.Label = labelNode?.Value?.Trim('"') ?? "Unknown";
            joker.Effect = effectNode?.Value?.Trim('"') ?? string.Empty;
            joker.SortId = sortIdNode != null && int.TryParse(sortIdNode.Value, out int sid) ? sid : 0;
            joker.Rank = rankNode != null && int.TryParse(rankNode.Value, out int r) ? r : 0;
            joker.CenterKey = centerNode?.Value?.Trim('"') ?? string.Empty;
            joker.IsEternal = eternalNode != null && string.Equals(eternalNode.Value, "true", StringComparison.OrdinalIgnoreCase);
            joker.IsRental = rentalNode != null && string.Equals(rentalNode.Value, "true", StringComparison.OrdinalIgnoreCase);
            joker.IsPerishable = perishableNode != null && string.Equals(perishableNode.Value, "true", StringComparison.OrdinalIgnoreCase);
            joker.PerishTally = perishTallyNode != null && int.TryParse(perishTallyNode.Value, out int pt) ? pt : 0;
            joker.SellCost = sellCostNode != null && int.TryParse(sellCostNode.Value, out int sc) ? sc : 0;
            joker.Cost = costNode != null && int.TryParse(costNode.Value, out int c) ? c : 0;
            joker.BaseCost = baseCostNode != null && int.TryParse(baseCostNode.Value, out int bc) ? bc : 0;
            joker.ExtraCost = extraCostNode != null && int.TryParse(extraCostNode.Value, out int ec) ? ec : 0;
            joker.SetSelectedEditionSilently(GetEditionType(cardNode));

            ApplyCardPresentation(cardNode, joker);
        }

        /// <summary>
        /// Art, type label, accent and tooltip for any card that can sit in a
        /// card area: jokers, consumables, playing cards and vouchers.
        /// </summary>
        private static void ApplyCardPresentation(LuaNode cardNode, JokerViewModel joker)
        {
            var saveFields = cardNode.Children.FirstOrDefault(n => n.Key == "save_fields");
            var playingCardKey = saveFields?.Children.FirstOrDefault(n => n.Key == "card")?.Value?.Trim('"');
            var center = joker.CenterKey ?? string.Empty;
            var seal = cardNode.Children.FirstOrDefault(n => n.Key == "seal")?.Value?.Trim('"');

            joker.OverlayText = null;

            // Playing cards are identified by their card key; their "center" is
            // the enhancement (c_base when unenhanced).
            if (!string.IsNullOrEmpty(playingCardKey))
            {
                var layers = new List<ImageSource>();
                var baseLayer = CardSpriteService.GetPlayingCardBase(center);
                if (baseLayer != null)
                    layers.Add(baseLayer);
                var sealLayer = CardSpriteService.GetSealSprite(seal != null ? $"{seal} Seal" : null);
                if (sealLayer != null)
                    layers.Add(sealLayer);
                joker.SetSpriteLayers(layers);

                var suitChar = playingCardKey.Length > 0 ? playingCardKey[0] : '?';
                var rank = playingCardKey.Length > 2 ? playingCardKey.Substring(2) : "?";
                if (rank == "T") rank = "10";
                var glyph = suitChar switch { 'H' => "♥", 'D' => "♦", 'C' => "♣", 'S' => "♠", _ => "?" };

                joker.TypeLabel = "Playing Card";
                joker.Accent = PeekCardViewModel.SetAccent("Playing Card");
                joker.OverlayText = center == "m_stone" ? null : $"{rank}{glyph}";
                joker.OverlayForeground = PeekCardViewModel.SuitForeground(suitChar is 'H' or 'D');

                var body = new List<string>();
                if (BalatroItems.EnhancementsByKey.TryGetValue(center, out var enh))
                    body.Add($"{enh.Name}: {enh.Text}");
                if (seal != null) body.Add($"{seal} Seal");
                joker.CardTooltip = new PeekTooltipViewModel
                {
                    Title = BalatroItems.CardDisplayName(playingCardKey),
                    Subtitle = "Playing Card",
                    Body = body.Count > 0 ? string.Join("\n", body) : null
                };
                return;
            }

            if (BalatroItems.ConsumablesByKey.TryGetValue(center, out var consumable))
            {
                var set = BalatroItems.Tarots.Any(t => t.Key == center) ? "Tarot"
                    : BalatroItems.Planets.Any(p => p.Key == center) ? "Planet"
                    : "Spectral";
                var sprite = CardSpriteService.GetConsumableSprite(center);
                joker.SetSpriteLayers(sprite != null ? new[] { sprite } : Array.Empty<ImageSource>());
                joker.TypeLabel = set;
                joker.Accent = PeekCardViewModel.SetAccent(set);
                joker.CardTooltip = new PeekTooltipViewModel
                {
                    Title = consumable.Name,
                    Subtitle = set,
                    Body = consumable.Text
                };
                return;
            }

            if (center.StartsWith("v_", StringComparison.Ordinal))
            {
                var sprite = CardSpriteService.GetVoucherSprite(center);
                joker.SetSpriteLayers(sprite != null ? new[] { sprite } : Array.Empty<ImageSource>());
                joker.TypeLabel = "Voucher";
                joker.Accent = PeekCardViewModel.SetAccent("Voucher");
                BalatroItems.VouchersByKey.TryGetValue(center, out var voucher);
                joker.CardTooltip = new PeekTooltipViewModel
                {
                    Title = voucher?.Name ?? joker.Label,
                    Subtitle = "Voucher",
                    Body = voucher?.Text
                };
                return;
            }

            joker.SetSpriteLayers(JokerSpriteService.GetSpriteLayers(center));
            BalatroItems.JokersByKey.TryGetValue(center, out var def);
            var rarity = def?.Rarity ?? 1;
            joker.TypeLabel = PeekCardViewModel.RarityDisplayName(rarity);
            joker.Accent = PeekCardViewModel.RarityAccent(rarity);
            joker.CardTooltip = new PeekTooltipViewModel
            {
                Title = joker.Label,
                Subtitle = $"{joker.TypeLabel} Joker",
                Body = def?.Text
            };
        }
    }
}