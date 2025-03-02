using System.IO;
using System.Text;
using System.Windows;
using Balatron.Models;
using Balatron.Services;

namespace Balatron.Views
{
    public partial class EditorView : Window
    {
        private LuaNode _selectedNode;

        public EditorView()
        {
            InitializeComponent();
            LoadAndParseLuaFile();
        }

        private void LoadAndParseLuaFile()
        {
            // Replace with your actual temporary file path.
            string tempFilePath = Path.Combine(Path.GetTempPath(), "save.txt");

            if (File.Exists(tempFilePath))
            {
                string luaText = File.ReadAllText(tempFilePath, Encoding.ASCII);
                LuaNode rootNode = LuaParser.Parse(luaText);
                LuaTreeView.ItemsSource = rootNode.Children;
            }
            else
            {
                MessageBox.Show("Temporary save file not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LuaTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _selectedNode = e.NewValue as LuaNode;
            if (_selectedNode != null && _selectedNode.IsLeaf)
            {
                AddressTextBox.Text = GetAddress(_selectedNode);
                ModifyValueButton.IsEnabled = true;
            }
            else
            {
                AddressTextBox.Text = "";
                ModifyValueButton.IsEnabled = false;
            }
        }

        private string GetAddress(LuaNode node)
        {
            string address = node.Key;
            var current = node.Parent;
            while (current != null && current.Key != "Root")
            {
                address = current.Key + "." + address;
                current = current.Parent;
            }
            return address;
        }

        private void ModifyValueButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode != null && _selectedNode.IsLeaf)
            {
                var modifyWindow = new ModifyValueWindow(GetAddress(_selectedNode), _selectedNode.Value);
                if (modifyWindow.ShowDialog() == true)
                {
                    _selectedNode.Value = modifyWindow.NewValue;
                }
            }
        }
    }
}
