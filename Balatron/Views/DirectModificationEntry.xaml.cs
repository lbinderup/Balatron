using System.Windows;
using System.Windows.Controls;

namespace Balatron.Views
{
    public partial class DirectModificationEntry : UserControl
    {
        private string OptionName { get; set; }
        private string KeyAddress { get; set; }

        public delegate string GetValueDelegate(string keyAddress);
        public delegate void SetValueDelegate(string keyAddress, string newValue);

        private GetValueDelegate GetValueFunc { get; set; }
        private SetValueDelegate SetValueFunc { get; set; }

        public DirectModificationEntry(string optionName, string keyAddress,
            GetValueDelegate getter, SetValueDelegate setter)
        {
            InitializeComponent();
            OptionName = optionName;
            KeyAddress = keyAddress;
            GetValueFunc = getter;
            SetValueFunc = setter;
            OptionNameTextBlock.Text = OptionName;
            RefreshValue();
        }

        private void RefreshValue()
        {
            if (GetValueFunc != null)
            {
                ValueTextBlock.Text = GetValueFunc(KeyAddress);
            }
        }

        private void ModifyButton_Click(object sender, RoutedEventArgs e)
        {
            var currentVal = GetValueFunc(KeyAddress);
            var modifyWindow = new ModifyValueWindow(KeyAddress, currentVal);
            if (modifyWindow.ShowDialog() != true)
                return;

            // Use the delegate to update the global data.
            SetValueFunc(KeyAddress, modifyWindow.NewValue);
            RefreshValue();
        }
    }
}