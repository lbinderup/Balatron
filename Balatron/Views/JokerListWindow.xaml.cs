using System.Collections.ObjectModel;
using System.Windows;
using Balatron.Models;

namespace Balatron.Views
{
    public partial class JokerListWindow : Window
    {
        public ObservableCollection<JokerViewModel> Jokers { get; set; }

        public JokerListWindow(ObservableCollection<JokerViewModel> jokers)
        {
            InitializeComponent();
            Jokers = jokers;
            DataContext = this;
        }
    }
}