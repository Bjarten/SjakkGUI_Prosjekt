using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SjakkGUI
{
    public class UCI
    {
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
            UCI_Engine.StartInfo.CreateNoWindow = false;
            UCI_Engine.StartInfo.RedirectStandardInput = true;
            UCI_Engine.StartInfo.RedirectStandardOutput = true;
            UCI_Engine.Start();
            UCI_Engine.OutputDataReceived += OutputDataReceivedProc;
            UCI_Engine.BeginOutputReadLine();

            // start new game
            EngineCommand(kSetUCIMode);
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

        //public bool CalculateBestMove( /*out Int32 From, out Int32 To*/ )
        //{
        //    if (UCI_Engine != null)
        //    {
        //        // setup engine board string
        //        String searchString = kStartMovesFromStartPos + ConstructMoveString();

        //        // stop thinking
        //        EngineCommand(kStopEngine);

        //        // setup engine board
        //        EngineCommand(searchString);

        //        // think!
        //        EngineCommand("go depth 12");
        //    }

        //    return true;
        //}

        #endregion

        //////////////////////////////////////////////////////////////////////////
        // Private methods
        //////////////////////////////////////////////////////////////////////////
        #region Private methods

        private static void OutputDataReceivedProc(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine.Data == null)
                return;

            String t = outLine.Data;
            if (t.Contains("bestmove"))
            {
                String bestmove = t.Substring(9, 4);
                Console.WriteLine("Best move: " + bestmove);
            }
            else if (t.Contains(" pv "))
            {
                String considerering = t;
                Int32 length = considerering.Length;
                Int32 idxofline = considerering.IndexOf(" pv ") + 4;
                considerering = considerering.Substring(idxofline, length - idxofline);
                Console.WriteLine(" Considering line: " + considerering);
            }
        }

        //private String ConstructMoveString()
        //{
        //    String result = "";

        //    for (Int32 idx = 0; idx < Globals.GameData.g_MoveHistory.Count; ++idx)
        //    {
        //        Int32 From = Globals.GameData.g_MoveHistory[idx].FromSquare;
        //        Int32 To = Globals.GameData.g_MoveHistory[idx].ToSquare;

        //        Int32 RowFrom = 0;
        //        Int32 ColFrom = 0;
        //        Globals.Etc.GetRowColFromSquare(From, out RowFrom, out ColFrom);

        //        Int32 RowTo = 0;
        //        Int32 ColTo = 0;
        //        Globals.Etc.GetRowColFromSquare(To, out RowTo, out ColTo);

        //        String FromString = "";
        //        switch (ColFrom)
        //        {
        //            case 0:
        //                FromString += "a";
        //                break;
        //            case 1:
        //                FromString += "b";
        //                break;
        //            case 2:
        //                FromString += "c";
        //                break;
        //            case 3:
        //                FromString += "d";
        //                break;
        //            case 4:
        //                FromString += "e";
        //                break;
        //            case 5:
        //                FromString += "f";
        //                break;
        //            case 6:
        //                FromString += "g";
        //                break;
        //            case 7:
        //                FromString += "h";
        //                break;
        //        }

        //        FromString += (RowFrom + 1).ToString();

        //        String ToString = "";
        //        switch (ColTo)
        //        {
        //            case 0:
        //                FromString += "a";
        //                break;
        //            case 1:
        //                FromString += "b";
        //                break;
        //            case 2:
        //                FromString += "c";
        //                break;
        //            case 3:
        //                FromString += "d";
        //                break;
        //            case 4:
        //                FromString += "e";
        //                break;
        //            case 5:
        //                FromString += "f";
        //                break;
        //            case 6:
        //                FromString += "g";
        //                break;
        //            case 7:
        //                FromString += "h";
        //                break;
        //        }

        //        FromString += (RowTo + 1).ToString();

        //        result += FromString + ToString + " ";
        //    }

        //    return result;
        //}

        public void EngineCommand(String cmd)
        {
            if (UCI_Engine != null)
                UCI_Engine.StandardInput.WriteLine(cmd);
        }

        public string GetEngineOutput()
        {
            if (UCI_Engine != null)
                 return UCI_Engine.StandardOutput.ReadToEnd();


            return null;
        }

        #endregion
    }
}
