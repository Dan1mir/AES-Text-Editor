using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using System.Text.Json;
using Text_Editor.Data;
using Text_Editor.Data.Encrypt;
using System.Security.Cryptography;
using System.Text;

namespace Text_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _hasTextChanged = false;
        private string _fileName = "";
        private string _dialogFileTypes = "Text file (*.txt)|*.txt|All files|*.*|C# file (*.cs)|*.cs|C++ file (*.cpp)|*.cpp|" +
                "HTML file (*.html, *.htm)|*.html;*.htm|Java file (*.java)|*.java|Javascript file (*.js)|*.js|" +
                "Visual Basic file (*.vb)|*.vb|XML file (*.xml)|*.xml|PHP file (*.php)|*.php";
        private string _jsonFileType = "Json file (*.json)|*.json";

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                if (!Properties.Settings.Default.Started)
                {
                    GenerateAES();

                    Properties.Settings.Default.Started = true;
                    Properties.Settings.Default.Save();
                }
                CheckMods();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,"OH FUCK", MessageBoxButton.OK);
            }

        }

        private void generateNewKey_Click(object sender, RoutedEventArgs e)
        {
            GenerateAES();
        }

        private void GenerateAES()
        {
            var aes = new AES();
            var (key, iv) = aes.GenerateKeyIV();

            var encryptkey = Convert.FromBase64String(DeobfuscateString(Properties.Settings.Default.Key));
            var encryptiv = Convert.FromBase64String(DeobfuscateString(Properties.Settings.Default.IV));

            string json = $"{{\"Key\":\"{Convert.ToBase64String(key)}\",\"IV\":\"{Convert.ToBase64String(iv)}\"}}";

            byte[] encryptedJson = aes.Encrypt(json, encryptkey, encryptiv);

            string fileName = GenerateUniqueFileName("aesKeyInfo", ".json");

            File.WriteAllBytes(fileName, encryptedJson);

            setEncryptKey(fileName);
        }

        string GenerateUniqueFileName(string baseFileName, string extension)
        {
            string fileName = $"{baseFileName}{extension}";
            int fileIndex = 1;

            while (File.Exists(fileName))
            {
                fileName = $"{baseFileName}({fileIndex}){extension}";
                fileIndex++;
            }

            return fileName;
        }

        private void SaveBeforeClosing_Prompt()
        {
            if (_hasTextChanged)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("Do you want to save before closing?", "Closing", MessageBoxButton.YesNoCancel);

                switch (messageBoxResult)
                {
                    case MessageBoxResult.Yes:
                        SaveFile();
                        break;
                    case MessageBoxResult.No:
                        NewFile();
                        break;
                    default:
                        return;
                }
            }

            TxtBoxDoc.Clear();
            _hasTextChanged = false;
        }

        private void MenuNew_Click(object sender, RoutedEventArgs e)
        {
            SaveBeforeClosing_Prompt();
            NewFile();
        }

        private void NewFile()
        {
            Title = "AES Text Editor";
            _fileName = "";
            _hasTextChanged = false;
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            SaveBeforeClosing_Prompt();

            OpenFile();
        }

        private void DetectSyntaxAndChange()
        {
            string fileType;
            byte indexfileType;

            // Change syntax upon detecting file name
            switch (_fileName.Substring(_fileName.LastIndexOf('.') + 1))
            {
                case ("cs"):
                    fileType = "C#";
                    indexfileType = 1;
                    break;
                case ("cpp"):
                    fileType = "C++";
                    indexfileType = 2;
                    break;
                case ("html"):
                case ("htm"):
                    fileType = "HTML";
                    indexfileType = 3;
                    break;
                case ("java"):
                    fileType = "Java";
                    indexfileType = 4;
                    break;
                case ("js"):
                    fileType = "Javascript";
                    indexfileType = 5;
                    break;
                case ("php"):
                    fileType = "PHP";
                    indexfileType = 6;
                    break;
                case ("vb"):
                    fileType = "Visual Basic";
                    indexfileType = 7;
                    break;
                case ("xml"):
                    fileType = "XML";
                    indexfileType = 8;
                    break;
                default:
                    fileType = "Text";
                    indexfileType = 0;
                    break;
            }

            ChangeSyntax(fileType);
            syntaxComboBox.SelectedIndex = indexfileType;
        }

        private void OpenFile()
        {
            OpenFileDialog openDlg = new OpenFileDialog
            {
                Filter = _dialogFileTypes,
                InitialDirectory = File.Exists(_fileName) ?
                    Path.GetDirectoryName(_fileName) :
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (openDlg.ShowDialog() == true)
            {
                OpenFile(openDlg.FileName); 
            }
        }

        public void OpenFile(string filePath)
        {
            try
            {
                var aes = new AES();

                var encryptkey = Convert.FromBase64String(DeobfuscateString(Properties.Settings.Default.Key));
                var encryptiv = Convert.FromBase64String(DeobfuscateString(Properties.Settings.Default.IV));

                var encryptedJson = File.ReadAllBytes(Properties.Settings.Default.aesEncryptPath);
                var decryptedJson = aes.Decrypt(encryptedJson, encryptkey, encryptiv);
                var config = JsonSerializer.Deserialize<DecryptKeys>(decryptedJson);

                var text = Convert.FromBase64String(File.ReadAllText(filePath));
                var decText = aes.Decrypt(text, config.Key, config.IV);

                TxtBoxDoc.Text = decText;
                _fileName = filePath;
                Title = "AES Text Editor - " + Path.GetFileName(_fileName);
                DetectSyntaxAndChange();
                _hasTextChanged = false;
            }
            catch (CryptographicException)
            {
                MessageBox.Show("Wrong AES key, decrypt failed.", "AES Key Error", MessageBoxButton.OK);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error just occurred: " + ex.Message, "Open File Error", MessageBoxButton.OK);
                return;
            }
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFile(true);
        }

        private void SaveFile(bool saveAs = false)
        {
            var aes = new AES();

            var encryptkey = Convert.FromBase64String(DeobfuscateString(Properties.Settings.Default.Key));
            var encryptiv = Convert.FromBase64String(DeobfuscateString(Properties.Settings.Default.IV));

            var encryptedJson = File.ReadAllBytes(Properties.Settings.Default.aesEncryptPath);
            var decryptedJson = aes.Decrypt(encryptedJson, encryptkey, encryptiv);
            var config = JsonSerializer.Deserialize<DecryptKeys>(decryptedJson);

            var encText = aes.Encrypt(TxtBoxDoc.Text, config.Key, config.IV);

            if (File.Exists(_fileName) && !saveAs)
            {
                File.WriteAllText(_fileName, Convert.ToBase64String(encText));
                FileSaved();
                return;
            }

            SaveFileDialog saveDlg = ReturnSaveDialog();

            if (saveDlg.ShowDialog() == true)
            {
                File.WriteAllText(saveDlg.FileName, Convert.ToBase64String(encText));
                _fileName = saveDlg.FileName;
                Title = "AES Text Editor - " + _fileName.Substring(_fileName.LastIndexOf('\\') + 1);
                FileSaved();
                DetectSyntaxAndChange();
            }
        }

        private SaveFileDialog ReturnSaveDialog()
        {
            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = _dialogFileTypes,

                InitialDirectory = File.Exists(_fileName) ?
                    _fileName.Remove(_fileName.LastIndexOf('\\')) :
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                DefaultExt = "txt",
                AddExtension = true,
                FileName = _fileName.LastIndexOf('\\') != -1 ? _fileName.Substring(_fileName.LastIndexOf('\\') + 1) : _fileName
            };
            return saveDlg;
        }

        private void TxtBoxDoc_TextChanged(object sender, EventArgs e)
        {
            _hasTextChanged = true;
            if (Title[Title.Length - 1] != '*')
                Title += '*';
        }

        private void FileSaved()
        {
            if (Title.EndsWith("*"))
            {
                Title = Title.TrimEnd('*');
                _hasTextChanged = false;
            }
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Show();
        }

        private void ComboFontSize_DropDownClosed(object sender, EventArgs e)
        {
            ChangeFontSize();
        }

        private void ComboFontSize_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ChangeFontSize();
        }

        private void ChangeFontSize()
        {
            if (comboFontSize.Text != null)
            {
                TxtBoxDoc.FontSize = double.Parse(comboFontSize.Text);
            }
        }

        private void SyntaxComboBox_OnDropDownClosed(object sender, EventArgs e)
        {
            if (sender is ComboBox comboBox)
                ChangeSyntax(comboBox.Text);
        }

        private void ChangeSyntax(string syntax)
        {
            var typeConverter = new HighlightingDefinitionTypeConverter();
            var syntaxHighlighter = (IHighlightingDefinition)typeConverter.ConvertFrom(syntax);
            TxtBoxDoc.SyntaxHighlighting = syntaxHighlighter;
        }

        private void MenuLineNumbers_OnClick(object sender, RoutedEventArgs e)
        {
            TxtBoxDoc.ShowLineNumbers = !Properties.Settings.Default.LineNumbers;
            Properties.Settings.Default.LineNumbers = !Properties.Settings.Default.LineNumbers;
            menuLineNumbers.IsChecked = Properties.Settings.Default.LineNumbers;

            Properties.Settings.Default.Save();
        }
        private void MenuNightMode_OnClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.NightMode = !Properties.Settings.Default.NightMode;
            menuNightMode.IsChecked = Properties.Settings.Default.NightMode;

            ChangeTheme(Properties.Settings.Default.NightMode);

            Properties.Settings.Default.Save();
        }

        private void ChangeTheme(bool isNightMode)
        {
            var themeUri = isNightMode ? new Uri("Data\\Themes\\DarkTheme.xaml", UriKind.Relative) 
                : new Uri("Data\\Themes\\LightTheme.xaml", UriKind.Relative);
            ((App)Application.Current).ChangeTheme(themeUri);
        }

        private void CheckMods()
        {
            menuLineNumbers.IsChecked = Properties.Settings.Default.LineNumbers;
            menuNightMode.IsChecked = Properties.Settings.Default.NightMode;

            TxtBoxDoc.ShowLineNumbers = Properties.Settings.Default.LineNumbers;

            ChangeTheme(Properties.Settings.Default.NightMode);
        }
        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            SaveBeforeClosing_Prompt();

            if (_hasTextChanged)
                e.Cancel = true;

            Properties.Settings.Default.Save();
        }

        private void MenuTodayDate_OnClick(object sender, RoutedEventArgs e)
        {
            TxtBoxDoc.Text += DateTime.Now;
        }

        private void MenuFind_Click(object sender, RoutedEventArgs e)
        {
            SearchPanel.Install(TxtBoxDoc);
        }

        private void Replace_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FindReplaceDialog.ShowForReplace(TxtBoxDoc);
        }

        private void Replace_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Find_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FindReplaceDialog.ShowForFind(TxtBoxDoc);
        }

        private void Find_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void TxtBoxDoc_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string filePath = files[0];
                    string extension = Path.GetExtension(filePath).ToLower();

                    if (extension == ".txt")
                    {
                        OpenFile(filePath);
                    }
                }
            }
            else
            {
                Console.Beep();
            }
        }
        public static void SaveToFile(byte[] key, byte[] iv, string filePath)
        {
            var aesKeyInfo = new DecryptKeys { Key = key, IV = iv };
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonString = JsonSerializer.Serialize(aesKeyInfo, options);

            File.WriteAllText(filePath, jsonString);
        }
        private void setEncrytKeyPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDlg = new OpenFileDialog
            {
                Filter = _jsonFileType,
                InitialDirectory = File.Exists(_fileName) ?
                Path.GetDirectoryName(_fileName) :
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (openDlg.ShowDialog() == true)
            {
                setEncryptKey(openDlg.FileName);
            }
        }
        private void setEncryptKey(string path)
        {
            Properties.Settings.Default.aesEncryptPath = path;
            try
            {
                var aes = new AES();

                var encryptkey = Convert.FromBase64String(DeobfuscateString(Properties.Settings.Default.Key));
                var encryptiv = Convert.FromBase64String(DeobfuscateString(Properties.Settings.Default.IV));

                var encryptedJson = File.ReadAllBytes(Properties.Settings.Default.aesEncryptPath);
                var decryptedJson = aes.Decrypt(encryptedJson, encryptkey, encryptiv);
                Properties.Settings.Default.Save();
            }
            catch (Exception)
            {
                MessageBox.Show("File open error. Maybe it's not an AES key file?", "AES Key Error", MessageBoxButton.OK);
                Properties.Settings.Default.aesEncryptPath = "aesKeyInfo.json";
                Properties.Settings.Default.Save();
                return;
            }
        }
          
        static string ObfuscateString(string input)
        {
            StringBuilder obfuscated = new StringBuilder();
            foreach (char c in input)
            {
                obfuscated.Append((char)(c ^ 0x55));
            }
            return obfuscated.ToString();
        }

        static string DeobfuscateString(string input)
        {
            return ObfuscateString(input);
        }

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }
            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                mainPanelBorder.Margin = new Thickness();
            }
        }
    }
}
