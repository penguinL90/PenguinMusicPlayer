using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MusicApp
{
    /// <summary>
    /// VersionWindows.xaml 的互動邏輯
    /// </summary>
    public partial class VersionWindows : Window
    {
        public VersionWindows()
        {
            InitializeComponent();
            Assembly assembly = Assembly.GetExecutingAssembly();
            Version.Text = "Version: " + assembly.GetName().Version;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
