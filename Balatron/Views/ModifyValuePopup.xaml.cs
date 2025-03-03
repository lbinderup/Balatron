using System.Windows;

namespace Balatron.Views
{
    public partial class ModifyValuePopup : Window
    {
        public string NewValue { get; private set; }

        public ModifyValuePopup(string address, string currentValue)
        {
            InitializeComponent();
            ValueTextBox.Text = currentValue;
            
            // Enable dragging
            MouseLeftButtonDown += (s, e) => DragMove();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            NewValue = ValueTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}