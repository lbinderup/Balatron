using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace Balatron
{
    public partial class MainWindow : Window
    {
        private string originalFilePath;
        private string tempTextFilePath;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JKR files (*.jkr)|*.jkr"
            };

            if (openFileDialog.ShowDialog() != true)
                return;
            
            originalFilePath = openFileDialog.FileName;
            CreateBackup(originalFilePath);

            // Use the same temporary file path as in EditorView
            tempTextFilePath = Path.Combine(Path.GetTempPath(), "save.txt");
            DeflateFile(originalFilePath, tempTextFilePath);

            // Load file contents into the main TextEditor
            TextEditor.Text = File.ReadAllText(tempTextFilePath, Encoding.ASCII);
            SaveButton.IsEnabled = true;
                
            // Open the EditorView. It will operate on the same temp file.
            var editor = new Balatron.Views.EditorView();
            editor.Show();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Option 1: Re-read the temporary file to get the latest modifications.
            var updatedText = File.ReadAllText(tempTextFilePath, Encoding.ASCII);
            TextEditor.Text = updatedText;
            
            // Compress the temporary file to create the new save file.
            var newSavePath = Path.Combine(Path.GetDirectoryName(originalFilePath), "newsave.jkr");
            CompressFile(tempTextFilePath, newSavePath);

            // Replace the original file with the new save file.
            File.Copy(newSavePath, originalFilePath, true);
            MessageBox.Show("File saved successfully.");
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
