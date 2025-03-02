using System.IO;
using System.Text;
using System.Windows;
using Balatron.Models;
using Balatron.Services;

namespace Balatron.Views
{
    public partial class EditorView : Window
    {
        private LuaNode _rootNode;
        private LuaNode _selectedNode;
        // Adjust this path as needed.
        private readonly string _tempFilePath = Path.Combine(Path.GetTempPath(), "save.txt");

        public EditorView()
        {
            InitializeComponent();
            LoadAndParseLuaFile();
        }

        private void LoadAndParseLuaFile()
        {
            if (File.Exists(_tempFilePath))
            {
                string luaText = File.ReadAllText(_tempFilePath, Encoding.ASCII);
                _rootNode = LuaParser.Parse(luaText);
                LuaTreeView.ItemsSource = _rootNode.Children;
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

        private static string GetAddress(LuaNode node)
        {
            var address = node.Key;
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
            if (_selectedNode is not { IsLeaf: true })
                return;
            
            var modifyWindow = new ModifyValueWindow(GetAddress(_selectedNode), _selectedNode.Value);
            if (modifyWindow.ShowDialog() != true)
                return;
            
            _selectedNode.Value = modifyWindow.NewValue;
            // Re-serialize the entire tree with the updated value.
            var newLuaText = LuaSerializer.Serialize(_rootNode);
            File.WriteAllText(_tempFilePath, newLuaText, Encoding.ASCII);
        }
    }
}
