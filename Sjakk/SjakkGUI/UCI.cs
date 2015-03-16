using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

namespace SjakkGUI
{
   
    public class UCI
    {
        /// <summary>
        /// Gets set when the best move is found
        /// </summary>
        public EventWaitHandle ewhCalculating = new EventWaitHandle(false, EventResetMode.AutoReset);
        string bestMove = string.Empty;
        char lastPieceMoved = ' ';
        string considering = string.Empty;
        string lastMove = string.Empty;
        string depth = "20";
        string earlierMoves = string.Empty;
        // The current score
        string score = string.Empty;


        int[,] chessboardInt = new int[8, 8] {{ 1 , 1 , 1 , 1 , 1 , 1 , 1 , 1 },
                                           { 1 , 1 , 1 , 1 , 1 , 1 , 1 , 1 },
 				                           { 0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 },
                                           { 0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 },
                                           { 0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 },
                                           { 0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 },
                                           { 1 , 1 , 1 , 1 , 1 , 1 , 1 , 1 },
                                           { 1 , 1 , 1 , 1 , 1 , 1 , 1 , 1 }};

        char[,] chessboardChar = new char[8, 8] {{ 'r' , 'n' , 'b' , 'q' , 'k' , 'b' , 'n' , 'r' },
                                                 { 'p' , 'p' , 'p' , 'p' , 'p' , 'p' , 'p' , 'p' },
 				                                 { ' ' , ' ' , ' ' , ' ' , ' ' , ' ' , ' ' , ' ' },
                                                 { ' ' , ' ' , ' ' , ' ' , ' ' , ' ' , ' ' , ' ' },
                                                 { ' ' , ' ' , ' ' , ' ' , ' ' , ' ' , ' ' , ' ' },
                                                 { ' ' , ' ' , ' ' , ' ' , ' ' , ' ' , ' ' , ' ' },
                                                 { 'P' , 'P' , 'P' , 'P' , 'P' , 'P' , 'P' , 'P' },
                                                 { 'R' , 'N' , 'B' , 'Q' , 'K' , 'B' , 'N' , 'R' }};


        public string EarlierMoves
        {
            get { return earlierMoves; }
            set { earlierMoves = value; }
        }

        public string Depth
        {
            get { return depth; }
            set { depth = value; }
        }


        public string LastMove
        {
            get { return lastMove; }
            set { lastMove = value; }
        }



        public string Considering
        {
            get { return considering; }
            set { considering = value; }
        }


        public String BestMove
        {
            get { return bestMove; }
            set { bestMove = value; }
        }

        public string Score
        {
            get { return score; }
            set { score = value; }
        }

        //////////////////////////////////////////////////////////////////////////
        // CONSTANTS
        //////////////////////////////////////////////////////////////////////////
        static String kSetUCIMode = "uci";
        static String kResetEngine = "ucinewgame";
        static String kStopEngine = "stop";
        static String kQuitEngine = "quit";
        static String kSetPosition = "position ";
        static String kStartPosition = "startpos ";
        static String kStartMoves = "moves ";
        static String kGo = "go ";
        static String kDepth = "depth ";
        static String kStartMovesFromStartPos = kSetPosition + kStartPosition + kStartMoves;

        //////////////////////////////////////////////////////////////////////////
        // Members
        //////////////////////////////////////////////////////////////////////////
        Process UCI_Engine;

        //////////////////////////////////////////////////////////////////////////
        // Public methods
        //////////////////////////////////////////////////////////////////////////
        #region Public methods


        public bool InitEngine(String enginePath, String engineIniPath)
        {
            // create process
            UCI_Engine = new Process();
            UCI_Engine.StartInfo.FileName = enginePath;
            UCI_Engine.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(enginePath);
            UCI_Engine.StartInfo.UseShellExecute = false;
            UCI_Engine.StartInfo.CreateNoWindow = true;
            UCI_Engine.StartInfo.RedirectStandardInput = true;
            UCI_Engine.StartInfo.RedirectStandardOutput = true;
            UCI_Engine.Start();
            UCI_Engine.OutputDataReceived += OutputDataReceivedProc;
            UCI_Engine.BeginOutputReadLine();

            // start new game
            EngineCommand(kSetUCIMode);

            //DEBUG write to debugfile
            //EngineCommand("setoption name Write Debug Log value true");

            ResetEngine();

            return true;
        }

        public bool ResetEngine()
        {
            // stop engine 
            EngineCommand(kStopEngine);

            // reset engine
            EngineCommand(kResetEngine);
            return true;
        }

        public bool ShutdownEngine()
        {
            if (UCI_Engine != null)
            {
                // stop kill STOP!
                EngineCommand(kStopEngine);
                EngineCommand(kQuitEngine);
                UCI_Engine.Kill();
            }

            return true;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////
        // Private methods
        //////////////////////////////////////////////////////////////////////////
        #region Private methods

        private void OutputDataReceivedProc(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine.Data == null)
                return;

            String t = outLine.Data;

            if (t.Contains(" cp "))
            {
                
            }

            if (Regex.IsMatch(t,@"(?<= cp )\d{1,3}"))
            {
                this.Score = Regex.Match(t, @"(?<= cp )\d{1,4}").Value;
            }
            else if (Regex.IsMatch(t,@"(?<= cp )-\d{1,3}"))
            {
                this.Score = Regex.Match(t, @"(?<= cp )-\d{1,4}").Value;
            }

            if (Regex.IsMatch(t, @"(?<=bestmove )(\w\d\w\d\w|\w\d\w\d)"))
            {
                this.BestMove = Regex.Match(t, @"(?<=bestmove )(\w\d\w\d\w|\w\d\w\d)").Value;
                ewhCalculating.Set();
            }

            //if (t.Contains("bestmove"))
            //{
            //    String bestmove = t.Substring(9, 4);
            //    this.BestMove = bestmove;
            //    ewhCalculating.Set();
            //}
            //else if (t.Contains(" pv "))
            //{
            //    String considerering = t;
            //    Int32 length = considerering.Length;
            //    Int32 idxofline = considerering.IndexOf(" pv ") + 4;
            //    this.Considering += considerering.Substring(idxofline, length - idxofline) + "\n";
            //}


        }

        public void EngineCommand(String cmd)
        {
            if (UCI_Engine != null)
                UCI_Engine.StandardInput.WriteLine(cmd);
        }

        /// <summary>
        /// The move that is being played
        /// </summary>
        /// <param name="nextMove"></param>
        public void EngineCommandMove(String nextMove)
        {
            if (UCI_Engine != null)
            {
                this.lastMove = nextMove;
                UCI_Engine.StandardInput.WriteLine(kStartMovesFromStartPos + this.earlierMoves + nextMove);
                UCI_Engine.StandardInput.WriteLine(kGo + kDepth + this.depth);
                if(nextMove != "")
                this.earlierMoves += nextMove + " ";
            }
        }

        public string GetEngineOutput()
        {
            if (UCI_Engine != null)
                 return UCI_Engine.StandardOutput.ReadToEnd();

            return null;
        }
       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x1">from x1</param>
        /// <param name="y1">from y1</param>
        /// <param name="x2">to x2</param>
        /// <param name="y2">to y2</param>
        /// <param name="x3">en pasant coordinates</param>
        /// <param name="y3">en pasant coordiantes</param>
        /// <param name="takePiece"></param>
        /// <param name="positionInt"></param>
        /// <param name="positionChar"></param>
        /// <param name="castling"></param>
        /// <param name="enPassant"></param>
        public void decodingChessMoveToCoordinates(out int x1, out int y1, out int x2, out int y2, out int x3, out int y3, out bool takePiece, out int [,]positionInt, out char [,]positionChar, out string castling, out bool enPassant, string chessMove)
        {
            x1 = 8;
            x2 = 8;
            y1 = 8;
            y2 = 8;
            x3 = 8;
            y3 = 8;
            takePiece = false;
            enPassant = false;
            castling = "";

            string lastChessMove = this.lastMove;

           char substring = Convert.ToChar(chessMove.Substring(0, 1));

            switch (substring)
            {
                case 'a':
                    x1 = 0;
                    break;
                case 'b':
                    x1 = 1;
                    break;
                case 'c':
                    x1 = 2;
                    break;
                case 'd':
                    x1 = 3;
                    break;
                case 'e':
                    x1 = 4;
                    break;
                case 'f':
                    x1 = 5;
                    break;
                case 'g':
                    x1 = 6;
                    break;
                case 'h':
                    x1 = 7;
                    break;
            }

            substring = Convert.ToChar(chessMove.Substring(1, 1));

            switch (substring)
            {
                case '1':
                    y1 = 0;
                    break;
                case '2':
                    y1 = 1;
                    break;
                case '3':
                    y1 = 2;
                    break;
                case '4':
                    y1 = 3;
                    break;
                case '5':
                    y1 = 4;
                    break;
                case '6':
                    y1 = 5;
                    break;
                case '7':
                    y1 = 6;
                    break;
                case '8':
                    y1 = 7;
                    break;
            }

            substring = Convert.ToChar(chessMove.Substring(2, 1));

            switch (substring)
            {
                case 'a':
                    x2 = 0;
                    break;
                case 'b':
                    x2 = 1;
                    break;
                case 'c':
                    x2 = 2;
                    break;
                case 'd':
                    x2 = 3;
                    break;
                case 'e':
                    x2 = 4;
                    break;
                case 'f':
                    x2 = 5;
                    break;
                case 'g':
                    x2 = 6;
                    break;
                case 'h':
                    x2 = 7;
                    break;
            }

            substring = Convert.ToChar(chessMove.Substring(3, 1));

            switch (substring)
            {
                case '1':
                    y2 = 0;
                    break;
                case '2':
                    y2 = 1;
                    break;
                case '3':
                    y2 = 2;
                    break;
                case '4':
                    y2 = 3;
                    break;
                case '5':
                    y2 = 4;
                    break;
                case '6':
                    y2 = 5;
                    break;
                case '7':
                    y2 = 6;
                    break;
                case '8':
                    y2 = 7;
                    break;
            }


            // Testing to se if there is castling
            if (chessboardChar[y1, x1] == 'k' || chessboardChar[y1, x1] == 'K')
            {
                if (chessMove == "e1g1")
                {
                    chessboardChar[0, 4] = ' ';
                    chessboardChar[0, 7] = ' ';
                    chessboardChar[0, 5] = 'r';
                    chessboardChar[0, 6] = 'k';
                    positionChar = chessboardChar;

                    chessboardInt[0, 4] = 0;
                    chessboardInt[0, 7] = 0;
                    chessboardInt[0, 5] = 1;
                    chessboardInt[0, 6] = 1;
                    positionInt = chessboardInt;

                    castling = "WShort";
                }
                else if (chessMove == "e1c1")
                {
                    chessboardChar[0, 4] = ' ';
                    chessboardChar[0, 0] = ' ';
                    chessboardChar[0, 3] = 'r';
                    chessboardChar[0, 2] = 'k';
                    positionChar = chessboardChar;

                    chessboardInt[0, 4] = 0;
                    chessboardInt[0, 0] = 0;
                    chessboardInt[0, 3] = 1;
                    chessboardInt[0, 2] = 1;
                    positionInt = chessboardInt;

                    castling = "WLong";
                }
                else if (chessMove == "e8g8")
                {
                    chessboardChar[7, 4] = ' ';
                    chessboardChar[7, 7] = ' ';
                    chessboardChar[7, 5] = 'R';
                    chessboardChar[7, 6] = 'K';
                    positionChar = chessboardChar;

                    chessboardInt[7, 4] = 0;
                    chessboardInt[7, 7] = 0;
                    chessboardInt[7, 5] = 1;
                    chessboardInt[7, 6] = 1;
                    positionInt = chessboardInt;

                    castling = "BShort";
                }
                else if (chessMove == "e8c8")
                {
                    chessboardChar[7, 4] = ' ';
                    chessboardChar[7, 0] = ' ';
                    chessboardChar[7, 3] = 'R';
                    chessboardChar[7, 2] = 'K';
                    positionChar = chessboardChar;

                    chessboardInt[7, 4] = 0;
                    chessboardInt[7, 0] = 0;
                    chessboardInt[7, 3] = 1;
                    chessboardInt[7, 2] = 1;
                    positionInt = chessboardInt;

                    castling = "BLong";
                }
                else
                {
                    takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);

                }
            }
            else if (lastPieceMoved == 'p' || lastPieceMoved == 'P') // Testing for en passant
                {
                    if (lastChessMove == "a2a4" || lastChessMove == "b2b4" || lastChessMove == "c2c4" || lastChessMove == "d2d4" || lastChessMove == "e2e4" || lastChessMove == "f2f4" || lastChessMove == "g2g4" || lastChessMove == "h2ah4" ||
                        lastChessMove == "a7a5" || lastChessMove == "b7b5" || lastChessMove == "c7c5" || lastChessMove == "d7d5" || lastChessMove == "e7e5" || lastChessMove == "f7f5" || lastChessMove == "g7g5" || lastChessMove == "h7h5")
                    {
                        if (chessboardChar[y1, x1] == 'p' || chessboardChar[y1, x1] == 'P')
                        {
                            switch (lastChessMove)
                            {
                                case "a2a4":
                                    if (chessMove == "b4a3")
                                    {
                                        enPassant = true;
                                        x3 = 0;
                                        y3 = 3;

                                        enPasantChessmoveToMatrix(x1, y1, x2, y2, x3, y3, out positionInt, out positionChar);
                                    }
                                    else
                                    takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                                case "b2b4":
                                    if (chessMove == "c4b3" || chessMove == "a4b3")
                                    {
                                        enPassant = true;
                                        x3 = 1;
                                        y3 = 3;

                                        enPasantChessmoveToMatrix(x1, y1, x2, y2, x3, y3, out positionInt, out positionChar);
                                    }
                                    else
                                        takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                                case "c2c4":
                                    if (chessMove == "d4c3" || chessMove == "b4c3")
                                    {
                                        enPassant = true;
                                        x3 = 2;
                                        y3 = 3;

                                        enPasantChessmoveToMatrix(x1, y1, x2, y2, x3, y3, out positionInt, out positionChar);
                                    }
                                    else
                                        takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                                case "d2d4":
                                    if (chessMove == "e4d3" || chessMove == "c4d3")
                                    {
                                        enPassant = true;
                                        x3 = 3;
                                        y3 = 3;

                                        enPasantChessmoveToMatrix(x1, y1, x2, y2, x3, y3, out positionInt, out positionChar);
                                    }
                                    else
                                        takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                                case "e2e4":
                                    if (chessMove == "f4e3" || chessMove == "d4e3")
                                    {
                                        enPassant = true;
                                        x3 = 4;
                                        y3 = 3;

                                        enPasantChessmoveToMatrix(x1, y1, x2, y2, x3, y3, out positionInt, out positionChar);
                                    }
                                    else
                                        takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                                case "f2f4":
                                    if (chessMove == "g4f3" || chessMove == "e4f3")
                                    {
                                        enPassant = true;
                                        x3 = 5;
                                        y3 = 3;

                                        enPasantChessmoveToMatrix(x1, y1, x2, y2, x3, y3, out positionInt, out positionChar);
                                    }
                                    else
                                        takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                                case "g2g4":
                                    if (chessMove == "h4g3" || chessMove == "f4g3")
                                    {
                                        enPassant = true;
                                        x3 = 6;
                                        y3 = 3;

                                        enPasantChessmoveToMatrix(x1, y1, x2, y2, x3, y3, out positionInt, out positionChar);
                                    }
                                    else
                                        takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                                case "h2h4":
                                    if (chessMove == "g4h3")
                                    {
                                        enPassant = true;
                                        x3 = 7;
                                        y3 = 3;

                                        enPasantChessmoveToMatrix(x1, y1, x2, y2, x3, y3, out positionInt, out positionChar);
                                    }
                                    else
                                        takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                                case "a7a5":
                                    if (chessMove == "b5a6")
                                    {
                                        enPassant = true;
                                        x3 = 0;
                                        y3 = 4;

                                        enPasantChessmoveToMatrix(x1, y1, x2, y2, x3, y3, out positionInt, out positionChar);
                                    }
                                    else
                                        takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                                case "b7b5":
                                    if (chessMove == "a5b6" || chessMove == "c5b6")
                                    {
                                        enPassant = true;
                                        x3 = 1;
                                        y3 = 4;

                                        enPasantChessmoveToMatrix(x1, y1, x2, y2, x3, y3, out positionInt, out positionChar);
                                    }
                                    else
                                        takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                                case "c7c5":
                                    if (chessMove == "b5c6" || chessMove == "d5d3")
                                    {
                                        enPassant = true;
                                        x3 = 2;
                                        y3 = 4;

                                        enPasantChessmoveToMatrix(x1, y1, x2, y2, x3, y3, out positionInt, out positionChar);
                                    }
                                    else
                                        takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                                case "d7d5":
                                    if (chessMove == "c5d6" || chessMove == "e5d6")
                                    {
                                        enPassant = true;
                                        x3 = 3;
                                        y3 = 4;

                                        enPasantChessmoveToMatrix(x1, y1, x2, y2, x3, y3, out positionInt, out positionChar);
                                    }
                                    else
                                        takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                                case "e7e5":
                                    if (chessMove == "d5e6" || chessMove == "f5e6")
                                    {
                                        enPassant = true;
                                        x3 = 4;
                                        y3 = 4;

                                        enPasantChessmoveToMatrix(x1, y1, x2, y2, x3, y3, out positionInt, out positionChar);
                                    }
                                    else
                                        takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                                case "f7f5":
                                    if (chessMove == "e5f6" || chessMove == "g5f6")
                                    {
                                        enPassant = true;
                                        x3 = 5;
                                        y3 = 4;

                                        enPasantChessmoveToMatrix(x1, y1, x2, y2, x3, y3, out positionInt, out positionChar);
                                    }
                                    else
                                        takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                                case "g7g5":
                                    if (chessMove == "h5g6" || chessMove == "h5g6")
                                    {
                                        enPassant = true;
                                        x3 = 6;
                                        y3 = 4;

                                        enPasantChessmoveToMatrix(x1, y1, x2, y2, x3, y3, out positionInt, out positionChar);
                                    }
                                    else
                                        takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                                case "h7h5":
                                    if (chessMove == "g5h6")
                                    {
                                        enPassant = true;
                                        x3 = 7;
                                        y3 = 4;

                                        enPasantChessmoveToMatrix(x1, y1, x2, y2, x3, y3, out positionInt, out positionChar);
                                    }
                                    else
                                        takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                                default:
                                    takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                                    break;
                            }
                        }
                        else
                        takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                    }
                    else
                    takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
                }
            else
            {
                takePiece = ChessmoveToMatrix(x1, y1, x2, y2, takePiece, out positionInt, out positionChar);
            }


            // Remembers the last move
            this.lastPieceMoved = chessboardChar[y2, x2];
        }

        private bool ChessmoveToMatrix(int x1, int y1, int x2, int y2, bool takePiece, out int[,] positionInt, out char[,] positionChar)
        {
            // Move the piece to new coordinates
            chessboardChar[y2, x2] = chessboardChar[y1, x1];
            // Remove piece from old coordinates
            chessboardChar[y1, x1] = ' ';
            // Copy marix to the output matrix
            positionChar = chessboardChar;

            // Write what chesspiece that has been moved to the chessboard matrix
            chessboardInt[y1, x1] = 0;
            if (chessboardInt[y2, x2] == 1)
                takePiece = true;
            chessboardInt[y2, x2] = 1;
            positionInt = chessboardInt;
            return takePiece;
        }

        private void enPasantChessmoveToMatrix(int x1, int y1, int x2, int y2, int x3, int y3, out int[,] positionInt, out char[,] positionChar)
        {
            // Move the piece to new coordinates
            chessboardChar[y2, x2] = chessboardChar[y1, x1];
            // Remove piece from old coordinates
            chessboardChar[y1, x1] = ' ';
            // Remove piece en pasant
            chessboardChar[y3, x3] = ' ';
            // Copy marix to the output matrix
            positionChar = chessboardChar;

            // salmost same as over
            chessboardInt[y1, x1] = 0;
            chessboardInt[y2, x2] = 1;
            chessboardInt[y3, x3] = 0;
            positionInt = chessboardInt;
        }


        #endregion
    }
}
