using System;
using System.ComponentModel;
using System.Windows.Input;

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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}