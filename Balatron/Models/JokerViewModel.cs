using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using Balatron.Services;

namespace Balatron.Models
{
    public class JokerViewModel : INotifyPropertyChanged
    {
        public int SortId { get; set; }
        public int Rank { get; set; }

        public int SlotIndex { get; set; }

        private bool _isEternal;
        public bool IsEternal
        {
            get => _isEternal;
            set
            {
                _isEternal = value;
                OnPropertyChanged(nameof(IsEternal));
                OnPropertyChanged(nameof(EternalToggleLabel));
                ComposeLayers();
            }
        }

        private bool _isRental;
        public bool IsRental
        {
            get => _isRental;
            set
            {
                _isRental = value;
                OnPropertyChanged(nameof(IsRental));
                OnPropertyChanged(nameof(RentalToggleLabel));
                ComposeLayers();
            }
        }

        private bool _isPerishable;
        public bool IsPerishable
        {
            get => _isPerishable;
            set
            {
                _isPerishable = value;
                OnPropertyChanged(nameof(IsPerishable));
                OnPropertyChanged(nameof(PerishableLabel));
                OnPropertyChanged(nameof(PerishableToggleLabel));
                ComposeLayers();
            }
        }

        private int _perishTally;
        public int PerishTally
        {
            get => _perishTally;
            set
            {
                _perishTally = value;
                OnPropertyChanged(nameof(PerishTally));
                OnPropertyChanged(nameof(PerishableLabel));
            }
        }

        private int _sellCost;
        public int SellCost
        {
            get => _sellCost;
            set
            {
                _sellCost = value;
                OnPropertyChanged(nameof(SellCost));
                OnPropertyChanged(nameof(SellCostLabel));
            }
        }

        private int _cost;
        public int Cost
        {
            get => _cost;
            set
            {
                _cost = value;
                OnPropertyChanged(nameof(Cost));
                OnPropertyChanged(nameof(CostLabel));
            }
        }

        private int _baseCost;
        public int BaseCost
        {
            get => _baseCost;
            set
            {
                _baseCost = value;
                OnPropertyChanged(nameof(BaseCost));
                OnPropertyChanged(nameof(BaseCostLabel));
            }
        }

        private int _extraCost;
        public int ExtraCost
        {
            get => _extraCost;
            set
            {
                _extraCost = value;
                OnPropertyChanged(nameof(ExtraCost));
                OnPropertyChanged(nameof(ExtraCostLabel));
            }
        }

        private string _label;
        public string Label
        {
            get => _label;
            set { _label = value; OnPropertyChanged(nameof(Label)); }
        }

        private string _centerKey;
        public string CenterKey
        {
            get => _centerKey;
            set { _centerKey = value; OnPropertyChanged(nameof(CenterKey)); }
        }

        private ObservableCollection<ImageSource> _spriteLayers = new();
        public ObservableCollection<ImageSource> SpriteLayers
        {
            get => _spriteLayers;
            set
            {
                _spriteLayers = value ?? new ObservableCollection<ImageSource>();
                OnPropertyChanged(nameof(SpriteLayers));
            }
        }

        private string _effect;
        public string Effect
        {
            get => _effect;
            set { _effect = value; OnPropertyChanged(nameof(Effect)); }
        }

        private LuaNode _cardNode;
        public LuaNode CardNode
        {
            get => _cardNode;
            set { _cardNode = value; OnPropertyChanged(nameof(CardNode)); }
        }

        public string SlotLabel => $"Slot {SlotIndex}";

        public ICommand ExchangeCommand { get; set; }
        public ICommand ExportCommand { get; set; }
        public ICommand ToggleEternalCommand { get; set; }
        public ICommand ToggleRentalCommand { get; set; }
        public ICommand TogglePerishableCommand { get; set; }
        public ICommand EditPerishTallyCommand { get; set; }
        public ICommand EditSellCostCommand { get; set; }
        public ICommand SetEditionCommand { get; set; }

        private Action<JokerViewModel> _importAction;
        public Action<JokerViewModel> ImportAction
        {
            get => _importAction;
            set
            {
                _importAction = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private Action<JokerViewModel> _exportAction;
        public Action<JokerViewModel> ExportAction
        {
            get => _exportAction;
            set
            {
                _exportAction = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private Action<JokerViewModel> _toggleEternalAction;
        public Action<JokerViewModel> ToggleEternalAction
        {
            get => _toggleEternalAction;
            set
            {
                _toggleEternalAction = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private Action<JokerViewModel> _toggleRentalAction;
        public Action<JokerViewModel> ToggleRentalAction
        {
            get => _toggleRentalAction;
            set
            {
                _toggleRentalAction = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private Action<JokerViewModel> _togglePerishableAction;
        public Action<JokerViewModel> TogglePerishableAction
        {
            get => _togglePerishableAction;
            set
            {
                _togglePerishableAction = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private Action<JokerViewModel> _editPerishTallyAction;
        public Action<JokerViewModel> EditPerishTallyAction
        {
            get => _editPerishTallyAction;
            set
            {
                _editPerishTallyAction = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private Action<JokerViewModel> _editSellCostAction;
        public Action<JokerViewModel> EditSellCostAction
        {
            get => _editSellCostAction;
            set
            {
                _editSellCostAction = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private Action<JokerViewModel, string> _setEditionAction;
        public Action<JokerViewModel, string> SetEditionAction
        {
            get => _setEditionAction;
            set
            {
                _setEditionAction = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string EternalToggleLabel => IsEternal ? "☑ Eternal" : "☐ Eternal";
        public string RentalToggleLabel => IsRental ? "☑ Rental" : "☐ Rental";
        public string PerishableLabel => IsPerishable ? $"Perishable ({PerishTally} turns)" : "Perishable: Off";
        public string PerishableToggleLabel => IsPerishable ? "☑ Perishable" : "☐ Perishable";
        public string SellCostLabel => $"Sell Cost: {SellCost}";
        public string CostLabel => $"Cost: {Cost}";
        public string BaseCostLabel => $"Base Cost: {BaseCost}";
        public string ExtraCostLabel => $"Extra Cost: {ExtraCost}";

        public string[] EditionOptions { get; } = { "None", "Negative", "Foil", "Holographic", "Polychrome" };

        private string _selectedEdition = "None";
        private bool _suppressEditionAction;
        public string SelectedEdition
        {
            get => _selectedEdition;
            set
            {
                if (_selectedEdition == value)
                    return;

                _selectedEdition = value;
                OnPropertyChanged(nameof(SelectedEdition));
                if (!_suppressEditionAction)
                {
                    SetEditionAction?.Invoke(this, value);
                }
            }
        }

        public void SetSelectedEditionSilently(string edition)
        {
            _suppressEditionAction = true;
            SelectedEdition = edition;
            _suppressEditionAction = false;
        }

        public JokerViewModel(
            Action<JokerViewModel> importAction = null,
            Action<JokerViewModel> exportAction = null,
            Action<JokerViewModel> toggleEternalAction = null,
            Action<JokerViewModel> toggleRentalAction = null,
            Action<JokerViewModel> togglePerishableAction = null,
            Action<JokerViewModel> editPerishTallyAction = null,
            Action<JokerViewModel> editSellCostAction = null,
            Action<JokerViewModel, string> setEditionAction = null)
        {
            ImportAction = importAction;
            ExportAction = exportAction;
            ToggleEternalAction = toggleEternalAction;
            ToggleRentalAction = toggleRentalAction;
            TogglePerishableAction = togglePerishableAction;
            EditPerishTallyAction = editPerishTallyAction;
            EditSellCostAction = editSellCostAction;
            SetEditionAction = setEditionAction;
            ExchangeCommand = new RelayCommand(_ => ImportAction?.Invoke(this), _ => ImportAction != null);
            ExportCommand = new RelayCommand(_ => ExportAction?.Invoke(this), _ => ExportAction != null);
            ToggleEternalCommand = new RelayCommand(_ => ToggleEternalAction?.Invoke(this), _ => ToggleEternalAction != null);
            ToggleRentalCommand = new RelayCommand(_ => ToggleRentalAction?.Invoke(this), _ => ToggleRentalAction != null);
            TogglePerishableCommand = new RelayCommand(_ => TogglePerishableAction?.Invoke(this), _ => TogglePerishableAction != null);
            EditPerishTallyCommand = new RelayCommand(_ => EditPerishTallyAction?.Invoke(this), _ => EditPerishTallyAction != null);
            EditSellCostCommand = new RelayCommand(_ => EditSellCostAction?.Invoke(this), _ => EditSellCostAction != null);
            SetEditionCommand = new RelayCommand(param => SetEditionAction?.Invoke(this, param as string), _ => SetEditionAction != null);
        }

        private IReadOnlyList<ImageSource> _baseLayers = Array.Empty<ImageSource>();

        public void SetSpriteLayers(IEnumerable<ImageSource> layers)
        {
            _baseLayers = layers?.ToList() ?? (IReadOnlyList<ImageSource>)Array.Empty<ImageSource>();
            ComposeLayers();
        }

        /// <summary>Card art = base joker sprite + the sticker sprites for the active flags.</summary>
        private void ComposeLayers()
        {
            SpriteLayers.Clear();
            foreach (var layer in _baseLayers)
                SpriteLayers.Add(layer);
            foreach (var sticker in CardSpriteService.GetStickerSprites(IsEternal, IsPerishable, IsRental))
                SpriteLayers.Add(sticker);
        }

        /// <summary>Rarity for jokers, otherwise the card type ("Tarot", "Playing Card", …).</summary>
        private string _typeLabel;
        public string TypeLabel
        {
            get => _typeLabel;
            set { _typeLabel = value; OnPropertyChanged(nameof(TypeLabel)); }
        }

        private string _overlayText;
        public string OverlayText
        {
            get => _overlayText;
            set { _overlayText = value; OnPropertyChanged(nameof(OverlayText)); }
        }

        private Brush _overlayForeground;
        public Brush OverlayForeground
        {
            get => _overlayForeground;
            set { _overlayForeground = value; OnPropertyChanged(nameof(OverlayForeground)); }
        }

        private Brush _accent;
        public Brush Accent
        {
            get => _accent;
            set { _accent = value; OnPropertyChanged(nameof(Accent)); }
        }

        private object _cardTooltip;
        public object CardTooltip
        {
            get => _cardTooltip;
            set { _cardTooltip = value; OnPropertyChanged(nameof(CardTooltip)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}