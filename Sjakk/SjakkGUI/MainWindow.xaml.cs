﻿//#define DEBUG

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
        UCI myChessEngine;
        string enginePath = @"stockfish_14053109_32bit.exe";
        Thread threadChessWork;

        static bool robot = false;

        // For communicating with Scorpion Vision 
        SerialPort sPort;
        static string visionSystemInData = string.Empty;
        static string humanMoveFromScorpion = string.Empty;

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

        // The virtual COM port used with Scorpion Vision
        static string virtualCOM = "COM24";

        public MainWindow()
        {

            InitializeComponent();

            CreateController();

            // Setup for the COM port. Is used to communicate with Scorpion Vision System

#if(DEBUG)
                string[] ports = SerialPort.GetPortNames();

                if (ports.Contains(virtualCOM))
                {
                    sPort = new SerialPort(virtualCOM, 9600, Parity.None, 8, StopBits.One);
                    sPort.DataReceived += sPort_DataReceived;
                    sPort.Open();
                }
                else
                {
                    MessageBox.Show("Could not find " + virtualCOM + ". This is the virtual COM port used to connect with Scorpion Vision System. To set up a virtual COM use com0com");
                }
#endif

            ewhChessWork = new EventWaitHandle(false, EventResetMode.AutoReset);
            threadChessWork = new Thread(chessWork);
            threadChessWork.IsBackground = true;
            threadChessWork.Start();

            lblHumanRobotTurn.Content = "Human";

            myChessEngine = new UCI();
            myChessEngine.InitEngine(enginePath, "");

            lblShowMode.Content = "";

            // Event triggerd when operating mode changes
            controller.OperatingModeChanged += controller_OperatingModeChanged;

            // Remove pictures from grid. Are goning to use the for promotion
            MainGrid.Children.Remove(Imagebq1);
            MainGrid.Children.Remove(Imagewq1);
            Imagebq1.Visibility = Visibility.Visible;
            Imagewq1.Visibility = Visibility.Visible;


            // Set board style
            Brush firstBrush = Brushes.LightGray;
            Brush secondBrush = Brushes.Gray;
            movedFromToBrush = Brushes.LightPink;

            cbChessBoardStyleColorLeftSide.Color = Colors.LightGray;
            cbChessBoardStyleColorRightSide.Color = Colors.Gray;

            ColorChessboard(firstBrush, secondBrush);


            // Depth combobox
            cbDepth.SelectionChanged += cbDepth_SelectionChanged;
            cbDepth.SelectedIndex = 3;

            // Skill Level combobox
            cbSkillLevel.SelectionChanged += cbSkillLevel_SelectionChanged;
            cbSkillLevel.SelectedIndex = 0;


            MainWindowWindow.KeyDown += MainWindowWindow_KeyDown;

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
#if(DEBUG)
            ControllerInfo[] controllers = scanner.GetControllers(NetworkScannerSearchCriterias.Virtual);
#else
                        ControllerInfo[] controllers = scanner.GetControllers(NetworkScannerSearchCriterias.Real);
#endif

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
                lblShowMode.Content = e.NewMode;
            }));
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            ewhChessWork.Set();
        }


        private void chessWork(object obj)
        {
            // true if it's the robots turn
            robot = false;
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
            string promotion;

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

                // For testing
                if (true)
                {
                    if (false)
                    {
                        // The best move the robot can make
                        move = myChessEngine.BestMove;
                        // Sends in the move to the UCI object. Gets out coordinates to be sent to robotcontroller
                        myChessEngine.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2, out x3, out y3, out takePiece, out positionInt, out positionChar, out castling, out enPassant, out promotion, move, robot);


                        myChessEngine.EngineCommandMove(move);

                        // Wait for calculations to finish
                        myChessEngine.ewhCalculating.WaitOne();


                        double scoreFormated = 0.5 + Convert.ToDouble(myChessEngine.Score) / 2000; // Max 10 pawns


                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            lblBlackScore.Content = string.Format("{0:F1}", (Convert.ToDouble(myChessEngine.Score) * -1) / 100);
                            lblWhiteScore.Content = string.Format("{0:F1}", Convert.ToDouble(myChessEngine.Score) / 100);

                            Debug.Write("Tidligere trekk: " + myChessEngine.EarlierMoves + "\n" + "Beste trekk: " + myChessEngine.BestMove + "\nStilling: " + myChessEngine.Score + " Score: " + scoreFormated);
                            tbNextMove.Text = myChessEngine.BestMove;

                            lblHumanRobotTurn.Content = "Human";

                            ScoreAnimation(scoreFormated);

                        }));
                        robot = false;
                    }
                    else
                    {
                        move = humanMoveFromScorpion;
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            // Takes in the move from the human player
                            myChessEngine.EngineCommandMove(move);
                        }));

                        // Wait for calculations to finish
                        myChessEngine.ewhCalculating.WaitOne();


                        // Lag metode av denne biten

                        // Get score
                        double scoreFormated = 0.5 + (Convert.ToDouble(myChessEngine.Score) * -1) / 2000; // max score 10 pawns


                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            lblBlackScore.Content = string.Format("{0:F1}", Convert.ToDouble(myChessEngine.Score) / 100);
                            lblWhiteScore.Content = string.Format("{0:F1}", (Convert.ToDouble(myChessEngine.Score) * -1) / 100);

                            Debug.Write("Tidligere trekk: " + myChessEngine.EarlierMoves + "\n" + "Beste trekk: " + myChessEngine.BestMove + "\nStilling: " + myChessEngine.Score + " Score: " + scoreFormated);
                            //tbNextMove.Text = myChessEngine.BestMove;
                            tbNextMove.Clear();

                            ScoreAnimation(scoreFormated);

                            lblHumanRobotTurn.Content = "Robot";
                        }));

                        // Coordinates to be sent to robotcontroller
                        myChessEngine.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2, out x3, out y3, out takePiece, out positionInt, out positionChar, out castling, out enPassant, out promotion, move, robot);

                        robot = true;
                    }
                }
                else
                {
                    if (robot)
                    {
                        // The best move the robot can make
                        move = myChessEngine.BestMove;
                        // Sends in the move to the UCI object. Gets out coordinates to be sent to robotcontroller
                        myChessEngine.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2, out x3, out y3, out takePiece, out positionInt, out positionChar, out castling, out enPassant, out promotion, move, robot);


                        myChessEngine.EngineCommandMove(move);

                        // Wait for calculations to finish
                        myChessEngine.ewhCalculating.WaitOne();


                        double scoreFormated = 0.5 + Convert.ToDouble(myChessEngine.Score) / 2000; // Max 10 pawns


                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            lblBlackScore.Content = string.Format("{0:F1}", (Convert.ToDouble(myChessEngine.Score) * -1) / 100);
                            lblWhiteScore.Content = string.Format("{0:F1}", Convert.ToDouble(myChessEngine.Score) / 100);

                            Debug.Write("Tidligere trekk: " + myChessEngine.EarlierMoves + "\n" + "Beste trekk: " + myChessEngine.BestMove + "\nStilling: " + myChessEngine.Score + " Score: " + scoreFormated);
                            tbNextMove.Text = myChessEngine.BestMove;

                            lblHumanRobotTurn.Content = "Human";

                            ScoreAnimation(scoreFormated);

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
                        double scoreFormated = 0.5 + (Convert.ToDouble(myChessEngine.Score) * -1) / 2000; // max score 10 pawns


                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            lblBlackScore.Content = string.Format("{0:F1}", Convert.ToDouble(myChessEngine.Score) / 100);
                            lblWhiteScore.Content = string.Format("{0:F1}", (Convert.ToDouble(myChessEngine.Score) * -1) / 100);

                            Debug.Write("Tidligere trekk: " + myChessEngine.EarlierMoves + "\n" + "Beste trekk: " + myChessEngine.BestMove + "\nStilling: " + myChessEngine.Score + " Score: " + scoreFormated);
                            //tbNextMove.Text = myChessEngine.BestMove;
                            tbNextMove.Clear();

                            ScoreAnimation(scoreFormated);

                            lblHumanRobotTurn.Content = "Robot";
                        }));

                        // Coordinates to be sent to robotcontroller
                        myChessEngine.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2, out x3, out y3, out takePiece, out positionInt, out positionChar, out castling, out enPassant, out promotion, move, robot);

                        robot = true;
                    }
                }

#if (DEBUG)
                writeChessboardToDEBUGint(positionInt);
                writeChessboardToDEBUGChar(positionChar, takePiece, castling, promotion);
#endif

                // Write the current position to the chessboard graphic in the GUI
                ChessboardGraphic(borders, x1, x2, x3, y1, y2, y3, castling, enPassant, promotion);

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
        private void ChessboardGraphic(Border[,] borders, int x1, int x2, int x3, int y1, int y2, int y3, string castling, bool enPasant, string promotion)
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

                if (promotion == "WPromotion")
                {
                    Image image = new Image();
                    // delete the piece in old position
                    borders[y1, x1].Child = null;
                    // Paste piece in new position
                    borders[y2, x2].Child = Imagewq1;
                }
                else if (promotion == "BPromotion")
                {
                    Image image = new Image();
                    // delete the piece in old position
                    borders[y1, x1].Child = null;
                    // Paste piece in new position
                    borders[y2, x2].Child = Imagebq1;
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

        private void writeChessboardToDEBUGChar(char[,] position, bool takePiece, string castling, string promotion)
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


            Debug.Write("\n\nCapture piece = " + takePiece);

            Debug.Write("\n\nCastling = " + castling);

            Debug.Write("\n\nPromotion = " + promotion);

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

        private void btnBrownChessboard_Click(object sender, RoutedEventArgs e)
        {
            Brush firstBrush = Brushes.SandyBrown;
            Brush secondBrush = Brushes.SaddleBrown;
            movedFromToBrush = Brushes.LightGoldenrodYellow;

            cbChessBoardStyleColorLeftSide.Color = Colors.SandyBrown;
            cbChessBoardStyleColorRightSide.Color = Colors.SaddleBrown;

            ColorChessboard(firstBrush, secondBrush);
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

        void cbDepth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int caseSwitch = cbDepth.SelectedIndex;
            switch (caseSwitch)
            {
                case 0:
                    myChessEngine.Depth = "1";
                    break;
                case 1:
                    myChessEngine.Depth = "2";
                    break;
                case 2:
                    myChessEngine.Depth = "3";
                    break;
                case 3:
                    myChessEngine.Depth = "4";
                    break;
                case 4:
                    myChessEngine.Depth = "5";
                    break;
                case 5:
                    myChessEngine.Depth = "6";
                    break;
                case 6:
                    myChessEngine.Depth = "7";
                    break;
                case 7:
                    myChessEngine.Depth = "8";
                    break;
                case 8:
                    myChessEngine.Depth = "9";
                    break;
                case 9:
                    myChessEngine.Depth = "10";
                    break;
                case 10:
                    myChessEngine.Depth = "11";
                    break;
                case 11:
                    myChessEngine.Depth = "12";
                    break;
                case 12:
                    myChessEngine.Depth = "13";
                    break;
                case 13:
                    myChessEngine.Depth = "14";
                    break;
                case 14:
                    myChessEngine.Depth = "15";
                    break;
                case 15:
                    myChessEngine.Depth = "16";
                    break;
                case 16:
                    myChessEngine.Depth = "17";
                    break;
                case 17:
                    myChessEngine.Depth = "18";
                    break;
                case 18:
                    myChessEngine.Depth = "19";
                    break;
                case 19:
                    myChessEngine.Depth = "20";
                    break;
                case 20:
                    myChessEngine.Depth = "21";
                    break;
                case 21:
                    myChessEngine.Depth = "22";
                    break;
                case 22:
                    myChessEngine.Depth = "23";
                    break;
                case 23:
                    myChessEngine.Depth = "24";
                    break;
                case 24:
                    myChessEngine.Depth = "25";
                    break;
                case 25:
                    myChessEngine.Depth = "26";
                    break;
            }
        }

        void cbSkillLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int caseSwitch = cbSkillLevel.SelectedIndex;
            switch (caseSwitch)
            {
                case 0:
                    myChessEngine.SkillLevel("0");
                    break;
                case 1:
                    myChessEngine.SkillLevel("1");
                    break;
                case 2:
                    myChessEngine.SkillLevel("2");
                    break;
                case 3:
                    myChessEngine.SkillLevel("3");
                    break;
                case 4:
                    myChessEngine.SkillLevel("4");
                    break;
                case 5:
                    myChessEngine.SkillLevel("5");
                    break;
                case 6:
                    myChessEngine.SkillLevel("6");
                    break;
                case 7:
                    myChessEngine.SkillLevel("7");
                    break;
                case 8:
                    myChessEngine.SkillLevel("8");
                    break;
                case 9:
                    myChessEngine.SkillLevel("9");
                    break;
                case 10:
                    myChessEngine.SkillLevel("10");
                    break;
                case 11:
                    myChessEngine.SkillLevel("11");
                    break;
                case 12:
                    myChessEngine.SkillLevel("12");
                    break;
                case 13:
                    myChessEngine.SkillLevel("13");
                    break;
                case 14:
                    myChessEngine.SkillLevel("14");
                    break;
                case 15:
                    myChessEngine.SkillLevel("15");
                    break;
                case 16:
                    myChessEngine.SkillLevel("16");
                    break;
                case 17:
                    myChessEngine.SkillLevel("17");
                    break;
                case 18:
                    myChessEngine.SkillLevel("18");
                    break;
                case 19:
                    myChessEngine.SkillLevel("19");
                    break;
                case 20:
                    myChessEngine.SkillLevel("20");
                    break;
            }
        }

        private void btnNewGame_Click(object sender, RoutedEventArgs e)
        {

            myChessEngine.NewGame();

            robot = false;

            // reset score
            lblBlackScore.Content = "-0,2";
            lblWhiteScore.Content = "0,2";
            ScoreAnimation(0.492);

            // Set board style
            Brush firstBrush = Brushes.LightGray;
            Brush secondBrush = Brushes.Gray;
            movedFromToBrush = Brushes.LightPink;

            cbChessBoardStyleColorLeftSide.Color = Colors.LightGray;
            cbChessBoardStyleColorRightSide.Color = Colors.Gray;

            ColorChessboard(firstBrush, secondBrush);

            oldBorderFrom = null;
            oldBorderTo = null;

            oldToBrush = null;
            oldFromBrush = null;

            List<Border> borders = new List<Border>();

            // Set up start position
            foreach (var child in gridChessboard.Children)
            {
                if (child != null)
                {
                    if (child.GetType().Name == "Border")
                    {
                        borders.Add((Border)child);
                    }
                }
            }

            
            foreach (Border border in borders)
            {    
                if (border.Child != null)
                {
                    if (border.Child.GetType().Name != "Label")
                        border.Child = null;
                }
            }

            Border_a8.Child = ImagebrLeft;
            Border_b8.Child = ImagebnLeft;
            Border_c8.Child = ImagebbLeft;
            Border_d8.Child = Imagebq;
            Border_e8.Child = Imagebk;
            Border_f8.Child = ImagebbRight;
            Border_g8.Child = ImagebnRight;
            Border_h8.Child = ImagebrRight;
            Border_a7.Child = Imagebp_1;
            Border_b7.Child = Imagebp_2;
            Border_c7.Child = Imagebp_3;
            Border_d7.Child = Imagebp_4;
            Border_e7.Child = Imagebp_5;
            Border_f7.Child = Imagebp_6;
            Border_g7.Child = Imagebp_7;
            Border_h7.Child = Imagebp_8;
            Border_a2.Child = Imagewp_1;
            Border_b2.Child = Imagewp_2;
            Border_c2.Child = Imagewp_3;
            Border_d2.Child = Imagewp_4;
            Border_e2.Child = Imagewp_5;
            Border_f2.Child = Imagewp_6;
            Border_g2.Child = Imagewp_7;
            Border_h2.Child = Imagewp_8;
            Border_a1.Child = ImagewrLeft;
            Border_b1.Child = ImagewnLeft;
            Border_c1.Child = ImagewbLeft;
            Border_d1.Child = Imagewq;
            Border_e1.Child = Imagewk;
            Border_f1.Child = ImagewbRight;
            Border_g1.Child = ImagewnRight;
            Border_h1.Child = ImagewrRight;


        }

        private void btnFullscreen_Click(object sender, RoutedEventArgs e)
        {
            MainWindowWindow.MaxHeight = 10000;
            MainWindowWindow.MaxWidth = 10000;

            MainWindowWindow.WindowStyle = System.Windows.WindowStyle.None;
            MainWindowWindow.WindowState = System.Windows.WindowState.Maximized;

            btnFullscreen.Visibility = Visibility.Hidden;
            btnNewGame.Visibility = Visibility.Hidden;
            //btnSend.Visibility = Visibility.Hidden;
            //tbNextMove.Visibility = Visibility.Hidden;
            cbChessboardStyle.Visibility = Visibility.Hidden;
            cbDepth.Visibility = Visibility.Hidden;
            cbSkillLevel.Visibility = Visibility.Hidden;
            lblHumanRobotTurn.Visibility = Visibility.Hidden;
            lblShowMode.Visibility = Visibility.Hidden;
            lblMode.Visibility = Visibility.Hidden;
            lblSkillLevel.Visibility = Visibility.Hidden;
            lblDepth.Visibility = Visibility.Hidden;
            lstControllerInformation.Visibility = Visibility.Hidden;

           // gridChessboard.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

           
        }

        void MainWindowWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                MainWindowWindow.MaxHeight = 750;
                MainWindowWindow.MaxWidth = 1200;
                MainWindowWindow.WindowStyle = System.Windows.WindowStyle.ToolWindow;
                MainWindowWindow.WindowState = System.Windows.WindowState.Normal;

                btnFullscreen.Visibility = Visibility.Visible;
                btnNewGame.Visibility = Visibility.Visible;
                btnSend.Visibility = Visibility.Visible;
                tbNextMove.Visibility = Visibility.Visible;
                cbChessboardStyle.Visibility = Visibility.Visible;
                cbDepth.Visibility = Visibility.Visible;
                cbSkillLevel.Visibility = Visibility.Visible;
                lblHumanRobotTurn.Visibility = Visibility.Visible;
                lblShowMode.Visibility = Visibility.Visible;
                lblMode.Visibility = Visibility.Visible;
                lblSkillLevel.Visibility = Visibility.Visible;
                lblDepth.Visibility = Visibility.Visible;
                lstControllerInformation.Visibility = Visibility.Visible;
            }
        }

        private void sPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            visionSystemInData = sp.ReadExisting();
            List<int> axisValues = new List<int>();


                while (Regex.IsMatch(visionSystemInData,@"\d{1,3}(?=\.)"))
                {
                    Match match = Regex.Match(visionSystemInData, @"\d{1,3}(?=\.)");
                    axisValues.Add(Convert.ToInt16(match.Value));
                    visionSystemInData = visionSystemInData.Remove(match.Index, match.Length);
                }

                string Move = "";


            for (int i = 0; i < 3; i += 2)
            {
                int x_value = axisValues[i];
                int y_value = axisValues[i + 1];

                if (x_value > 4 && x_value < 47)
                {
                    Move += "a";
                }
                else if (x_value > 47 && x_value < 90)
                {
                    Move += "b";
                }
                else if (x_value > 90 && x_value < 133)
                {
                    Move += "c";
                }
                else if (x_value > 133 && x_value < 176)
                {
                    Move += "d";
                }
                else if (x_value > 176 && x_value < 220)
                {
                    Move += "e";
                }
                else if (x_value > 220 && x_value < 263)
                {
                    Move += "f";
                }
                else if (x_value > 263 && x_value < 304)
                {
                    Move += "g";
                }
                else if (x_value > 304 && x_value < 350)
                {
                    Move += "h";
                }


                if (y_value > 4 && y_value < 47)
                {
                    Move += "1";
                }
                else if (y_value > 47 && y_value < 90)
                {
                    Move += "2";
                }
                else if (y_value > 90 && y_value < 133)
                {
                    Move += "3";
                }
                else if (y_value > 133 && y_value < 176)
                {
                    Move += "4";
                }
                else if (y_value > 176 && y_value < 220)
                {
                    Move += "5";
                }
                else if (y_value > 220 && y_value < 263)
                {
                    Move += "6";
                }
                else if (y_value > 263 && y_value < 304)
                {
                    Move += "7";
                }
                else if (y_value > 304 && y_value < 350)
                {
                    Move += "8";
                }

            }

            Debug.WriteLine(Move);
            humanMoveFromScorpion = Move;
            ewhChessWork.Set();
          
        }

    }// MainWindow
}


