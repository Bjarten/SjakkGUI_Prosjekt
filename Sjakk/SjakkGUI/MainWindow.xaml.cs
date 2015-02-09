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

using ABB.Robotics;
using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.RapidDomain;

using System.Text.RegularExpressions;

namespace SjakkGUI
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        EventWaitHandle ewhChessCalculations;
        EventWaitHandle ewhSendCommandToRobot;
        UCI minSjakkMotor;
        string motorSti = @"stockfish_14053109_32bit.exe";
        Thread threadChessEngine;
        Thread threadRobot;

        static string sysID = string.Empty;
        static NetworkScanner scanner;
        static Controller controller;
        static Mastership master;

        public MainWindow()
        {
            InitializeComponent();

            ewhChessCalculations = new EventWaitHandle(false, EventResetMode.AutoReset);
            threadChessEngine = new Thread(chessCalculations);
            threadChessEngine.IsBackground = true;
            threadChessEngine.Start();

            ewhSendCommandToRobot = new EventWaitHandle(false, EventResetMode.AutoReset);
            threadRobot = new Thread(SendCommandToRobot);
            threadRobot.IsBackground = true;
            threadRobot.Start();

            minSjakkMotor = new UCI();
            minSjakkMotor.InitEngine(motorSti, "");
            minSjakkMotor.Depth = "20";

            lblMode.Content = "";

            CreateController();

            // Event triggerd when operating mode changes
            controller.OperatingModeChanged += controller_OperatingModeChanged;
        }


        private void CreateController()
        {
            scanner = new NetworkScanner();

            ControllerInfo[] controllers = scanner.GetControllers(NetworkScannerSearchCriterias.Virtual);
            controller = ControllerFactory.CreateFrom(controllers[0]);
            controller.Logon(UserInfo.DefaultUser);

            foreach (ControllerInfo controllerInfo in controllers)
            {
                this.lstControllerInformation.Items.Add(new ControllerInformationItems
                {
                    IPadress = controllerInfo.IPAddress.ToString(),
                    ID = controllerInfo.Id,
                    Availabillity = controllerInfo.Availability.ToString(),
                    Virtual = controllerInfo.IsVirtual.ToString(),
                    SystemName = controllerInfo.SystemName,
                    Version = controllerInfo.Version.ToString(),
                    ControllerName = controllerInfo.Name,
                    RobotWare = ""
                });
            }

            //sysID = controllers[0].SystemId.ToString();
            //controller = ControllerFactory.CreateFrom(controllers[0]);
            //controller.Logon(UserInfo.DefaultUser);
        }



        void controller_OperatingModeChanged(object sender, OperatingModeChangeEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                lblMode.Content = e.NewMode;
            }));
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            ewhChessCalculations.Set();
        }


        private void chessCalculations(object obj)
        {
            while (true)
            {
                ewhChessCalculations.WaitOne();

                this.Dispatcher.Invoke((Action)(() =>
                {
                    minSjakkMotor.EngineCommandMove(tbNextMove.Text);
                }));

                // Venter på kalkulasjoner
                minSjakkMotor.ewhCalculating.WaitOne();

                this.Dispatcher.Invoke((Action)(() =>
                {
                    tbConsole.Clear();
                    tbConsole.Text = "Tidligere trekk: " + minSjakkMotor.EarlierMoves + "\n" + "Beste trekk: " + minSjakkMotor.BestMove;
                    tbNextMove.Text = minSjakkMotor.BestMove;
                }));

                int x1;
                int x2;
                int y1;
                int y2;
                bool takePiece;
                int[,] positionInt;
                char[,] positionChar;
                string castling;

                // Koordinater som skal sendes til robotkontroller
                minSjakkMotor.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2,out takePiece ,out positionInt,out positionChar, out castling);

                // DEBUG
                writeChessboardToTextboxInt(positionInt);
                writeChessboardToTextboxChar(positionChar, takePiece, castling);
            }
        }

        private void writeChessboardToTextboxInt(int[,] position)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                tbConsole.Text += "\n";
            }));

            for (int i = position.GetLength(0) - 1; i >= 0; i--)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    tbConsole.Text += i + 1 + " ";
                }));

                for (int j = 0; j < position.GetLength(1); j++)
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        if (j == 0)
                            tbConsole.Text += " | " + position[i, j].ToString() + " | ";
                        else
                            tbConsole.Text += position[i, j].ToString() + " | ";
                    }));
                }
                this.Dispatcher.Invoke((Action)(() =>
                {
                    tbConsole.Text += "\n";
                }));
            }
            this.Dispatcher.Invoke((Action)(() =>
            {
                tbConsole.Text += "    | ";
            }));
            for (int k = 0; k <= 7; k++)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    tbConsole.Text += (char)(97 + k) + " | ";
                }));
            }
  
        }

        private void writeChessboardToTextboxChar(char[,] position, bool takePiece, string castling)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                tbConsole.Text += "\n\n";
            }));

            for (int i = position.GetLength(0) - 1; i >= 0; i--)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    tbConsole.Text += i + 1 + " ";
                }));

                for (int j = 0; j < position.GetLength(1); j++)
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        if (j == 0)
                        {
                            if (position[i, j] == ' ')
                                tbConsole.Text += "  |   " + position[i, j].ToString() + "   |  ";
                            else
                                tbConsole.Text += "  |  " + position[i, j].ToString() + "  |  ";

                        }
                        else
                        {
                            if (position[i, j] == ' ')
                            tbConsole.Text += position[i, j].ToString() + "   |  ";
                             else
                                tbConsole.Text += position[i, j].ToString() + "  |  ";
                        }
                    }));
                }
                this.Dispatcher.Invoke((Action)(() =>
                {
                    tbConsole.Text += "\n";
                }));
            }
            this.Dispatcher.Invoke((Action)(() =>
            {
                tbConsole.Text += "     |  ";
            }));
            for (int k = 0; k <= 7; k++)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    tbConsole.Text += (char)(97 + k) + "  |  ";
                }));
            }

            this.Dispatcher.Invoke((Action)(() =>
            {
                tbConsole.Text += "\n\nTake piece = " + takePiece;
            }));

                      this.Dispatcher.Invoke((Action)(() =>
            {
                tbConsole.Text += "\n\nCastling = " + castling;
            }));
        }

        private void SendCommandToRobot(object obj)
        {
            while (true)
            {
                // Wait for new koordinates for robot
                ewhSendCommandToRobot.WaitOne();

                //declare a variable of data type RapidDomain.Bool
                ABB.Robotics.Controllers.RapidDomain.Bool rapidBool;
                ABB.Robotics.Controllers.RapidDomain.Num rapidNum;

                //Make a variable that is connected to the variable in the robotcontroller 
                ABB.Robotics.Controllers.RapidDomain.RapidData rd = controller.Rapid.GetRapidData("T_ROB1", "stableSnus", "flag");
                ABB.Robotics.Controllers.RapidDomain.RapidData x_1 = controller.Rapid.GetRapidData("T_ROB1", "stableSnus", "x_1");

                //test that data type is correct before cast
                if (rd.Value is ABB.Robotics.Controllers.RapidDomain.Bool)
                {
                    // Read the current value from the robotcontroller
                    rapidBool = (ABB.Robotics.Controllers.RapidDomain.Bool)rd.Value;
                    rapidNum = (ABB.Robotics.Controllers.RapidDomain.Num)x_1.Value;

                    //assign the value of the RAPID data to a local variable
                    bool boolValue = rapidBool.Value;
                    int numValue = (int)rapidNum.Value;

                    // New values to be written to the robotcontroller
                    rapidBool.Value = true;
                    rapidNum.Value = 8;

                    //Request mastership of Rapid before writing to the controller
                    master = Mastership.Request(controller.Rapid);
                    //Change: controller is repaced by aController
                    rd.Value = rapidBool;
                    x_1.Value = rapidNum;
                    //Release mastership as soon as possible
                    master.ReleaseOnDispose = true;
                    master.Dispose();

                }
            }
        }


        public class ControllerInformationItems
        {
            public string IPadress { get; set; }
            public string ID { get; set; }
            public string Availabillity { get; set; }
            public string Virtual { get; set; }
            public string SystemName { get; set; }
            public string RobotWare { get; set; }
            public string Version { get; set; }
            public string ControllerName { get; set; }
        }

        private void lstControllerInformation_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {


        }

        private void btnStartRAPID_Click(object sender, RoutedEventArgs e)
        {
            ewhSendCommandToRobot.Set();
        }

    }
}


