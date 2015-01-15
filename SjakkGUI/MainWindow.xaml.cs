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
using System.Threading;
using System.Diagnostics;

namespace SjakkGUI
{
  
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        EventWaitHandle ewh;
        UCI minSjakkMotor;
        string motorSti = @"C:\Users\Bjarte\Documents\GitHub\Sjakk\stockfish_14053109_32bit.exe";
        Thread traad;

        public MainWindow()
        {
            ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
            traad = new Thread(sjakkBeregninger);
            traad.IsBackground = true;
            traad.Start();
            minSjakkMotor = new UCI();
            minSjakkMotor.InitEngine(motorSti,"");


            // start new game

            string bestMove = string.Empty;
            string considering = string.Empty;
            string trekk = string.Empty;

            InitializeComponent();
        }


        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            ewh.Set();
        }


        private void sjakkBeregninger(object obj)
        {
            while (true)
            {
                ewh.WaitOne();

                  this.Dispatcher.Invoke((Action)(() =>
                            {
                                minSjakkMotor.EngineCommand("position startpos moves " + tbConsole.Text);
                        }));

                minSjakkMotor.EngineCommand("go");

                minSjakkMotor.ewh.WaitOne();

                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        minSjakkMotor.Trekk = tbConsole.Text;

                        tbConsole.Text = minSjakkMotor.Trekk + " " + minSjakkMotor.BestMove;
                    }));

                    //   tbConsole.Text += "Beste trekk: " + minSjakkMotor.BestMove;

                    minSjakkMotor.Considering = string.Empty;
                    minSjakkMotor.Ferdig = false;
                
                //  tbConsol.Text = minSjakkMotor.GetEngineOutput();

            }
        }
    }
}


