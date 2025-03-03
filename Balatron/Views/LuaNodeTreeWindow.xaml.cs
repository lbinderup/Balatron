using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Balatron.Models;
using Balatron.Services;

namespace Balatron.Views
{
    public partial class LuaNodeTreeWindow : Window
    {
        private static LuaNodeTreeWindow _instance;
        public static LuaNodeTreeWindow Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LuaNodeTreeWindow();
                    _instance.Closed += (s, e) => _instance = null;
                }
                return _instance;
            }
        }

        private LuaNode _rootNode;
        private LuaNode _selectedNode;
        private readonly string _tempFilePath = Path.Combine(Path.GetTempPath(), "save.txt");

        private LuaNodeTreeWindow()
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
        
        private void LuaTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is not LuaNode { IsLeaf: true })
                return;

            ModifyValueButton_Click(sender, e);
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

            var modifyWindow = new ModifyValuePopup(GetAddress(_selectedNode), _selectedNode.Value);
    
            var mousePosition = Mouse.GetPosition(Application.Current.MainWindow);
            var windowPosition = Application.Current.MainWindow.PointToScreen(mousePosition);
    
            modifyWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            modifyWindow.Left = windowPosition.X;
            modifyWindow.Top = windowPosition.Y;
    
            if (modifyWindow.ShowDialog() != true)
                return;

            _selectedNode.Value = modifyWindow.NewValue;
            var newLuaText = LuaSerializer.Serialize(_rootNode);
            File.WriteAllText(_tempFilePath, newLuaText, Encoding.ASCII);

            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.RePopulateTextEditor();
            }
        }

        public string GetValueByAddress(string address)
        {
            if (_rootNode == null)
                return "";
            var parts = address.Split('.');
            var current = _rootNode;
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
            var current = _rootNode;
            foreach (var part in parts)
            {
                current = current.Children.FirstOrDefault(n => n.Key == part);
                if (current == null)
                    return;
            }
            current.Value = newValue;
            // Re-serialize the entire Lua tree and write to the temp file.
            var newLuaText = LuaSerializer.Serialize(_rootNode);
            File.WriteAllText(_tempFilePath, newLuaText, Encoding.ASCII);
        }
        
        public ObservableCollection<JokerViewModel> GetJokerViewModels()
        {
            var jokers = new ObservableCollection<JokerViewModel>();

            // Navigate to the node "cardAreas" -> "jokers" -> "cards"
            var cardAreas = _rootNode.Children.FirstOrDefault(n => n.Key == "cardAreas");
            if (cardAreas == null) return jokers;
            var jokersNode = cardAreas.Children.FirstOrDefault(n => n.Key == "jokers");
            if (jokersNode == null) return jokers;
            var cardsNode = jokersNode.Children.FirstOrDefault(n => n.Key == "cards");
            if (cardsNode == null) return jokers;

            // Iterate over the children of "cards" and convert each into a JokerViewModel.
            foreach (var card in cardsNode.Children)
            {
                var labelNode = card.Children.FirstOrDefault(n => n.Key == "label");
                var abilityNode = card.Children.FirstOrDefault(n => n.Key == "ability");
                var effectNode = abilityNode?.Children.FirstOrDefault(n => n.Key == "effect");
                var sortIdNode = card.Children.FirstOrDefault(n => n.Key == "sort_id");
                var rankNode = card.Children.FirstOrDefault(n => n.Key == "rank");

                var joker = new JokerViewModel
                {
                    Label = labelNode?.Value ?? "Unknown",
                    Effect = effectNode?.Value ?? "",
                    SortId = sortIdNode != null && int.TryParse(sortIdNode.Value, out int sid) ? sid : 0,
                    Rank = rankNode != null && int.TryParse(rankNode.Value, out int r) ? r : 0,
                };
                jokers.Add(joker);
            }
            return jokers;
        }
    }
}