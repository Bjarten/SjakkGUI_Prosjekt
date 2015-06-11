/*
 GUI program for Bacheloroppgave BO15E-15 Sjakkrobot med datasyn
 Skrevet av: Bjarte Mehus Sunde, Henrik Kilvær, Tor Stian Hjørungdal.
 Utarbeidet våren 2015 ved Høgskolen i Bergen, Institutt for elektrofag
 
 Spørmål om kode kan sendes til BjarteSunde@Outlook.com
 */

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
using System.Timers;

using ABB.Robotics;
using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.RapidDomain;



using System.Text.RegularExpressions;

using System.Windows.Media.Animation;

using System.IO.Ports;


namespace Chess_Robot
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        EventWaitHandle ewhChessWork;
        EventWaitHandle ewhWaitForRobotToFinish;
        EventWaitHandle ewhSafeNewGame_1;
        EventWaitHandle ewhSafeNewGame_2;
        EventWaitHandle ewhNewGame;

        UCI myChessEngine;
        string enginePath = @"stockfish_14053109_32bit.exe";
        Thread threadChessWork;
        Thread threadNewGame;

        static bool blackSide = false;
        static bool checkmate = false;
        string playerName = "Human";

        public string PlayerName
        {
            get { return playerName; }
            set { playerName = value; }
        }

        // For communicating with Scorpion Vision 
        SerialPort sPort;
        static string visionSystemInData = string.Empty;
        static string humanMoveFromScorpion = string.Empty;
        enum Mode { HumanVsRobot = 0, RobotVsRobot = 1, Manual = 2 };
        static Mode SelectedMode = new Mode();
        static bool useThisPicture = false;
        static string sysID = string.Empty;
        static NetworkScanner scanner;
        static Controller controller;
        static Mastership master;
        static bool firstTimeTrough = true;
        static System.Timers.Timer myTimer;
        static bool waitForTimer;

        static Border oldBorderFrom = null;
        static Border oldBorderTo = null;

        static Brush oldToBrush = null;
        static Brush oldFromBrush = null;

        static Brush movedFromToBrush;

        static Storyboard myStoryboard;

        // The virtual COM port used with Scorpion Vision
        static string virtualCOM = "COM7";

        // Used to check if there is a second queenpromotion
        static bool secondBPromotion = false;
        static bool secondWPromotion = false;

        // Used in the Robot VS. robot Mode
        static string bestMoveWhiteSide = "e2e4";

        public MainWindow()
        {

            InitializeComponent();

            CreateController();

            // Setup for the COM port. Is used to communicate with Scorpion Vision System


            string[] ports = SerialPort.GetPortNames();

            if (ports.Contains(virtualCOM))
            {
                sPort = new SerialPort(virtualCOM, 9600, Parity.None, 8, StopBits.One);
                sPort.DataReceived += sPort_DataReceived;
                sPort.Open();
                sPort.DiscardInBuffer();
            }
            else
            {
                System.Media.SystemSounds.Exclamation.Play();
                this.Dispatcher.Invoke((Action)(() =>
                {
                MessageBox.Show(this, "Could not find " + virtualCOM + ". This is the virtual COM port used to connect with Scorpion Vision System. To set up a virtual COM use com0com");
                }));
            }

            ewhChessWork = new EventWaitHandle(false, EventResetMode.AutoReset);
            threadChessWork = new Thread(chessWork);
            threadChessWork.IsBackground = true;
            threadChessWork.Start();

            ewhNewGame = new EventWaitHandle(false, EventResetMode.AutoReset);
            threadNewGame = new Thread(newGame);
            threadNewGame.IsBackground = true;
            threadNewGame.Start();

            ewhWaitForRobotToFinish = new EventWaitHandle(false, EventResetMode.AutoReset);
            ewhSafeNewGame_1 = new EventWaitHandle(true, EventResetMode.ManualReset);
            ewhSafeNewGame_2 = new EventWaitHandle(true, EventResetMode.ManualReset);


            lblHumanRobotTurn.Content = "Human";

            myChessEngine = new UCI();
            myChessEngine.InitEngine(enginePath, "");

            lblShowMode.Content = "";

            // Event triggerd when operating mode changes
            controller.OperatingModeChanged += controller_OperatingModeChanged;

            // Remove pictures from grid. Are goning to use the for promotion
            MainGrid.Children.Remove(Imagebq1);
            MainGrid.Children.Remove(Imagewq1);
            MainGrid.Children.Remove(Imagebq2);
            MainGrid.Children.Remove(Imagewq2);
            Imagebq1.Visibility = Visibility.Visible;
            Imagewq1.Visibility = Visibility.Visible;
            Imagebq2.Visibility = Visibility.Visible;
            Imagewq2.Visibility = Visibility.Visible;

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

            // Mode combobox
            cbMode.SelectionChanged += cbMode_SelectionChanged;
            cbMode.SelectedIndex = 2;

            MainWindowWindow.KeyDown += MainWindowWindow_KeyDown;

            // When the light is grren there will be taken a picture, when the light is white not.
            ChangeColorToWhite();

            myTimer = new System.Timers.Timer(1000);
            myTimer.Elapsed += myTimer_Elapsed;
            waitForTimer = false;

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
            sendMove();
        }

        private void sendMove()
        {
            if (SelectedMode == Mode.Manual)
            {
                if (Regex.IsMatch(tbNextMove.Text.ToLower(), @"^[a-h][1-8][a-h][1-8]$") || Regex.IsMatch(tbNextMove.Text.ToLower(), @"^[a-h][1-8][a-h][1-8]q$"))
                    ewhChessWork.Set();
                else
                {
                    tbNextMove.Clear();
                    MessageBox.Show(this,"wrong syntax","Error",MessageBoxButton.OK,MessageBoxImage.Error,MessageBoxResult.OK);
                }
            }

            if (SelectedMode == Mode.RobotVsRobot)
                ewhChessWork.Set();
        }


        private void chessWork(object obj)
        {
            // true if it's the robots turn
            blackSide = false;
            string move = string.Empty;

            int x1 = 7;
            int x2 = 8;
            int x3 = 8;
            int y1 = 7;
            int y2 = 8;
            int y3 = 8;
            bool capturePiece = false;
            int[,] positionInt = null;
            char[,] positionChar = null;
            string castling = System.String.Empty;
            bool enPassant = false;
            string promotion = System.String.Empty;
            bool illegalMove = false;
            string victory = string.Empty; 


            Border[,] bordersChessboard = new Border[8, 8] {{ Border_a1 , Border_b1 , Border_c1 , Border_d1 , Border_e1 , Border_f1 , Border_g1 , Border_h1 },
                                                  { Border_a2 , Border_b2 , Border_c2 , Border_d2 , Border_e2 , Border_f2 , Border_g2 , Border_h2 },
 				                                  { Border_a3 , Border_b3 , Border_c3 , Border_d3 , Border_e3 , Border_f3 , Border_g3 , Border_h3 },
                                                  { Border_a4 , Border_b4 , Border_c4 , Border_d4 , Border_e4 , Border_f4 , Border_g4 , Border_h4 },
                                                  { Border_a5 , Border_b5 , Border_c5 , Border_d5 , Border_e5 , Border_f5 , Border_g5 , Border_h5 },
                                                  { Border_a6 , Border_b6 , Border_c6 , Border_d6 , Border_e6 , Border_f6 , Border_g6 , Border_h6 },
                                                  { Border_a7 , Border_b7, Border_c7 , Border_d7 , Border_e7 , Border_f7 , Border_g7 , Border_h7, },
                                                  { Border_a8 , Border_b8, Border_c8 , Border_d8 , Border_e8 , Border_f8 , Border_g8 , Border_h8 }};

            Border[,] bordersCapturedPiecesWhite = new Border[8, 2]{{ Border_WhitePieceCaptured_1 , Border_WhitePieceCaptured_9  },
                                                                    { Border_WhitePieceCaptured_2 , Border_WhitePieceCaptured_10 },
 				                                                    { Border_WhitePieceCaptured_3 , Border_WhitePieceCaptured_11 },
                                                                    { Border_WhitePieceCaptured_4 , Border_WhitePieceCaptured_12 },
                                                                    { Border_WhitePieceCaptured_5 , Border_WhitePieceCaptured_13 },
                                                                    { Border_WhitePieceCaptured_6 , Border_WhitePieceCaptured_14 },
                                                                    { Border_WhitePieceCaptured_7 , Border_WhitePieceCaptured_15 },
                                                                    { Border_WhitePieceCaptured_8 , Border_WhitePieceCaptured_16 }};

            Border[,] bordersCapturedPiecesBlack = new Border[8, 2]{{ Border_BlackPieceCaptured_1 , Border_BlackPieceCaptured_9  },
                                                                    { Border_BlackPieceCaptured_2 , Border_BlackPieceCaptured_10 },
 				                                                    { Border_BlackPieceCaptured_3 , Border_BlackPieceCaptured_11 },
                                                                    { Border_BlackPieceCaptured_4 , Border_BlackPieceCaptured_12 },
                                                                    { Border_BlackPieceCaptured_5 , Border_BlackPieceCaptured_13 },
                                                                    { Border_BlackPieceCaptured_6 , Border_BlackPieceCaptured_14 },
                                                                    { Border_BlackPieceCaptured_7 , Border_BlackPieceCaptured_15 },
                                                                    { Border_BlackPieceCaptured_8 , Border_BlackPieceCaptured_16 }};

            //declare a variable of data type RapidDomain.Bool
            ABB.Robotics.Controllers.RapidDomain.Num rapidNumxCord1;
            ABB.Robotics.Controllers.RapidDomain.Num rapidNumxCord2;
            ABB.Robotics.Controllers.RapidDomain.Num rapidNumyCord1;
            ABB.Robotics.Controllers.RapidDomain.Num rapidNumyCord2;
            ABB.Robotics.Controllers.RapidDomain.Num rapidNumxEnPassant;
            ABB.Robotics.Controllers.RapidDomain.Num rapidNumyEnPassant;
            ABB.Robotics.Controllers.RapidDomain.Bool rapidBoolEnPassantActive;
            ABB.Robotics.Controllers.RapidDomain.Bool rapidBoolcapturePiece;
            ABB.Robotics.Controllers.RapidDomain.Bool rapidBoolwaitForTurn;
            ABB.Robotics.Controllers.RapidDomain.String rapidStringCastlingState;
            ABB.Robotics.Controllers.RapidDomain.String rapidStringqPromotion;
            ABB.Robotics.Controllers.RapidDomain.String rapidStringvictory;

            //Make a variable that is connected to the variable in the robotcontroller 
            ABB.Robotics.Controllers.RapidDomain.RapidData rdxCoord1 = controller.Rapid.GetRapidData("T_ROB1", "Chess_Robot_RAPID_final", "xCoord1");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdyCoord1 = controller.Rapid.GetRapidData("T_ROB1", "Chess_Robot_RAPID_final", "yCoord1");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdxCoord2 = controller.Rapid.GetRapidData("T_ROB1", "Chess_Robot_RAPID_final", "xCoord2");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdyCoord2 = controller.Rapid.GetRapidData("T_ROB1", "Chess_Robot_RAPID_final", "yCoord2");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdxEnPassant = controller.Rapid.GetRapidData("T_ROB1", "Chess_Robot_RAPID_final", "xEnPassant");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdyEnPassant = controller.Rapid.GetRapidData("T_ROB1", "Chess_Robot_RAPID_final", "yEnPassant");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdEnPassantActive = controller.Rapid.GetRapidData("T_ROB1", "Chess_Robot_RAPID_final", "EnPassantActive");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdcapturePiece = controller.Rapid.GetRapidData("T_ROB1", "Chess_Robot_RAPID_final", "capturePiece");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdwaitForTurn = controller.Rapid.GetRapidData("T_ROB1", "Chess_Robot_RAPID_final", "waitForTurn");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdCastlingState = controller.Rapid.GetRapidData("T_ROB1", "Chess_Robot_RAPID_final", "CastlingState");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdqPromotion = controller.Rapid.GetRapidData("T_ROB1", "Chess_Robot_RAPID_final", "qPromotion");
            ABB.Robotics.Controllers.RapidDomain.RapidData rdvictory = controller.Rapid.GetRapidData("T_ROB1", "Chess_Robot_RAPID_final", "victory");

            // Event that fires when the waitForTurn variable changes value
            rdwaitForTurn.ValueChanged += rdwaitForTurn_ValueChanged;

            while (true)
            {
                ewhChessWork.WaitOne();
                ewhSafeNewGame_1.Reset();

                this.Dispatcher.Invoke((Action)(() =>
                {
                    cbMode.IsEnabled = false;
                    cbDepth.IsEnabled = false;
                    cbSkillLevel.IsEnabled = false;
                    cbChessboardStyle.IsEnabled = false;
                    if (myChessEngine.CountEveryMove % 2 == 0)
                        lblMovesMade.Content = string.Empty;
                }));

                if (SelectedMode == Mode.HumanVsRobot)
                {
                    if (blackSide)
                    {
                        // The best move the robot can do
                        move = myChessEngine.BestMove;

                        myChessEngine.EngineCommandMove(move);

                        // Wait for calculations to finish
                        myChessEngine.ewhCalculating.WaitOne();


                        double scoreFormated = 0.5 + Convert.ToDouble(myChessEngine.Score) / 2000; // Max 10 pawns


                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            lblBlackScore.Content = string.Format("{0:F1}", (Convert.ToDouble(myChessEngine.Score) * -1) / 100);
                            lblWhiteScore.Content = string.Format("{0:F1}", Convert.ToDouble(myChessEngine.Score) / 100);

                            Debug.Write("Tidligere trekk: " + myChessEngine.EarlierMoves + "\n" + "Beste trekk: " + myChessEngine.BestMove + "\nStilling: " + myChessEngine.Score + " Score: " + scoreFormated);
                            tbNextMove.Clear();

                            lblBestMove.Content = myChessEngine.BestMove;

                            lblHumanRobotTurn.Content = PlayerName;

                            ScoreAnimation(scoreFormated);

                            lblMoveNumber.Content = myChessEngine.CountMove;



                        }));

                        // Sends in the move to the UCI object. Gets out coordinates to be sent to robotcontroller
                        myChessEngine.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2, out x3, out y3, out capturePiece, out positionInt, out positionChar, out castling, out enPassant, out promotion, move, blackSide);

                        ChessboardGraphic(bordersChessboard, bordersCapturedPiecesWhite, bordersCapturedPiecesBlack, x1, x2, x3, y1, y2, y3, castling, enPassant, promotion, capturePiece);
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            lblMovesMade.Content += myChessEngine.LastPieceMoved.ToString().ToUpper() + move + "  ";

                        }));
                    }
                    else
                    {
                        if (myChessEngine.LegalMoveWhite(humanMoveFromScorpion))
                        {

                            this.Dispatcher.Invoke((Action)(() =>
                            {
                                lblHumanRobotTurn.Content = "Robot";
                            }));

                            move = humanMoveFromScorpion;

                            myChessEngine.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2, out x3, out y3, out capturePiece, out positionInt, out positionChar, out castling, out enPassant, out promotion, move, blackSide);

                            ChessboardGraphic(bordersChessboard, bordersCapturedPiecesWhite, bordersCapturedPiecesBlack, x1, x2, x3, y1, y2, y3, castling, enPassant, promotion, capturePiece);

                            myChessEngine.EngineCommandMove(move);


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

                                lblBestMove.Content = myChessEngine.BestMove;

                                ScoreAnimation(scoreFormated);

                                lblMoveNumber.Content = myChessEngine.CountMove;

                            }));


                            // Coordinates to be sent to robotcontroller
                            //myChessEngine.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2, out x3, out y3, out capturePiece, out positionInt, out positionChar, out castling, out enPassant, out promotion, move, blackSide);
                            this.Dispatcher.Invoke((Action)(() =>
                            {
                                lblMovesMade.Content += myChessEngine.LastPieceMoved.ToString().ToUpper() + move + "  ";

                            }));
                        }//Illegal Move
                        else
                            illegalMove = true;
                    }

                }
                else if (SelectedMode == Mode.RobotVsRobot)
                {
                    if (blackSide)
                    {
                        // The best move the robot can make
                        move = myChessEngine.BestMove;
                        // Sends in the move to the UCI object. Gets out coordinates to be sent to robotcontroller
                        myChessEngine.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2, out x3, out y3, out capturePiece, out positionInt, out positionChar, out castling, out enPassant, out promotion, move, blackSide);

                        ChessboardGraphic(bordersChessboard, bordersCapturedPiecesWhite, bordersCapturedPiecesBlack, x1, x2, x3, y1, y2, y3, castling, enPassant, promotion, capturePiece);

                        myChessEngine.EngineCommandMove(move);

                        // Wait for calculations to finish
                        myChessEngine.ewhCalculating.WaitOne();


                        double scoreFormated = 0.5 + Convert.ToDouble(myChessEngine.Score) / 2000; // Max 10 pawns


                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            lblBlackScore.Content = string.Format("{0:F1}", (Convert.ToDouble(myChessEngine.Score) * -1) / 100);
                            lblWhiteScore.Content = string.Format("{0:F1}", Convert.ToDouble(myChessEngine.Score) / 100);

                            Debug.Write("Tidligere trekk: " + myChessEngine.EarlierMoves + "\n" + "Beste trekk: " + myChessEngine.BestMove + "\nStilling: " + myChessEngine.Score + " Score: " + scoreFormated);
                            bestMoveWhiteSide = myChessEngine.BestMove;

                            lblBestMove.Content = myChessEngine.BestMove;

                            lblHumanRobotTurn.Content = "Robot";

                            ScoreAnimation(scoreFormated);

                            lblMoveNumber.Content = myChessEngine.CountMove;


                        }));

                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            lblMovesMade.Content += myChessEngine.LastPieceMoved.ToString().ToUpper() + move + "  ";

                        }));
                    }
                    else
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            // Takes in the move from the human player
                            move = bestMoveWhiteSide;
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

                            lblBestMove.Content = myChessEngine.BestMove;

                            ScoreAnimation(scoreFormated);

                            lblHumanRobotTurn.Content = "Robot";

                            lblMoveNumber.Content = myChessEngine.CountMove;

                        }));

                        // Coordinates to be sent to robotcontroller
                        myChessEngine.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2, out x3, out y3, out capturePiece, out positionInt, out positionChar, out castling, out enPassant, out promotion, move, blackSide);

                        ChessboardGraphic(bordersChessboard, bordersCapturedPiecesWhite, bordersCapturedPiecesBlack, x1, x2, x3, y1, y2, y3, castling, enPassant, promotion, capturePiece);

                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            lblMovesMade.Content += myChessEngine.LastPieceMoved.ToString().ToUpper() + move + "  ";

                        }));
                    }
                }
                else if (SelectedMode == Mode.Manual)
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        move = tbNextMove.Text.ToLower();
                    }));

                    if (blackSide)
                    {
                        if (myChessEngine.LegalMoveBlack(move))
                        {

                            // Sends in the move to the UCI object. Gets out coordinates to be sent to robotcontroller
                            myChessEngine.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2, out x3, out y3, out capturePiece, out positionInt, out positionChar, out castling, out enPassant, out promotion, move, blackSide);

                            ChessboardGraphic(bordersChessboard, bordersCapturedPiecesWhite, bordersCapturedPiecesBlack, x1, x2, x3, y1, y2, y3, castling, enPassant, promotion, capturePiece);

                            myChessEngine.EngineCommandMove(move);

                            // Wait for calculations to finish
                            myChessEngine.ewhCalculating.WaitOne();


                            double scoreFormated = 0.5 + Convert.ToDouble(myChessEngine.Score) / 2000; // Max 10 pawns


                            this.Dispatcher.Invoke((Action)(() =>
                            {
                                lblBlackScore.Content = string.Format("{0:F1}", (Convert.ToDouble(myChessEngine.Score) * -1) / 100);
                                lblWhiteScore.Content = string.Format("{0:F1}", Convert.ToDouble(myChessEngine.Score) / 100);

                                Debug.Write("Tidligere trekk: " + myChessEngine.EarlierMoves + "\n" + "Beste trekk: " + myChessEngine.BestMove + "\nStilling: " + myChessEngine.Score + " Score: " + scoreFormated);
                                tbNextMove.Clear();

                                lblBestMove.Content = myChessEngine.BestMove;

                                lblHumanRobotTurn.Content = PlayerName;

                                ScoreAnimation(scoreFormated);

                                lblMoveNumber.Content = myChessEngine.CountMove;

                            }));

                            this.Dispatcher.Invoke((Action)(() =>
                            {
                                lblMovesMade.Content += myChessEngine.LastPieceMoved.ToString().ToUpper() + move + "  ";

                            }));
                        }
                        else
                            illegalMove = true;
                    }
                    else
                    {
                        if (myChessEngine.LegalMoveWhite(move))
                        {
                            myChessEngine.EngineCommandMove(move);


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

                                lblBestMove.Content = myChessEngine.BestMove;

                                ScoreAnimation(scoreFormated);

                                lblHumanRobotTurn.Content = PlayerName;

                                lblMoveNumber.Content = myChessEngine.CountMove;

                            }));

                            // Coordinates to be sent to robotcontroller
                            myChessEngine.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2, out x3, out y3, out capturePiece, out positionInt, out positionChar, out castling, out enPassant, out promotion, move, blackSide);

                            ChessboardGraphic(bordersChessboard, bordersCapturedPiecesWhite, bordersCapturedPiecesBlack, x1, x2, x3, y1, y2, y3, castling, enPassant, promotion, capturePiece);

                            this.Dispatcher.Invoke((Action)(() =>
                            {
                                lblMovesMade.Content += myChessEngine.LastPieceMoved.ToString().ToUpper() + move + "  ";

                            }));

                        }
                        else
                            illegalMove = true;
                    }
                }
                

                // Check for checkmate
                victory = Checkmate(x2,y2);
                 
#if (DEBUG)
if (!illegalMove)
	{
		                writeChessboardToDEBUGint(positionInt);
                writeChessboardToDEBUGChar(positionChar, capturePiece, castling, promotion); 
	}
#endif
                if (!illegalMove) 
                {
                    // Write the current position to the chessboard graphic in the GUI
                    //ChessboardGraphic(bordersChessboard, bordersCapturedPiecesWhite, bordersCapturedPiecesBlack, x1, x2, x3, y1, y2, y3, castling, enPassant, promotion, capturePiece);

                    if (blackSide || SelectedMode == Mode.RobotVsRobot || SelectedMode == Mode.Manual)
                    {
                        //// Read the current value from the robotcontroller
                        rapidBoolcapturePiece = (ABB.Robotics.Controllers.RapidDomain.Bool)rdcapturePiece.Value;
                        rapidBoolwaitForTurn = (ABB.Robotics.Controllers.RapidDomain.Bool)rdwaitForTurn.Value;
                        rapidBoolEnPassantActive = (ABB.Robotics.Controllers.RapidDomain.Bool)rdEnPassantActive.Value;
                        rapidNumxCord1 = (ABB.Robotics.Controllers.RapidDomain.Num)rdxCoord1.Value;
                        rapidNumyCord1 = (ABB.Robotics.Controllers.RapidDomain.Num)rdyCoord1.Value;
                        rapidNumxCord2 = (ABB.Robotics.Controllers.RapidDomain.Num)rdxCoord2.Value;
                        rapidNumyCord2 = (ABB.Robotics.Controllers.RapidDomain.Num)rdyCoord2.Value;
                        rapidNumxEnPassant = (ABB.Robotics.Controllers.RapidDomain.Num)rdxEnPassant.Value;
                        rapidNumyEnPassant = (ABB.Robotics.Controllers.RapidDomain.Num)rdyEnPassant.Value;
                        rapidStringCastlingState = (ABB.Robotics.Controllers.RapidDomain.String)rdCastlingState.Value;
                        rapidStringqPromotion = (ABB.Robotics.Controllers.RapidDomain.String)rdqPromotion.Value;
                        rapidStringvictory = (ABB.Robotics.Controllers.RapidDomain.String)rdvictory.Value;


                        // New values to be written to the robotcontroller
                        rapidBoolcapturePiece.Value = capturePiece;
                        rapidBoolwaitForTurn.Value = false;
                        rapidBoolEnPassantActive.Value = enPassant;
                        rapidNumxCord1.Value = x1;
                        rapidNumyCord1.Value = y1;
                        rapidNumxCord2.Value = x2;
                        rapidNumyCord2.Value = y2;
                        rapidNumxEnPassant.Value = x3;
                        rapidNumyEnPassant.Value = y3;
                        rapidStringCastlingState.Value = castling;
                        rapidStringqPromotion.Value = promotion;
                        rapidStringvictory.Value = victory;

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
                        rdwaitForTurn.Value = rapidBoolwaitForTurn;
                        rdCastlingState.Value = rapidStringCastlingState;
                        rdqPromotion.Value = rapidStringqPromotion;
                        rdvictory.Value = rapidStringvictory;
                        //Release mastership as soon as possible
                        master.ReleaseOnDispose = true;
                        master.Dispose();

                    }

                    if (SelectedMode == Mode.RobotVsRobot)
                    {
                        ewhWaitForRobotToFinish.WaitOne();
                        ewhChessWork.Set();
                    }
                    else if (SelectedMode == Mode.HumanVsRobot)
                    {
                        if (blackSide)
                            ewhWaitForRobotToFinish.WaitOne();
                        if (!blackSide)
                            ewhChessWork.Set();
                    }


                    if (blackSide)
                        blackSide = false;
                    else
                        blackSide = true;

                    ewhSafeNewGame_1.Set();
                    ewhSafeNewGame_2.WaitOne();
                }
                else if (illegalMove)
                {
                    
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                    useThisPicture = false;
                    illegalMove = false;
                    ChangeColorToWhite();
                    tbNextMove.Clear();

                    System.Media.SystemSounds.Exclamation.Play();

                    MessageBoxResult result = MessageBox.Show(this, "Illegal move! Do you want the robot to move yor piece back for you?", "Illegal move!", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.Yes);
                    if (result == MessageBoxResult.Yes)
                    {
                        myChessEngine.decodingChessMoveToCoordinates(out x1, out y1, out x2, out y2, humanMoveFromScorpion);

                        //// Read the current value from the robotcontroller
                        rapidBoolwaitForTurn = (ABB.Robotics.Controllers.RapidDomain.Bool)rdwaitForTurn.Value;
                        rapidNumxCord1 = (ABB.Robotics.Controllers.RapidDomain.Num)rdxCoord1.Value;
                        rapidNumyCord1 = (ABB.Robotics.Controllers.RapidDomain.Num)rdyCoord1.Value;
                        rapidNumxCord2 = (ABB.Robotics.Controllers.RapidDomain.Num)rdxCoord2.Value;
                        rapidNumyCord2 = (ABB.Robotics.Controllers.RapidDomain.Num)rdyCoord2.Value;

                        // New values to be written to the robotcontroller
                        rapidNumxCord1.Value = x2;
                        rapidNumyCord1.Value = y2;
                        rapidNumxCord2.Value = x1;
                        rapidNumyCord2.Value = y1;
                        rapidBoolwaitForTurn.Value = false;

                        //Request mastership of Rapid before writing to the controller
                        master = Mastership.Request(controller.Rapid);
                        //Change: controller is repaced by aController
                        rdxCoord1.Value = rapidNumxCord1;
                        rdyCoord1.Value = rapidNumyCord1;
                        rdxCoord2.Value = rapidNumxCord2;
                        rdyCoord2.Value = rapidNumyCord2;
                        rdwaitForTurn.Value = rapidBoolwaitForTurn;

                        //Release mastership as soon as possible
                        master.ReleaseOnDispose = true;
                        master.Dispose();

                    }
                    else if (result == MessageBoxResult.No)
                    {

                    }
                    }));
                    //
                    // SEND TIL RAPID
                    //
                }
            }
        }

        private string Checkmate(int x1, int y1)
        {
            string victory = string.Empty;

            if (myChessEngine.BestMove == "mate")
            {
                victory = myChessEngine.ColorMoved(x1, y1);

                System.Media.SystemSounds.Exclamation.Play();
                this.Dispatcher.Invoke((Action)(() =>
                {
                MessageBox.Show(this, "CHECKMATE!");
                }));
                ewhWaitForRobotToFinish.Set();
               
            }

            return victory;
        }

        private bool Draw()
        {
            // Checks if the same move is repeated 3 times
            if (myChessEngine.EarlierMoves.Length >= 12)
            {
                string firstMatch;
                string secondMatch;
                string thirdMatch;

                firstMatch = Regex.Match(myChessEngine.EarlierMoves, @".{5}$").Value;
                firstMatch.TrimEnd();

                secondMatch = Regex.Match(myChessEngine.EarlierMoves, @".{10}$").Value;
                secondMatch = Regex.Match(secondMatch, @"^.{4}").Value;

                thirdMatch = Regex.Match(myChessEngine.EarlierMoves, @".{15}$").Value;
                thirdMatch = Regex.Match(thirdMatch, @"^.{4}").Value;

                if (firstMatch == secondMatch && secondMatch == thirdMatch)
                {
                    System.Media.SystemSounds.Exclamation.Play();
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                    MessageBox.Show(this, "CHECKMATE!");
                    }));
                    ewhWaitForRobotToFinish.Set();
                    return true;
                }
            }

            return false;
        }

        private void ScoreAnimation(double score)
        {
            if (score <= 0)
                score = 0.00001;

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
        /// <param name="bordersChessboard"></param>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <param name="castling"></param>
        private void ChessboardGraphic(Border[,] bordersChessboard, Border[,] bordersCapturedPiecesWhite, Border[,] bordersCapturedPiecesBlack, int x1, int x2, int x3, int y1, int y2, int y3, string castling, bool enPasant, string promotion, bool capturePiece)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                if (castling == "" && !enPasant)
                {
                    Image image = new Image();
                    Image image2 = new Image();

                    // If capturePiece is true, copy image
                    if (capturePiece)
                        image2 = (Image)bordersChessboard[y2, x2].Child;


                    // Delete the piece in the field its being moved to
                    bordersChessboard[y2, x2].Child = null;
                    // Copy the piece that is being moved
                    image = (Image)bordersChessboard[y1, x1].Child;
                    // delete the piece in old position
                    bordersChessboard[y1, x1].Child = null;
                    // Paste piece in new position
                    bordersChessboard[y2, x2].Child = image;


                    //Puts the captured piece in the capturepiece grid
                    if (capturePiece)
                    {
                        if (blackSide)
                        {
                            foreach (Border border in bordersCapturedPiecesWhite)
                            {
                                if (border.Child == null)
                                {
                                    border.Child = image2;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            foreach (Border border in bordersCapturedPiecesBlack)
                            {
                                if (border.Child == null)
                                {
                                    border.Child = image2;
                                    break;
                                }
                            }
                        }
                    }

                    //////////////////////////////////////////////////////////// Changes the color of the squares to the piece that has moved

                    PieceMovedColor(bordersChessboard, x1, x2, y1, y2);

                }
                else if (castling == "WShort")
                {
                    Image image = new Image();
                    // Copy the image of the king 
                    image = (Image)bordersChessboard[0, 4].Child;
                    // Delete the old picture
                    bordersChessboard[0, 4].Child = null;
                    // Paste king in new position
                    bordersChessboard[0, 6].Child = image;
                    // Copy picture of rook
                    image = (Image)bordersChessboard[0, 7].Child;
                    // Delete old picture of rook
                    bordersChessboard[0, 7].Child = null;
                    // Paste rook in new position
                    bordersChessboard[0, 5].Child = image;

                    //////////////////////////////////////////////////////////// Changes the color of the squares to the piece that has moved
                    PieceMovedColor(bordersChessboard, x1, x2, y1, y2);

                }
                else if (castling == "WLong")
                {
                    Image image = new Image();
                    image = (Image)bordersChessboard[0, 4].Child;
                    bordersChessboard[0, 4].Child = null;
                    bordersChessboard[0, 2].Child = image;
                    image = (Image)bordersChessboard[0, 0].Child;
                    bordersChessboard[0, 0].Child = null;
                    bordersChessboard[0, 3].Child = image;


                    //////////////////////////////////////////////////////////// Changes the color of the squares to the piece that has moved
                    PieceMovedColor(bordersChessboard, x1, x2, y1, y2);
                }
                else if (castling == "BShort")
                {
                    Image image = new Image();
                    image = (Image)bordersChessboard[7, 4].Child;
                    bordersChessboard[7, 4].Child = null;
                    bordersChessboard[7, 6].Child = image;
                    image = (Image)bordersChessboard[7, 7].Child;
                    bordersChessboard[7, 7].Child = null;
                    bordersChessboard[7, 5].Child = image;


                    //////////////////////////////////////////////////////////// Changes the color of the squares to the piece that has moved
                    PieceMovedColor(bordersChessboard, x1, x2, y1, y2);
                }
                else if (castling == "BLong")
                {
                    Image image = new Image();
                    image = (Image)bordersChessboard[7, 4].Child;
                    bordersChessboard[7, 4].Child = null;
                    bordersChessboard[7, 2].Child = image;
                    image = (Image)bordersChessboard[7, 0].Child;
                    bordersChessboard[7, 0].Child = null;
                    bordersChessboard[7, 3].Child = image;


                    //////////////////////////////////////////////////////////// Changes the color of the squares to the piece that has moved
                    PieceMovedColor(bordersChessboard, x1, x2, y1, y2);
                }

                if (enPasant)
                {
                    Image image = new Image();
                    Image image2 = new Image();
                    image2 = (Image)bordersChessboard[y3, x3].Child;

                    // Delete the piece in the field its being moved to
                    bordersChessboard[y2, x2].Child = null;
                    // Copy the piece that is being moved
                    image = (Image)bordersChessboard[y1, x1].Child;
                    // delete the piece in old position
                    bordersChessboard[y1, x1].Child = null;
                    // Paste piece in new position
                    bordersChessboard[y2, x2].Child = image;
                    // Delete piece en pasant
                    bordersChessboard[y3, x3].Child = null;

                    //Puts the captured piece in the capturepiece grid
                    if (blackSide)
                    {
                        foreach (Border border in bordersCapturedPiecesWhite)
                        {
                            if (border.Child == null)
                            {
                                border.Child = image2;
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (Border border in bordersCapturedPiecesBlack)
                        {
                            if (border.Child == null)
                            {
                                border.Child = image2;
                                break;
                            }
                        }
                    }

                    //////////////////////////////////////////////////////////// Changes the color of the squares to the piece that has moved
                    PieceMovedColor(bordersChessboard, x1, x2, y1, y2);
                }

                if (promotion == "WPromotion")
                {
                    if (!secondWPromotion)
                    {
                        // delete the piece in old position
                        bordersChessboard[y1, x1].Child = null;
                        // Paste piece in new position
                        bordersChessboard[y2, x2].Child = Imagewq1;
                    }
                    else
                    {
                        // delete the piece in old position
                        bordersChessboard[y1, x1].Child = null;
                        // Paste piece in new position
                        bordersChessboard[y2, x2].Child = Imagewq2;
                    }
                    secondWPromotion = true;

                }
                else if (promotion == "BPromotion")
                {


                    if (!secondBPromotion)
                    {
                        // delete the piece in old position
                        bordersChessboard[y1, x1].Child = null;
                        // Paste piece in new position
                        bordersChessboard[y2, x2].Child = Imagebq1;
                    }
                    else
                    {
                        // delete the piece in old position
                        bordersChessboard[y1, x1].Child = null;
                        // Paste piece in new position
                        bordersChessboard[y2, x2].Child = Imagebq2;
                    }

                    secondBPromotion = true;
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
            if (!firstTimeTrough)
            {
                // Dette er funkjsonen som leser den boolske verdien for at roboten er ferdig å flytte. Kommenterer den vekk midlertidig når eg tester ut nye funksjoner.
                if (SelectedMode == Mode.RobotVsRobot || SelectedMode == Mode.HumanVsRobot)
                {
                    ABB.Robotics.Controllers.RapidDomain.Bool rapidBoolwaitForTurn;
                    RapidData rdwaitForTurn = (RapidData)sender;
                    rapidBoolwaitForTurn = (ABB.Robotics.Controllers.RapidDomain.Bool)rdwaitForTurn.Value;
                    bool variabel = rapidBoolwaitForTurn.Value;

                    if (variabel)
                    {
                        ewhWaitForRobotToFinish.Set();
                    }
                }
            }
            firstTimeTrough = false;
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
            ewhNewGame.Set();
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
            cbMode.Visibility = Visibility.Hidden;
            lblHumanRobotTurn.Visibility = Visibility.Hidden;
            lblShowMode.Visibility = Visibility.Hidden;
            lblMode.Visibility = Visibility.Hidden;
            lblSkillLevel.Visibility = Visibility.Hidden;
            lblDepth.Visibility = Visibility.Hidden;
            lblMode.Visibility = Visibility.Hidden;
            lblMode1.Visibility = Visibility.Hidden;
            lstControllerInformation.Visibility = Visibility.Hidden;

            if (SelectedMode == Mode.RobotVsRobot || SelectedMode == Mode.HumanVsRobot)
            {
                tbNextMove.Visibility = Visibility.Hidden;
                btnSend.Visibility = Visibility.Hidden;
            }

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
                cbMode.Visibility = Visibility.Visible;
                lblHumanRobotTurn.Visibility = Visibility.Visible;
                lblShowMode.Visibility = Visibility.Visible;
                lblMode.Visibility = Visibility.Visible;
                lblSkillLevel.Visibility = Visibility.Visible;
                lblDepth.Visibility = Visibility.Visible;
                lblMode.Visibility = Visibility.Visible;
                lblMode1.Visibility = Visibility.Visible;
                lstControllerInformation.Visibility = Visibility.Visible;
                tbNextMove.Visibility = Visibility.Visible;
                btnSend.Visibility = Visibility.Visible;
            }

            if (e.Key == Key.Enter)
            {
                sendMove();
            }
        }


        void cbMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            if (cb.SelectedIndex == (int)Mode.HumanVsRobot)
            {
                SelectedMode = Mode.HumanVsRobot;
            }
            else if (cb.SelectedIndex == (int)Mode.RobotVsRobot)
            {
                SelectedMode = Mode.RobotVsRobot;
            }
            else if (cb.SelectedIndex == (int)Mode.Manual)
            {
                SelectedMode = Mode.Manual;
            }

        }

        private void sPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if ((SelectedMode == Mode.HumanVsRobot) && !waitForTimer)
            {
                waitForTimer = true;
                myTimer.Start();

                if (!useThisPicture)
                {
                    useThisPicture = true;
                    ChangeColorToGreen();
                    sPort.DiscardInBuffer();
                }
                else
                {
                    SerialPort sp = (SerialPort)sender;
                    visionSystemInData = sp.ReadExisting();
                    //splits the data into array
                    string[] parameters = Regex.Split(visionSystemInData, @"#");

                    List<int> axisValues = new List<int>();
                    int axisSwitch = 0;
                    string coordinates = parameters[0];
                    string squareOneEmpty = parameters[1];
                    int squareOneValue = 0;
                    int squareTwoValue = 0;
                    int shortCastlingValue = 0;
                    int longCastlingValue = 0;
                    string squareTwoEmpty = parameters[2];
                    string shortCastling = parameters[3];
                    string longCastling = parameters[4];

                    string Move = "";


                    // Takes out coordinates
                    while (Regex.IsMatch(coordinates, @"\d{1,3}(?=\.)"))
                    {
                        Match match = Regex.Match(coordinates, @"\d{1,3}(?=\.)");
                        axisValues.Add(Convert.ToInt16(match.Value));
                        coordinates = coordinates.Remove(match.Index, match.Length);
                    }

                    while (Regex.IsMatch(squareOneEmpty, @"\d{1,3}"))
                    {
                        Match match = Regex.Match(squareOneEmpty, @"\d{1,3}");
                        squareOneValue += Convert.ToInt16(match.Value);
                        squareOneEmpty = squareOneEmpty.Remove(match.Index, match.Length);
                    }

                    while (Regex.IsMatch(squareTwoEmpty, @"\d{1,3}"))
                    {
                        Match match = Regex.Match(squareTwoEmpty, @"\d{1,3}");
                        squareTwoValue += Convert.ToInt16(match.Value);
                        squareTwoEmpty = squareTwoEmpty.Remove(match.Index, match.Length);
                    }

                    while (Regex.IsMatch(shortCastling, @"\d{1,3}"))
                    {
                        Match match = Regex.Match(shortCastling, @"\d{1,3}");
                        shortCastlingValue += Convert.ToInt16(match.Value);
                        shortCastling = shortCastling.Remove(match.Index, match.Length);
                    }

                    while (Regex.IsMatch(longCastling, @"\d{1,3}"))
                    {
                        Match match = Regex.Match(longCastling, @"\d{1,3}");
                        longCastlingValue += Convert.ToInt16(match.Value);
                        longCastling = longCastling.Remove(match.Index, match.Length);
                    }



                    // Checks if any special cases have accured

                    if ((shortCastlingValue != 0) && (shortCastlingValue > 150))
                    {
                        Move = "e1g1";
                        MoveToGUI(Move);
                    }
                    else if ((longCastlingValue != 0) && (longCastlingValue > 150))
                    {
                        Move = "e1c1";
                        MoveToGUI(Move);
                    }
                    else if ((squareOneValue == 0) && (squareTwoValue == 0))
                    {
                        System.Media.SystemSounds.Exclamation.Play();
                        useThisPicture = false;
                        ChangeColorToWhite();

                        this.Dispatcher.Invoke((Action)(() =>
                        {
                        MessageBox.Show(this, "Empty square not detected. Please add the empty square to its respective template in Scorpion Vision Software and redo the move");
                        }));
                    }
                    else if (axisValues.Count < 4)
                    {
                        System.Media.SystemSounds.Exclamation.Play();
                        useThisPicture = false;
                        ChangeColorToWhite();

                        this.Dispatcher.Invoke((Action)(() =>
                        {
                        MessageBox.Show(this,"One of the moves is not detected. Please check Scorpion Vision Software, correct the error, and redo the move");
                        }));
                    }
                    else if (axisValues.Count >= 4)
                    {
                        if (squareOneValue != 0 && squareTwoValue != 0)
                        {
                            if (squareOneValue > squareTwoValue)
                            {
                                //Order is correct
                            }
                            else
                            {
                                // Order is backwards. Switch the two coordinates
                                if (axisValues.Count >= 4)
                                {
                                    axisSwitch = axisValues[0];
                                    axisValues[0] = axisValues[2];
                                    axisValues[2] = axisSwitch;

                                    axisSwitch = axisValues[1];
                                    axisValues[1] = axisValues[3];
                                    axisValues[3] = axisSwitch;
                                }
                            }
                        }
                        else if (squareOneValue != 0)
                        {
                            //Order is correct
                        }
                        else if (squareTwoValue != 0)
                        {
                            // Order is backwards. Switch the two coordinates
                            if (axisValues.Count >= 4)
                            {
                                axisSwitch = axisValues[0];
                                axisValues[0] = axisValues[2];
                                axisValues[2] = axisSwitch;

                                axisSwitch = axisValues[1];
                                axisValues[1] = axisValues[3];
                                axisValues[3] = axisSwitch;
                            }
                        }
                        else
                        {
                            //No match for empty square
                        }

                        // Turns coordinates into UCI standard
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

                        // Check for queen promotion
                        if (Move == "a7a8" || Move == "b7b8" || Move == "c7c8" || Move == "d7d8" || Move == "e7e8" || Move == "f7f8" || Move == "g7g8" || Move == "h7h8")
                        {
                            if (Move == "a7a8")
                            {
                                if (myChessEngine.ChessboardChar[6, 0] == 'p')
                                    Move += "q";
                            }
                            else if (Move == "b7b8")
                            {
                                if (myChessEngine.ChessboardChar[6, 1] == 'p')
                                    Move += "q";
                            }
                            else if (Move == "c7c8")
                            {
                                if (myChessEngine.ChessboardChar[6, 2] == 'p')
                                    Move += "q";
                            }
                            else if (Move == "d7d8")
                            {
                                if (myChessEngine.ChessboardChar[6, 3] == 'p')
                                    Move += "q";
                            }
                            else if (Move == "e7e8")
                            {
                                if (myChessEngine.ChessboardChar[6, 4] == 'p')
                                    Move += "q";
                            }
                            else if (Move == "f7f8")
                            {
                                if (myChessEngine.ChessboardChar[6, 5] == 'p')
                                    Move += "q";
                            }
                            else if (Move == "g7g8")
                            {
                                if (myChessEngine.ChessboardChar[6, 6] == 'p')
                                    Move += "q";
                            }
                            else if (Move == "h7h8")
                            {
                                if (myChessEngine.ChessboardChar[6, 7] == 'p')
                                    Move += "q";
                            }
                        }

                        MoveToGUI(Move);
                    }
                }
            }
        }

        private void MoveToGUI(string Move)
        {
            Debug.WriteLine(Move);
            humanMoveFromScorpion = Move;
            ewhChessWork.Set();
            useThisPicture = false;
            ChangeColorToWhite();
        }

        private void newGame(object obj)
        {
            //declare a variable of data type RapidDomain.Bool
            ABB.Robotics.Controllers.RapidDomain.Bool rapidBoolnewGame;

            //Make a variable that is connected to the variable in the robotcontroller
            ABB.Robotics.Controllers.RapidDomain.RapidData rdnewGame = controller.Rapid.GetRapidData("T_ROB1", "Chess_Robot_RAPID_final", "newGame");

            while (true)
            {
                ewhNewGame.WaitOne();

                // Use for safe new game
                if (!checkmate)
                {
                    ewhSafeNewGame_2.Reset();
                    ewhSafeNewGame_1.WaitOne();
                }
                ewhChessWork.Reset();
                ewhWaitForRobotToFinish.Reset();

                myChessEngine.NewGame();

                blackSide = false;
                useThisPicture = false;
                ChangeColorToWhite();
                bestMoveWhiteSide = "e2e4";
                checkmate = false;

                this.Dispatcher.Invoke((Action)(() =>
                {
                    // reset score
                    lblBlackScore.Content = "-0,2";
                    lblWhiteScore.Content = "0,2";
                    ScoreAnimation(0.492);

                    //Reset best move
                    lblBestMove.Content = string.Empty;
                    lblMoveNumber.Content = 0;

                    // Reset moves made 
                    lblMovesMade.Content = string.Empty;

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

                    foreach (Border border in gridCapturedPiecesWhite.Children)
                    {
                        if (border != null)
                            border.Child = null;
                    }

                    foreach (Border border in gridCapturedPiecesBlack.Children)
                    {
                        if (border != null)
                            border.Child = null;
                    }

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

                }));
                // Reset counters in RAPID code



                //// Read the current value from the robotcontroller
                rapidBoolnewGame = (ABB.Robotics.Controllers.RapidDomain.Bool)rdnewGame.Value;

                // New values to be written to the robotcontroller
                rapidBoolnewGame.Value = true;

                ABB.Robotics.Controllers.RapidDomain.Task[] taskCol = controller.Rapid.GetTasks();

                //Request mastership of Rapid before writing to the controller
                master = Mastership.Request(controller.Rapid);
                //Change: controller is repaced by aController
                rdnewGame.Value = rapidBoolnewGame;

                // PP to Main
                foreach (ABB.Robotics.Controllers.RapidDomain.Task atask in taskCol)
                {
                    atask.ResetProgramPointer();
                    atask.Start();
                }

                //Release mastership as soon as possible
                master.ReleaseOnDispose = true;
                master.Dispose();

                this.Dispatcher.Invoke((Action)(() =>
                {
                    cbMode.IsEnabled = true;
                    cbDepth.IsEnabled = true;
                    cbSkillLevel.IsEnabled = true;
                    cbChessboardStyle.IsEnabled = true;
                }));

                ewhSafeNewGame_2.Set();
            }
        }

        private void tbNextMove_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Regex.IsMatch(tbNextMove.Text.ToLower(), @"[a-h1-8]"))
                tbNextMove.Clear();
        }

        private void elliUseThisPicture_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            useThisPicture = false;

            ChangeColorToWhite();

        }

        private void ChangeColorToGreen()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                SolidColorBrush mySolidColorBrush = new SolidColorBrush();
                mySolidColorBrush.Color = Color.FromRgb(23, 255, 0);
                elliUseThisPicture.Fill = mySolidColorBrush;
            }));
        }

        private void ChangeColorToWhite()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                SolidColorBrush mySolidColorBrush = new SolidColorBrush();
                mySolidColorBrush.Color = Color.FromRgb(255, 255, 255);
                elliUseThisPicture.Fill = mySolidColorBrush;
            }));
        }


        void myTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            waitForTimer = false;
            myTimer.Stop();
        }

        private void lblHumanRobotTurn_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            NewPlayerNameWindow NewPlayerNameWindow = new NewPlayerNameWindow();
            NewPlayerNameWindow.Show();
        }


        public void UpdateName(string name)
        {
            this.PlayerName = name;
            if(!blackSide)
            lblHumanRobotTurn.Content = name;
        }

        private void Border_h1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "h1";
        }

        private void Border_g1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "g1";
        }

        private void Border_f1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "f1";
        }

        private void Border_e1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "e1";
        }

        private void Border_d1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "d1";
        }

        private void Border_c1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "c1";
        }

        private void Border_b1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "b1";
        }

        private void Border_a1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "a1";
        }

        private void Border_h2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "h2";
        }

        private void Border_g2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "g2";
        }

        private void Border_f2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "f2";
        }

        private void Border_e2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "e2";
        }

        private void Border_d2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "d2";
        }

        private void Border_c2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "c2";
        }

        private void Border_b2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "b2";
        }

        private void Border_a2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "a2";
        }

        private void Border_h3_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "h3";
        }

        private void Border_g3_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "g3";
        }

        private void Border_f3_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "f3";
        }

        private void Border_e3_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "e3";
        }

        private void Border_d3_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "d3";
        }

        private void Border_c3_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "c3";
        }

        private void Border_b3_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "b3";
        }

        private void Border_a3_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "a3";
        }

        private void Border_h4_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "h4";
        }

        private void Border_g4_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "g4";
        }

        private void Border_f4_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "f4";
        }

        private void Border_e4_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "e4";
        }

        private void Border_d4_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "d4";
        }

        private void Border_c4_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "c4";
        }

        private void Border_b4_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "b4";
        }

        private void Border_a4_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "a4";
        }

        private void Border_h5_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "h5";
        }

        private void Border_g5_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "g5";
        }

        private void Border_f5_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "f5";
        }

        private void Border_e5_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "e5";
        }

        private void Border_d5_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "d5";
        }

        private void Border_c5_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "c5";
        }

        private void Border_b5_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "b5";
        }

        private void Border_a5_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "a5";
        }

        private void Border_h6_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "h6";
        }

        private void Border_g6_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "g6";
        }

        private void Border_f6_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "f6";
        }

        private void Border_e6_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "e6";
        }

        private void Border_d6_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "d6";
        }

        private void Border_c6_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "c6";
        }

        private void Border_b6_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "b6";
        }

        private void Border_a6_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "a6";
        }

        private void Border_h7_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "h7";
        }

        private void Border_g7_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "g7";
        }

        private void Border_f7_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "f7";
        }

        private void Border_e7_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "e7";
        }

        private void Border_d7_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "d7";
        }

        private void Border_c7_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "c7";
        }

        private void Border_b7_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "b7";
        }

        private void Border_a7_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "a7";
        }

        private void Border_h8_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "h8";
        }

        private void Border_g8_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "g8";
        }

        private void Border_f8_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "f8";
        }

        private void Border_e8_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "e8";
        }

        private void Border_d8_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "d8";
        }

        private void Border_c8_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "c8";
        }

        private void Border_b8_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "b8";
        }

        private void Border_a8_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbNextMove.Text += "a8";
        }


    }// MainWindow
}


