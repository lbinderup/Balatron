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

        public JokerViewModel(Action<JokerViewModel> importAction = null, Action<JokerViewModel> exportAction = null)
        {
            ImportAction = importAction;
            ExportAction = exportAction;
            ExchangeCommand = new RelayCommand(_ => ImportAction?.Invoke(this), _ => ImportAction != null);
            ExportCommand = new RelayCommand(_ => ExportAction?.Invoke(this), _ => ExportAction != null);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}