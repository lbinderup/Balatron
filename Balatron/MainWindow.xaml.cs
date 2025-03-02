// MainWindow.xaml.cs
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

            if (openFileDialog.ShowDialog() == true)
            {
                originalFilePath = openFileDialog.FileName;
                CreateBackup(originalFilePath);

                tempTextFilePath = Path.Combine(Path.GetTempPath(), "save.txt");
                DeflateFile(originalFilePath, tempTextFilePath);

                TextEditor.Text = File.ReadAllText(tempTextFilePath, Encoding.ASCII);
                SaveButton.IsEnabled = true;
                
                var editor = new Balatron.Views.EditorView();
                editor.Show();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllText(tempTextFilePath, TextEditor.Text, Encoding.ASCII);

            string newSavePath = Path.Combine(Path.GetDirectoryName(originalFilePath), "newsave.jkr");
            CompressFile(tempTextFilePath, newSavePath);

            File.Copy(newSavePath, originalFilePath, true);
            MessageBox.Show("File saved successfully.");
        }

        private void CreateBackup(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string backupFileName = $"save_backup_{timestamp}.jkr";
            string backupPath = Path.Combine(directory, backupFileName);
            File.Copy(filePath, backupPath);
        }

        private void DeflateFile(string inputPath, string outputTextFile)
        {
            using (FileStream inStream = File.OpenRead(inputPath))
            using (DeflateStream deflateStream = new DeflateStream(inStream, CompressionMode.Decompress))
            using (StreamReader reader = new StreamReader(deflateStream, Encoding.ASCII))
            {
                string text = reader.ReadToEnd();
                File.WriteAllText(outputTextFile, text, Encoding.ASCII);
            }
        }

        private void CompressFile(string inputTextFile, string outputCompressedFile)
        {
            string text = File.ReadAllText(inputTextFile, Encoding.ASCII);
            using (FileStream outStream = File.Create(outputCompressedFile))
            using (DeflateStream compressStream = new DeflateStream(outStream, CompressionLevel.Fastest))
            using (StreamWriter writer = new StreamWriter(compressStream, Encoding.ASCII))
            {
                writer.Write(text);
            }
        }
    }
}
