using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;
using Balatron.Services;
using Microsoft.Win32;

namespace Balatron
{
    public partial class MainWindow : Window
    {
        private string _originalFilePath;
        private string _tempTextFilePath;

        public MainWindow()
        {
            InitializeComponent();
        }

        internal void RePopulateTextEditor()
        {
            var updatedText = File.ReadAllText(_tempTextFilePath, Encoding.ASCII);
            var rootNode = LuaParser.Parse(updatedText);
            var readableTableData = LuaSerializer.Serialize(rootNode, true);
            TextEditor.Text = readableTableData;
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JKR files (*.jkr)|*.jkr"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            _originalFilePath = openFileDialog.FileName;
            CreateBackup(_originalFilePath);

            // Deflate the compressed file to a temporary text file
            _tempTextFilePath = Path.Combine(Path.GetTempPath(), "save.txt");
            DeflateFile(_originalFilePath, _tempTextFilePath);
            RePopulateTextEditor();

            SaveButton.IsEnabled = true;
            DataViewerButton.IsEnabled = true;

            var editor = Views.EditorView.Instance;
            editor.Show();
            editor.Activate();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Compress the temporary file to create the new save file.
            var newSavePath = Path.Combine(Path.GetDirectoryName(_originalFilePath), "newsave.jkr");
            CompressFile(_tempTextFilePath, newSavePath);

            // Replace the original file with the new save file.
            File.Copy(newSavePath, _originalFilePath, true);
            MessageBox.Show("File saved successfully.");
        }

        private void DataViewerButton_Click(object sender, RoutedEventArgs e)
        {
            var editor = Views.EditorView.Instance;
            editor.Show();
            editor.Activate();
        }

        private void CreateBackup(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var backupFileName = $"save_backup_{timestamp}.jkr";
            var backupPath = Path.Combine(directory, backupFileName);
            File.Copy(filePath, backupPath);
        }

        private static void DeflateFile(string inputPath, string outputTextFile)
        {
            using var inStream = File.OpenRead(inputPath);
            using var deflateStream = new DeflateStream(inStream, CompressionMode.Decompress);
            using var reader = new StreamReader(deflateStream, Encoding.ASCII);
            var text = reader.ReadToEnd();
            File.WriteAllText(outputTextFile, text, Encoding.ASCII);
        }

        private static void CompressFile(string inputTextFile, string outputCompressedFile)
        {
            var text = File.ReadAllText(inputTextFile, Encoding.ASCII);
            using var outStream = File.Create(outputCompressedFile);
            using var compressStream = new DeflateStream(outStream, CompressionLevel.Fastest);
            using var writer = new StreamWriter(compressStream, Encoding.ASCII);
            writer.Write(text);
        }
    }
}
