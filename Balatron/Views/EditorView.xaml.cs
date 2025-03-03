// File: Views/EditorView.xaml.cs
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Balatron.Models;
using Balatron.Services;

namespace Balatron.Views
{
    public partial class EditorView : Window
    {
        private static EditorView _instance;
        public static EditorView Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EditorView();
                    _instance.Closed += (s, e) => _instance = null;
                }
                return _instance;
            }
        }

        private LuaNode _rootNode;
        private LuaNode _selectedNode;
        private readonly string _tempFilePath = Path.Combine(Path.GetTempPath(), "save.txt");

        private EditorView()
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
            var newLuaText = LuaSerializer.Serialize(_rootNode);
            File.WriteAllText(_tempFilePath, newLuaText, Encoding.ASCII);

            // Retrieve the MainWindow instance and refresh the text editor.
            if (Application.Current.MainWindow is Balatron.MainWindow mainWindow)
            {
                mainWindow.RePopulateTextEditor();
            }
        }

        public string GetValueByAddress(string address)
        {
            if (_rootNode == null)
                return "";
            var parts = address.Split('.');
            LuaNode current = _rootNode;
            foreach (var part in parts)
            {
                current = current.Children.FirstOrDefault(n => n.Key == part);
                if (current == null)
                    return "";
            }
            return current.Value;
        }

        public void SetValueByAddress(string address, string newValue)
        {
            if (_rootNode == null)
                return;
            var parts = address.Split('.');
            LuaNode current = _rootNode;
            foreach (var part in parts)
            {
                current = current.Children.FirstOrDefault(n => n.Key == part);
                if (current == null)
                    return;
            }
            current.Value = newValue;
            // Re-serialize the entire Lua tree and write to the temp file.
            string newLuaText = LuaSerializer.Serialize(_rootNode);
            File.WriteAllText(_tempFilePath, newLuaText, Encoding.ASCII);
        }
    }
}