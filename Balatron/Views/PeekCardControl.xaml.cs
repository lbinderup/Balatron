using System.Windows;
using System.Windows.Controls;
using Balatron.Effects;
using Balatron.Models;

namespace Balatron.Views
{
    public partial class PeekCardControl : UserControl
    {
        public PeekCardControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var vm = DataContext as PeekCardViewModel;
            var effect = EditionEffect.Create(vm?.Edition, vm?.Name);
            ArtGrid.Effect = effect;
            effect?.Start();
        }
    }
}
