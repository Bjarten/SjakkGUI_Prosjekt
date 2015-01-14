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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SjakkGUI
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        UCI minSjakkMotor;
        string motorSti = @"C:\Users\Bjarte\Documents\GitHub\Sjakk\stockfish_14053109_32bit.exe";

        public MainWindow()
        {
            minSjakkMotor = new UCI();
            minSjakkMotor.InitEngine(motorSti,"");
          

            InitializeComponent();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            minSjakkMotor.EngineCommand("go");
          //  tbConsol.Text = minSjakkMotor.GetEngineOutput();
        }


    }
}


