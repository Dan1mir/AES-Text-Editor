using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;

namespace Text_Editor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow window = new MainWindow();
            if (e.Args.Length == 1 && File.Exists(e.Args[0]))
                window.OpenFile(e.Args[0]);
            window.Show();
        }
        public void ChangeTheme(Uri themeUri) 
        {
            ResourceDictionary theme = new ResourceDictionary() { Source = themeUri }; 
            Resources.MergedDictionaries.Clear(); 
            Resources.MergedDictionaries.Add(theme); 
        }
    }
}
