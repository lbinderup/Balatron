using System.Windows;

namespace Balatron.Views
{
    public partial class ModifyValueWindow : Window
    {
        public string NewValue { get; private set; }

        public ModifyValueWindow(string address, string currentValue)
        {
            InitializeComponent();
            AddressLabel.Text = "Address: " + address;
            ValueTextBox.Text = currentValue;
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