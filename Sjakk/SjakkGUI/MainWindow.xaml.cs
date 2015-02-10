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
using System.Media;

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
        EventWaitHandle ewhChessAndRobotWork;
        EventWaitHandle ewhSendCommandToRobot;
        UCI myChessEngine;
        string enginePath = @"stockfish_14053109_32bit.exe";
        Thread threadChessAndRobotWork;
        Thread threadRobot;

        static string sysID = string.Empty;
        static NetworkScanner scanner;
        static Controller controller;
        static Mastership master;

        public MainWindow()
        {
            InitializeComponent();

            CreateController();

            ewhChessAndRobotWork = new EventWaitHandle(false, EventResetMode.AutoReset);
            threadChessAndRobotWork = new Thread(chessAndRobotWork);
            threadChessAndRobotWork.IsBackground = true;
            threadChessAndRobotWork.Start();


            // Not to be used for the moment/////////////////////////////////////////////
            //ewhSendCommandToRobot = new EventWaitHandle(false, EventResetMode.AutoReset);
            //threadRobot = new Thread(SendCommandToRobot);
            //threadRobot.IsBackground = true;
            //threadRobot.Start();
            /////////////////////////////////////////////////////////////////////////////

            myChessEngine = new UCI();
            myChessEngine.InitEngine(enginePath, "");
            myChessEngine.Depth = "10";

            lblMode.Content = "";

            // Event triggerd when operating mode changes
            controller.OperatingModeChanged += controller_OperatingModeChanged;

            Brush firstBrush = Brushes.White;
            Brush secondBrush = Brushes.Pink;


            ColorChessboard(firstBrush, secondBrush);

            //Image br = new Image();
            //ImageSource brImage = new BitmapImage(new Uri("Pictures/br.png"));
            //br.Source = "Pictures/br.png");

            //Grid.SetRow(br,0);
            //Grid.SetColumn(br,3);
            //gridChessboard.Children.Add(br);

            

        }

        private void ColorChessboard(Brush firstBrush, Brush secondBrush)
        {
            Border_1.Background = firstBrush;
            Border_2.Background = secondBrush;
            Border_3.Background = firstBrush;
            Border_4.Background = secondBrush;
            Border_5.Background = firstBrush;
            Border_6.Background = secondBrush;
            Border_7.Background = firstBrush;
            Border_8.Background = secondBrush;
            Border_9.Background =  secondBrush;
            Border_10.Background = firstBrush;
            Border_11.Background = secondBrush;
            Border_12.Background = firstBrush;
            Border_13.Background = secondBrush;
            Border_14.Background = firstBrush;
            Border_15.Background = secondBrush;
            Border_16.Background = firstBrush;
            Border_17.Background = firstBrush;
            Border_18.Background = secondBrush;
            Border_19.Background = firstBrush;
            Border_20.Background = secondBrush;
            Border_21.Background = firstBrush;
            Border_22.Background = secondBrush;
            Border_23.Background = firstBrush;
            Border_24.Background = secondBrush;
            Border_25.Background = secondBrush;
            Border_26.Background = firstBrush;
            Border_27.Background = secondBrush;
            Border_28.Background = firstBrush;
            Border_29.Background = secondBrush;
            Border_30.Background = firstBrush;
            Border_31.Background = secondBrush;
            Border_32.Background = firstBrush;
            Border_33.Background = firstBrush;
            Border_34.Background = secondBrush;
            Border_35.Background = firstBrush;
            Border_36.Background = secondBrush;
            Border_37.Background = firstBrush;
            Border_38.Background = secondBrush;
            Border_39.Background = firstBrush;
            Border_40.Background = secondBrush;
            Border_41.Background = secondBrush;
            Border_42.Background = firstBrush;
            Border_43.Background = secondBrush;
            Border_44.Background = firstBrush;
            Border_45.Background = secondBrush;
            Border_46.Background = firstBrush;
            Border_47.Background = secondBrush;
            Border_48.Background = firstBrush;
            Border_49.Background = firstBrush;
            Border_50.Background = secondBrush;
            Border_51.Background = firstBrush;
            Border_52.Background = secondBrush;
            Border_53.Background = firstBrush;
            Border_54.Background = secondBrush;
            Border_55.Background = firstBrush;
            Border_56.Background = secondBrush;
            Border_57.Background = secondBrush;
            Border_58.Background = firstBrush;
            Border_59.Background = secondBrush;
            Border_60.Background = firstBrush;
            Border_61.Background = secondBrush;
            Border_62.Background = firstBrush;
            Border_63.Background = secondBrush;
            Border_64.Background = firstBrush;
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
            ewhChessAndRobotWork.Set();
        }


        private void chessAndRobotWork(object obj)
        {
            //declare a variable of data type RapidDomain.Bool
            ABB.Robotics.Controllers.RapidDomain.Num rapidNumxCord1;
            ABB.Robotics.Controllers.RapidDomain.Num rapidNumxCord2;
            ABB.Robotics.Controllers.RapidDomain.Num rapidNumyCord1;
            ABB.Robotics.Controllers.RapidDomain.Num rapidNumyCord2;
            ABB.Robotics.Controllers.RapidDomain.Bool rapidBoolcapturePiece;
            ABB.Robotics.Controllers.RapidDomain.Bool rapidBoolcheckMate;
            ABB.Robotics.Controllers.RapidDomain.Bool rapidBoolwaitForTurn;

            //Make a variable that is connected to the variable in the robotcontroller 
            ABB.Robotics.Controllers.RapidDomain.RapidData rdxCoord1 = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "xCoord1");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdyCoord1 = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "yCoord1");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdxCoord2 = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "xCoord2");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdyCoord2 = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "yCoord2");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdcapturePiece = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "capturePiece");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdcheckMate = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "checkMate");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdwaitForTurn = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "waitForTurn");

            rdwaitForTurn.ValueChanged += rdwaitForTurn_ValueChanged;
  
            while (true)
            {
                ewhChessAndRobotWork.WaitOne();

                this.Dispatcher.Invoke((Action)(() =>
                {
                    myChessEngine.EngineCommandMove(tbNextMove.Text);
                }));

                // Wait for calculations to finish
                myChessEngine.ewhCalculating.WaitOne();

                this.Dispatcher.Invoke((Action)(() =>
                {
                    tbConsole.Clear();
                    tbConsole.Text = "Tidligere trekk: " + myChessEngine.EarlierMoves + "\n" + "Beste trekk: " + myChessEngine.BestMove;
                    tbNextMove.Text = myChessEngine.BestMove;
                }));

                int x1;
                int x2;
                int y1;
                int y2;
                bool takePiece;
                int[,] positionInt;
                char[,] positionChar;
                string castling;

                // Coordinates to be sent to robotcontroller
                myChessEngine.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2,out takePiece ,out positionInt,out positionChar, out castling);

                // DEBUG
                writeChessboardToTextboxInt(positionInt);
                writeChessboardToTextboxChar(positionChar, takePiece, castling);

                //// Read the current value from the robotcontroller
                rapidBoolcapturePiece = (ABB.Robotics.Controllers.RapidDomain.Bool)rdcapturePiece.Value;
                rapidBoolcheckMate = (ABB.Robotics.Controllers.RapidDomain.Bool)rdcheckMate.Value;
                rapidBoolwaitForTurn = (ABB.Robotics.Controllers.RapidDomain.Bool)rdwaitForTurn.Value;
                rapidNumxCord1 = (ABB.Robotics.Controllers.RapidDomain.Num)rdxCoord1.Value;
                rapidNumyCord1 = (ABB.Robotics.Controllers.RapidDomain.Num)rdyCoord1.Value;
                rapidNumxCord2 = (ABB.Robotics.Controllers.RapidDomain.Num)rdxCoord2.Value;
                rapidNumyCord2 = (ABB.Robotics.Controllers.RapidDomain.Num)rdyCoord2.Value;

                //rapidBool = (ABB.Robotics.Controllers.RapidDomain.Bool)rd.Value;
                //rapidNum = (ABB.Robotics.Controllers.RapidDomain.Num)x_1.Value;

                ////assign the value of the RAPID data to a local variable
                //bool boolValue = rapidBool.Value;
                //int numValue = (int)rapidNum.Value;

                // New values to be written to the robotcontroller
                rapidBoolcapturePiece.Value = takePiece;
                rapidBoolcheckMate.Value = false;
                rapidBoolwaitForTurn.Value = false;
                rapidNumxCord1.Value = x1;
                rapidNumyCord1.Value = y1;
                rapidNumxCord2.Value = x2;
                rapidNumyCord2.Value = y2;
                
                //Request mastership of Rapid before writing to the controller
                master = Mastership.Request(controller.Rapid);
                //Change: controller is repaced by aController
                rdxCoord1.Value = rapidNumxCord1;
                rdyCoord1.Value = rapidNumyCord1;
                rdxCoord2.Value = rapidNumxCord2;
                rdyCoord2.Value = rapidNumyCord2;
                rdcapturePiece.Value = rapidBoolcapturePiece;
                rdcheckMate.Value = rapidBoolcheckMate;
                rdwaitForTurn.Value = rapidBoolwaitForTurn; 
                //Release mastership as soon as possible
                master.ReleaseOnDispose = true;
                master.Dispose();
            }
        }

        private void rdwaitForTurn_ValueChanged(object sender, DataValueChangedEventArgs e)
        {
            ABB.Robotics.Controllers.RapidDomain.Bool rapidBoolwaitForTurn;
            RapidData rdwaitForTurn = (RapidData)sender;
            rapidBoolwaitForTurn = (ABB.Robotics.Controllers.RapidDomain.Bool)rdwaitForTurn.Value;
            bool variabel = rapidBoolwaitForTurn.Value;

            if (variabel)
            ewhChessAndRobotWork.Set();
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

        /*
        private void SendCommandToRobot(object obj)
        {

            //declare a variable of data type RapidDomain.Bool
            ABB.Robotics.Controllers.RapidDomain.Bool rapidBool;
            ABB.Robotics.Controllers.RapidDomain.Num rapidNum;

            //Make a variable that is connected to the variable in the robotcontroller 
            ABB.Robotics.Controllers.RapidDomain.RapidData rd = controller.Rapid.GetRapidData("T_ROB1", "stableSnus", "flag");
            ABB.Robotics.Controllers.RapidDomain.RapidData x_1 = controller.Rapid.GetRapidData("T_ROB1", "stableSnus", "x_1");


            while (true)
            {
                // Wait for new koordinates for robot
                ewhSendCommandToRobot.WaitOne();

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
        */

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


