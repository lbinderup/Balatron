using System.ComponentModel;

namespace Balatron.Models
{
    public class BaseData : INotifyPropertyChanged
    {
        private int _nominal;
        public int Nominal { get => _nominal; set { _nominal = value; OnPropertyChanged(nameof(Nominal)); } }
        
        private double _suitNominal;
        public double SuitNominal { get => _suitNominal; set { _suitNominal = value; OnPropertyChanged(nameof(SuitNominal)); } }
        
        private double _faceNominal;
        public double FaceNominal { get => _faceNominal; set { _faceNominal = value; OnPropertyChanged(nameof(FaceNominal)); } }
        
        private int _timesPlayed;
        public int TimesPlayed { get => _timesPlayed; set { _timesPlayed = value; OnPropertyChanged(nameof(TimesPlayed)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}