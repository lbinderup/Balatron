using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Balatron.Models
{
    public class LuaNode : INotifyPropertyChanged
    {
        private string _value;
        public string Key { get; set; }
        public bool ForceQuotedKey { get; set; } = false;  // New flag
        public LuaNode Parent { get; set; }
        public ObservableCollection<LuaNode> Children { get; } = new ObservableCollection<LuaNode>();

        // Indicates that this node represents a Lua table.
        public bool IsTable { get; set; } = false;

        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public bool IsLeaf => Children.Count == 0 && !IsTable;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}