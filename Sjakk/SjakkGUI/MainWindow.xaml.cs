#define DEBUG

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

using System.Net;
using System.Net.Sockets;

using System.Text.RegularExpressions;

using System.Windows.Media.Animation;

using System.IO.Ports;


namespace SjakkGUI
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        EventWaitHandle ewhChessWork;
        EventWaitHandle ewhSendCommandToRobot;
        UCI myChessEngine;
        string enginePath = @"stockfish_14053109_32bit.exe";
        Thread threadChessWork;

        // For communicating with Scorpion Vision 
        SerialPort sPort;
        static string visionSystemInData = string.Empty;


        static string sysID = string.Empty;
        static NetworkScanner scanner;
        static Controller controller;
        static Mastership master;

        static Border oldBorderFrom = null;
        static Border oldBorderTo = null;

        static Brush oldToBrush = null;
        static Brush oldFromBrush = null;

        static Brush movedFromToBrush;

        static Storyboard myStoryboard;



        public MainWindow()
        {

                InitializeComponent();

                CreateController();

                // Setup for the COM port. Is used to communicate with Scorpion Vision System
                
#if (!DEBUG)
                sPort = new SerialPort("COM7", 9600, Parity.None, 8, StopBits.One);
                sPort.DataReceived += sPort_DataReceived;
                sPort.Open();
#endif
                ewhChessWork = new EventWaitHandle(false, EventResetMode.AutoReset);
                threadChessWork = new Thread(chessWork);
                threadChessWork.IsBackground = true;
                threadChessWork.Start();

                lblHumanRobotTurn.Content = "Human";

                myChessEngine = new UCI();
                myChessEngine.InitEngine(enginePath, "");
                myChessEngine.Depth = "4";

                lblMode.Content = "";

                // Event triggerd when operating mode changes
                controller.OperatingModeChanged += controller_OperatingModeChanged;

                // Set board style
                Brush firstBrush = Brushes.LightGray;
                Brush secondBrush = Brushes.Gray;
                movedFromToBrush = Brushes.LightPink;

                cbChessBoardStyleColorLeftSide.Color = Colors.LightGray;
                cbChessBoardStyleColorRightSide.Color = Colors.Gray;

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
            Border_a7.Background = secondBrush;
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
            ewhChessWork.Set();
        }


        private void chessWork(object obj)
        {
            // true if it's the robots turn
            bool robot = false;
            string move = string.Empty;

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
            ABB.Robotics.Controllers.RapidDomain.Num rapidNumxEnPassant;
            ABB.Robotics.Controllers.RapidDomain.Num rapidNumyEnPassant;
            ABB.Robotics.Controllers.RapidDomain.Bool rapidBoolEnPassantActive;
            ABB.Robotics.Controllers.RapidDomain.Bool rapidBoolcapturePiece;
            ABB.Robotics.Controllers.RapidDomain.Bool rapidBoolcheckMate;
            ABB.Robotics.Controllers.RapidDomain.Bool rapidBoolwaitForTurn;
            ABB.Robotics.Controllers.RapidDomain.String rapidStringCastlingState;

            //Make a variable that is connected to the variable in the robotcontroller 
            ABB.Robotics.Controllers.RapidDomain.RapidData rdxCoord1 = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "xCoord1");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdyCoord1 = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "yCoord1");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdxCoord2 = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "xCoord2");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdyCoord2 = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "yCoord2");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdxEnPassant = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "xEnPassant");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdyEnPassant = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "yEnPassant");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdEnPassantActive = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "EnPassantActive");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdcapturePiece = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "capturePiece");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdcheckMate = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "checkMate");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdwaitForTurn = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "waitForTurn");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdCastlingState = controller.Rapid.GetRapidData("T_ROB1", "SjakkTest", "CastlingState");

            // Event that fires when the waitForTurn variable changes value
            rdwaitForTurn.ValueChanged += rdwaitForTurn_ValueChanged;

            while (true)
            {
                ewhChessWork.WaitOne();

                if (robot)
                {
                    // The best move the robot can make
                    move = myChessEngine.BestMove;
                    // Sends in the move to the UCI object. Gets out coordinates to be sent to robotcontroller
                    myChessEngine.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2, out x3, out y3, out takePiece, out positionInt, out positionChar, out castling, out enPassant, move);


                    myChessEngine.EngineCommandMove(move);

                    // Wait for calculations to finish
                    myChessEngine.ewhCalculating.WaitOne();

                    double score = 0.5 + (Convert.ToDouble(myChessEngine.Score) * -1) / 2000; // max score 10 pawns
                    

                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        Debug.Write("Tidligere trekk: " + myChessEngine.EarlierMoves + "\n" + "Beste trekk: " + myChessEngine.BestMove + "\nStilling: " + myChessEngine.Score + " Score: " + score);
                        tbNextMove.Text = myChessEngine.BestMove;

                        lblHumanRobotTurn.Content = "Human";

                        ScoreAnimation(score);
               
                    }));
                    robot = false;
                }
                else
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        // Takes in the move from the human player
                        move = tbNextMove.Text;
                        myChessEngine.EngineCommandMove(move);
                    }));

                    // Wait for calculations to finish
                    myChessEngine.ewhCalculating.WaitOne();


                    // Lag metode av denne biten

                    // Get score
                    double score = 0.5 + Convert.ToDouble(myChessEngine.Score)/2000;

                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        Debug.Write("Tidligere trekk: " + myChessEngine.EarlierMoves + "\n" + "Beste trekk: " + myChessEngine.BestMove + "\nStilling: " + myChessEngine.Score + " Score: " + score);
                        //tbNextMove.Text = myChessEngine.BestMove;
                        tbNextMove.Clear();

                        ScoreAnimation(score);

                        lblHumanRobotTurn.Content = "Robot";
                    }));

                    // Coordinates to be sent to robotcontroller
                    myChessEngine.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2, out x3, out y3, out takePiece, out positionInt, out positionChar, out castling, out enPassant, move);

                    robot = true;
                }

#if (DEBUG)
                writeChessboardToDEBUGint(positionInt);
                writeChessboardToDEBUGChar(positionChar, takePiece, castling);
#endif

                // Write the current position to the chessboard graphic in the GUI
                ChessboardGraphic(borders, x1, x2, x3, y1, y2, y3, castling, enPassant);



                //// Read the current value from the robotcontroller
                rapidBoolcapturePiece = (ABB.Robotics.Controllers.RapidDomain.Bool)rdcapturePiece.Value;
                rapidBoolcheckMate = (ABB.Robotics.Controllers.RapidDomain.Bool)rdcheckMate.Value;
                rapidBoolwaitForTurn = (ABB.Robotics.Controllers.RapidDomain.Bool)rdwaitForTurn.Value;
                rapidBoolEnPassantActive = (ABB.Robotics.Controllers.RapidDomain.Bool)rdEnPassantActive.Value;
                rapidNumxCord1 = (ABB.Robotics.Controllers.RapidDomain.Num)rdxCoord1.Value;
                rapidNumyCord1 = (ABB.Robotics.Controllers.RapidDomain.Num)rdyCoord1.Value;
                rapidNumxCord2 = (ABB.Robotics.Controllers.RapidDomain.Num)rdxCoord2.Value;
                rapidNumyCord2 = (ABB.Robotics.Controllers.RapidDomain.Num)rdyCoord2.Value;
                rapidNumxEnPassant = (ABB.Robotics.Controllers.RapidDomain.Num)rdxEnPassant.Value;
                rapidNumyEnPassant = (ABB.Robotics.Controllers.RapidDomain.Num)rdyEnPassant.Value;
                rapidStringCastlingState = (ABB.Robotics.Controllers.RapidDomain.String)rdCastlingState.Value;

                //                ROKADE:
                //    VAR string CastlingState;


                ///////////////////////////
                //EN PASSANT
                ///////////////////////////
                //    VAR bool EnPassantActive;
                //    VAR num xEnPassant;
                //    VAR num yEnPassant;
                

                //rapidBool = (ABB.Robotics.Controllers.RapidDomain.Bool)rd.Value;
                //rapidNum = (ABB.Robotics.Controllers.RapidDomain.Num)x_1.Value;

                ////assign the value of the RAPID data to a local variable
                //bool boolValue = rapidBool.Value;
                //int numValue = (int)rapidNum.Value;




                // New values to be written to the robotcontroller
                rapidBoolcapturePiece.Value = takePiece;
                rapidBoolcheckMate.Value = false;
                rapidBoolwaitForTurn.Value = false;
                rapidBoolEnPassantActive.Value = enPassant;
                rapidNumxCord1.Value = x1;
                rapidNumyCord1.Value = y1;
                rapidNumxCord2.Value = x2;
                rapidNumyCord2.Value = y2;
                rapidNumxEnPassant.Value = x3;
                rapidNumyEnPassant.Value = y3;
                rapidStringCastlingState.Value = castling;

                //Request mastership of Rapid before writing to the controller
                master = Mastership.Request(controller.Rapid);
                //Change: controller is repaced by aController
                rdxCoord1.Value = rapidNumxCord1;
                rdyCoord1.Value = rapidNumyCord1;
                rdxCoord2.Value = rapidNumxCord2;
                rdyCoord2.Value = rapidNumyCord2;
                rdxEnPassant.Value = rapidNumxEnPassant;
                rdyEnPassant.Value = rapidNumyEnPassant;
                rdEnPassantActive.Value = rapidBoolEnPassantActive;
                rdcapturePiece.Value = rapidBoolcapturePiece;
                rdcheckMate.Value = rapidBoolcheckMate;
                rdwaitForTurn.Value = rapidBoolwaitForTurn;
                rdCastlingState.Value = rapidStringCastlingState;
                //Release mastership as soon as possible
                master.ReleaseOnDispose = true;
                master.Dispose();

            }
        }

        private void ScoreAnimation(double score)
        {
            PointAnimation myPointAnimation = new PointAnimation();
            myPointAnimation.From = lgbScore.StartPoint;
            myPointAnimation.To = new Point(score, 0.5);

            myPointAnimation.Duration = new Duration(TimeSpan.FromSeconds(1));

            myStoryboard = new Storyboard();
            myStoryboard.Children.Add(myPointAnimation);

            Storyboard.SetTargetName(myPointAnimation, "lgbScore");

            Storyboard.SetTargetProperty(myPointAnimation, new PropertyPath(LinearGradientBrush.StartPointProperty));

            myStoryboard.Begin(this);
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


                    //////////////////////////////////////////////////////////// Changes the color of the squares to the piece that has moved

                    PieceMovedColor(borders, x1, x2, y1, y2);

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

                    //////////////////////////////////////////////////////////// Changes the color of the squares to the piece that has moved
                    PieceMovedColor(borders, x1, x2, y1, y2);

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


                    //////////////////////////////////////////////////////////// Changes the color of the squares to the piece that has moved
                    PieceMovedColor(borders, x1, x2, y1, y2);
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


                    //////////////////////////////////////////////////////////// Changes the color of the squares to the piece that has moved
                    PieceMovedColor(borders, x1, x2, y1, y2);
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


                    //////////////////////////////////////////////////////////// Changes the color of the squares to the piece that has moved
                    PieceMovedColor(borders, x1, x2, y1, y2);
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


                    //////////////////////////////////////////////////////////// Changes the color of the squares to the piece that has moved
                    PieceMovedColor(borders, x1, x2, y1, y2);
                }
            }));
        }

        private static void PieceMovedColor(Border[,] borders, int x1, int x2, int y1, int y2)
        {


            if (oldBorderFrom != null)
            {
                // Chnges the squares color back to the old color
                oldBorderFrom.Background = oldFromBrush;
                oldBorderTo.Background = oldToBrush;
            }

            // Copys the color of the square
            oldToBrush = borders[y2, x2].Background;
            oldFromBrush = borders[y1, x1].Background;

            // Change the color of the square
            borders[y2, x2].Background = movedFromToBrush;
            borders[y1, x1].Background = movedFromToBrush;
            
            // remebers the last border
            oldBorderFrom = borders[y1, x1];
            oldBorderTo = borders[y2, x2];
        }

        private void rdwaitForTurn_ValueChanged(object sender, DataValueChangedEventArgs e)
        {

            // Dette er funkjsonen som leser den boolske verdien for at roboten er ferdig å flytte. Kommenterer den vekk midlertidig når eg tester ut nye funksjoner.
            /*
            ABB.Robotics.Controllers.RapidDomain.Bool rapidBoolwaitForTurn;
            RapidData rdwaitForTurn = (RapidData)sender;
            rapidBoolwaitForTurn = (ABB.Robotics.Controllers.RapidDomain.Bool)rdwaitForTurn.Value;
            bool variabel = rapidBoolwaitForTurn.Value;

            if (variabel)
            ewhChessWork.Set();
            */
        }

        private void writeChessboardToDEBUGint(int[,] position)
        {
                Debug.Write("\n");

            for (int i = position.GetLength(0) - 1; i >= 0; i--)
            {
                    Debug.Write(i + 1 + " ");

                for (int j = 0; j < position.GetLength(1); j++)
                {

                        if (j == 0)
                            Debug.Write(" | " + position[i, j].ToString() + " | ");
                        else
                            Debug.Write(position[i, j].ToString() + " | ");
                }
                    Debug.Write("\n");
            }
                Debug.Write("   | ");
            for (int k = 0; k <= 7; k++)
            {
                    Debug.Write((char)(97 + k) + " | ");
            }

        }

        private void writeChessboardToDEBUGChar(char[,] position, bool takePiece, string castling)
        {
                Debug.Write("\n\n");

            for (int i = position.GetLength(0) - 1; i >= 0; i--)
            {
                    Debug.Write(i + 1 + " ");    

                for (int j = 0; j < position.GetLength(1); j++)
                {
  
                        if (j == 0)
                        {
                            if (position[i, j] == ' ')
                                Debug.Write("  |  " + position[i, j].ToString() + "  |  ");
                            else
                                Debug.Write("  |  " + position[i, j].ToString() + "  |  ");

                        }
                        else
                        {
                            if (position[i, j] == ' ')
                                Debug.Write(position[i, j].ToString() + "  |  ");
                            else
                                Debug.Write(position[i, j].ToString() + "  |  ");
                        }
                }
                    Debug.Write("\n");
            }

                Debug.Write("\n       ");

            for (int k = 0; k <= 7; k++)
            { 
                    Debug.Write((char)(97 + k) + "     ");
            }

            this.Dispatcher.Invoke((Action)(() =>
            {
                Debug.Write("\n\nCapture piece = " + takePiece);
            }));

            this.Dispatcher.Invoke((Action)(() =>
            {
                Debug.Write("\n\nCastling = " + castling);
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

        private void btnRedChessboard_Click(object sender, RoutedEventArgs e)
        {
            Brush firstBrush = Brushes.Red;
            Brush secondBrush = Brushes.DarkRed;
            movedFromToBrush = Brushes.LightGray;

            cbChessBoardStyleColorLeftSide.Color = Colors.Red;
            cbChessBoardStyleColorRightSide.Color = Colors.DarkRed;
         
            ColorChessboard(firstBrush, secondBrush);
        }

        private void btnBlueChessboard_Click(object sender, RoutedEventArgs e)
        {
            Brush firstBrush = Brushes.LightBlue;
            Brush secondBrush = Brushes.Blue;
            movedFromToBrush = Brushes.LightGreen;

            cbChessBoardStyleColorLeftSide.Color = Colors.LightBlue;
            cbChessBoardStyleColorRightSide.Color = Colors.Blue;

            ColorChessboard(firstBrush, secondBrush);
        }

        private void btnGreyChessboard_Click(object sender, RoutedEventArgs e)
        {
            Brush firstBrush = Brushes.LightGray;
            Brush secondBrush = Brushes.Gray;
            movedFromToBrush = Brushes.LightPink;

            cbChessBoardStyleColorLeftSide.Color = Colors.LightGray;
            cbChessBoardStyleColorRightSide.Color = Colors.Gray;

            ColorChessboard(firstBrush, secondBrush);
        }

        private void btnPinkChessboard_Click(object sender, RoutedEventArgs e)
        {
            Brush firstBrush = Brushes.LightPink;
            Brush secondBrush = Brushes.DeepPink;
            movedFromToBrush = Brushes.LightBlue;

            cbChessBoardStyleColorLeftSide.Color = Colors.LightPink;
            cbChessBoardStyleColorRightSide.Color = Colors.DeepPink;

            ColorChessboard(firstBrush, secondBrush);
        }
        private void sPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            visionSystemInData = sp.ReadExisting();
        }


    }
}


