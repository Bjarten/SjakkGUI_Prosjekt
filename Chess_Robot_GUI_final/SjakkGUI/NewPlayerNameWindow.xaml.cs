using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Chess_Robot
{
    /// <summary>
    /// Interaction logic for NewPlayerName.xaml
    /// </summary>
    public partial class NewPlayerNameWindow : Window
    {
        public NewPlayerNameWindow()
        {
            InitializeComponent();
            tbNewPlayer.Focus();
            
        }

        private void btnNewPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (tbNewPlayer.Text == string.Empty)
            {
            }
            else
            {
                ((MainWindow)Application.Current.MainWindow).UpdateName(tbNewPlayer.Text);
                this.Close();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (tbNewPlayer.Text == string.Empty)
                {
                }
                else
                {
                    ((MainWindow)Application.Current.MainWindow).UpdateName(tbNewPlayer.Text);
                    this.Close();
                }
            }
        }
    }
}
