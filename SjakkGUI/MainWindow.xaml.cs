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
            minSjakkMotor.Depth = "20";


            // start new game
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
                                minSjakkMotor.EngineCommandMove(tbNextMove.Text);
                        }));

                // Venter på kalkulasjoner
                minSjakkMotor.ewhCalculating.WaitOne();

                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        tbConsole.Clear();
                        tbConsole.Text = "Tidligere trekk: " + minSjakkMotor.EarlierMoves + "\n" + "Beste trekk: "+ minSjakkMotor.BestMove;
                    }));
            }
        }
    }
}


