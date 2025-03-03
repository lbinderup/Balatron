using System.ComponentModel;

namespace Balatron.Models
{
    public class AbilityData : INotifyPropertyChanged
    {
        private int _order;
        public int Order { get => _order; set { _order = value; OnPropertyChanged(nameof(Order)); } }
        
        private int _handsPlayedAtCreate;
        public int HandsPlayedAtCreate { get => _handsPlayedAtCreate; set { _handsPlayedAtCreate = value; OnPropertyChanged(nameof(HandsPlayedAtCreate)); } }
        
        private int _extraValue;
        public int ExtraValue { get => _extraValue; set { _extraValue = value; OnPropertyChanged(nameof(ExtraValue)); } }
        
        private string _effect;
        public string Effect { get => _effect; set { _effect = value; OnPropertyChanged(nameof(Effect)); } }
        
        private string _name;
        public string Name { get => _name; set { _name = value; OnPropertyChanged(nameof(Name)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}