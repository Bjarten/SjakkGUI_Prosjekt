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
        string motorSti = @"C:\Users\Bjarte\Documents\Visual Studio 2012\Projects\Sjakk\Sjakk\Sjakk\stockfish_14053109_32bit.exe";
        Thread threadChessEngine;
        Thread threadRobot;

        static string sysID = string.Empty;
        static Controller dynamicController;
        static NetworkScanner scanner;
        static Controller controller;
        static ABB.Robotics.Controllers.RapidDomain.Task[] tasks;
        static NetworkWatcher networkwatcher;
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


            sysID = controllers[0].SystemId.ToString();
            controller = ControllerFactory.CreateFrom(controllers[0]);
            controller.Logon(UserInfo.DefaultUser);
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
                }));
            }
        }

        private void SendCommandToRobot(object obj)
        {
            while (true)
            {
                // Wait for new koordinates for robot
                ewhSendCommandToRobot.WaitOne();

                //declare a variable of data type RapidDomain.Bool
                ABB.Robotics.Controllers.RapidDomain.Bool rapidBool;

                ABB.Robotics.Controllers.RapidDomain.RapidData rd = controller.Rapid.GetRapidData("T_ROB1", "stableSnus", "flag");
                //test that data type is correct before cast
                if (rd.Value is ABB.Robotics.Controllers.RapidDomain.Bool)
                {
                    rapidBool = (ABB.Robotics.Controllers.RapidDomain.Bool)rd.Value;
                    //assign the value of the RAPID data to a local variable
                    bool boolValue = rapidBool.Value;

                    rapidBool.Value = false;

                    //Request mastership of Rapid before writing to the controller
                    master = Mastership.Request(controller.Rapid);
                    //Change: controller is repaced by aController
                    rd.Value = rapidBool;
                    //Release mastership as soon as possible
                    master.ReleaseOnDispose = true;
                    master.Dispose();

                }



                //if (controller.OperatingMode == ControllerOperatingMode.Auto)
                //{
                //    tasks = controller.Rapid.GetTasks();
                //    using (Mastership m = Mastership.Request(controller.Rapid))
                //    {
                //        //Perform operation
                //        //tasks[0].Start();
                //        tasks[0].Stop();
                //    }
                //}
                //else
                //{
                //  }

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


