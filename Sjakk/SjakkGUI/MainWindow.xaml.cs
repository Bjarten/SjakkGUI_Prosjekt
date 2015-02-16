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
            myChessEngine.Depth = "4";

            lblMode.Content = "";

            // Event triggerd when operating mode changes
            controller.OperatingModeChanged += controller_OperatingModeChanged;

            Brush firstBrush = Brushes.LightGray;
            Brush secondBrush = Brushes.Gray;


            ColorChessboard(firstBrush, secondBrush);
      
        }

        private void ColorChessboard(Brush firstBrush, Brush secondBrush)
        {
            Border_a8.Background = firstBrush;
            Border_b8.Background = secondBrush;
            Border_c8.Background = firstBrush;
            Border_d8.Background = secondBrush;
            Border_e8.Background = firstBrush;
            Border_f8.Background = secondBrush;
            Border_g8.Background = firstBrush;
            Border_h8.Background = secondBrush;
            Border_a7.Background =  secondBrush;
            Border_b7.Background = firstBrush;
            Border_c7.Background = secondBrush;
            Border_d7.Background = firstBrush;
            Border_e7.Background = secondBrush;
            Border_f7.Background = firstBrush;
            Border_g7.Background = secondBrush;
            Border_h7.Background = firstBrush;
            Border_a6.Background = firstBrush;
            Border_b6.Background = secondBrush;
            Border_c6.Background = firstBrush;
            Border_d6.Background = secondBrush;
            Border_e6.Background = firstBrush;
            Border_f6.Background = secondBrush;
            Border_g6.Background = firstBrush;
            Border_h6.Background = secondBrush;
            Border_a5.Background = secondBrush;
            Border_b5.Background = firstBrush;
            Border_c5.Background = secondBrush;
            Border_d5.Background = firstBrush;
            Border_e5.Background = secondBrush;
            Border_f5.Background = firstBrush;
            Border_g5.Background = secondBrush;
            Border_h5.Background = firstBrush;
            Border_a4.Background = firstBrush;
            Border_b4.Background = secondBrush;
            Border_c4.Background = firstBrush;
            Border_d4.Background = secondBrush;
            Border_e4.Background = firstBrush;
            Border_f4.Background = secondBrush;
            Border_g4.Background = firstBrush;
            Border_h4.Background = secondBrush;
            Border_a3.Background = secondBrush;
            Border_b3.Background = firstBrush;
            Border_c3.Background = secondBrush;
            Border_d3.Background = firstBrush;
            Border_e3.Background = secondBrush;
            Border_f3.Background = firstBrush;
            Border_g3.Background = secondBrush;
            Border_h3.Background = firstBrush;
            Border_a2.Background = firstBrush;
            Border_b2.Background = secondBrush;
            Border_c2.Background = firstBrush;
            Border_d2.Background = secondBrush;
            Border_e2.Background = firstBrush;
            Border_f2.Background = secondBrush;
            Border_g2.Background = firstBrush;
            Border_h2.Background = secondBrush;
            Border_a1.Background = secondBrush;
            Border_b1.Background = firstBrush;
            Border_c1.Background = secondBrush;
            Border_d1.Background = firstBrush;
            Border_e1.Background = secondBrush;
            Border_f1.Background = firstBrush;
            Border_g1.Background = secondBrush;
            Border_h1.Background = firstBrush;
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

            Border[,] borders = new Border[8, 8] {{ Border_a1 , Border_b1 , Border_c1 , Border_d1 , Border_e1 , Border_f1 , Border_g1 , Border_h1 },
                                                  { Border_a2 , Border_b2 , Border_c2 , Border_d2 , Border_e2 , Border_f2 , Border_g2 , Border_h2 },
 				                                  { Border_a3 , Border_b3 , Border_c3 , Border_d3 , Border_e3 , Border_f3 , Border_g3 , Border_h3 },
                                                  { Border_a4 , Border_b4 , Border_c4 , Border_d4 , Border_e4 , Border_f4 , Border_g4 , Border_h4 },
                                                  { Border_a5 , Border_b5 , Border_c5 , Border_d5 , Border_e5 , Border_f5 , Border_g5 , Border_h5 },
                                                  { Border_a6 , Border_b6 , Border_c6 , Border_d6 , Border_e6 , Border_f6 , Border_g6 , Border_h6 },
                                                  { Border_a7 , Border_b7, Border_c7 , Border_d7 , Border_e7 , Border_f7 , Border_g7 , Border_h7, },
                                                  { Border_a8 , Border_b8, Border_c8 , Border_d8 , Border_e8 , Border_f8 , Border_g8 , Border_h8 }};

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
                    // Sender inn sjakktrekket frå tekstboks.
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
                int x3;
                int y1;
                int y2;
                int y3;
                bool takePiece;
                int[,] positionInt;
                char[,] positionChar;
                string castling;
                bool enPassant;
                // Coordinates to be sent to robotcontroller
                myChessEngine.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2, out x3,out y3,out takePiece ,out positionInt,out positionChar, out castling, out enPassant);

                // DEBUG
                writeChessboardToTextboxInt(positionInt);
                writeChessboardToTextboxChar(positionChar, takePiece, castling);

                // Write the current position to the chessboard graphic in the GUI
                ChessboardGraphic(borders, x1, x2, x3, y1, y2, y3, castling, enPassant);

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

        /// <summary>
        /// Updates the chessboard graphic in the GUI
        /// </summary>
        /// <param name="borders"></param>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <param name="castling"></param>
        private void ChessboardGraphic(Border[,] borders, int x1, int x2, int x3, int y1, int y2, int y3, string castling, bool enPasant)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                if (castling == "" && !enPasant)
                {
                    Image image = new Image();
                    // Delete the piece in the field its being moved to
                    borders[y2, x2].Child = null;
                    // Copy the piece that is being moved
                    image = (Image)borders[y1, x1].Child;
                    // delete the piece in old position
                    borders[y1, x1].Child = null;
                    // Paste piece in new position
                    borders[y2, x2].Child = image;
                }
                else if (castling == "WShort")
                {
                    Image image = new Image();
                    // Copy the image of the king 
                    image = (Image)borders[0, 4].Child;
                    // Delete the old picture
                    borders[0, 4].Child = null;
                    // Paste king in new position
                    borders[0, 6].Child = image;
                    // Copy picture of rook
                    image = (Image)borders[0, 7].Child;
                    // Delete old picture of rook
                    borders[0, 7].Child = null;
                    // Paste rook in new position
                    borders[0, 5].Child = image;
                }
                else if (castling == "WLong")
                {
                    Image image = new Image();
                    image = (Image)borders[0, 4].Child;
                    borders[0, 4].Child = null;
                    borders[0, 2].Child = image;
                    image = (Image)borders[0, 0].Child;
                    borders[0, 0].Child = null;
                    borders[0, 3].Child = image;
                }
                else if (castling == "BShort")
                {
                    Image image = new Image();
                    image = (Image)borders[7, 4].Child;
                    borders[7, 4].Child = null;
                    borders[7, 6].Child = image;
                    image = (Image)borders[7, 7].Child;
                    borders[7, 7].Child = null;
                    borders[7, 5].Child = image;
                }
                else if (castling == "BLong")
                {
                    Image image = new Image();
                    image = (Image)borders[7, 4].Child;
                    borders[7, 4].Child = null;
                    borders[7, 2].Child = image;
                    image = (Image)borders[7, 0].Child;
                    borders[7, 0].Child = null;
                    borders[7, 3].Child = image;
                }

                if (enPasant)
                {
                    Image image = new Image();
                    // Delete the piece in the field its being moved to
                    borders[y2, x2].Child = null;
                    // Copy the piece that is being moved
                    image = (Image)borders[y1, x1].Child;
                    // delete the piece in old position
                    borders[y1, x1].Child = null;
                    // Paste piece in new position
                    borders[y2, x2].Child = image;
                    // Delete piece en pasant
                    borders[y3, x3].Child = null;
                }
            }));
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


