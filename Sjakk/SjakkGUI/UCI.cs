using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace SjakkGUI
{
   
    public class UCI
    {
        /// <summary>
        /// Gets set when the best move is found
        /// </summary>
        public EventWaitHandle ewhCalculating = new EventWaitHandle(false, EventResetMode.AutoReset);
        string bestMove = string.Empty;
        string considering = string.Empty;
        string lastMove = string.Empty;
        string depth = "20";
        string earlierMoves = String.Empty;

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

            //DEBUG
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


            if (t.Contains("bestmove"))
            {
                String bestmove = t.Substring(9, 4);
                this.BestMove = t.Substring(9, 4);
                ewhCalculating.Set();
            }
            else if (t.Contains(" pv "))
            {
                String considerering = t;
                Int32 length = considerering.Length;
                Int32 idxofline = considerering.IndexOf(" pv ") + 4;
                this.Considering += considerering.Substring(idxofline, length - idxofline) + "\n";
            }
        }

        public void EngineCommand(String cmd)
        {
            if (UCI_Engine != null)
                UCI_Engine.StandardInput.WriteLine(cmd);
        }

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
       
        public void decodingChessMoveToCoordinates(out int x1, out int y1, out int x2, out int y2, out bool takePiece, out int [,]positionInt, out char [,]positionChar, out string castling)
        {
            x1 = 8;
            x2 = 8;
            y1 = 8;
            y2 = 8;
            takePiece = false;

            string chessMove = this.BestMove;

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
                    chessboardChar[y2, x2] = chessboardChar[y1, x1];
                    chessboardChar[y1, x1] = ' ';
                    positionChar = chessboardChar;

                    // Write what chesspiece that has been moved to the chessboardmatrix
                    chessboardInt[y1, x1] = 0;
                    if (chessboardInt[y2, x2] == 1)
                        takePiece = true;
                    chessboardInt[y2, x2] = 1;
                    positionInt = chessboardInt;

                    castling = "";
                }
            }
            else
            {

                chessboardChar[y2, x2] = chessboardChar[y1, x1];
                chessboardChar[y1, x1] = ' ';
                positionChar = chessboardChar;



                // Write what chesspiece that has been moved to the chessboardmatrix
                chessboardInt[y1, x1] = 0;
                if (chessboardInt[y2, x2] == 1)
                    takePiece = true;
                chessboardInt[y2, x2] = 1;
                positionInt = chessboardInt;

                castling = "";
            }
 
        }


        #endregion
    }
}
