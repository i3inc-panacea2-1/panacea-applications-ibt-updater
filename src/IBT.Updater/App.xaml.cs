using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using PanaceaLib;

namespace IBT.Updater
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App :  SingleInstanceApp
    {
	    public App() : base("IBT.Updater")
	    {
			InitializeComponent();
            ServicePointManager.MaxServicePointIdleTime = 10000;
            ServicePointManager.Expect100Continue = false;
        }

	    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                var sw = new StreamWriter("log.txt");
                sw.WriteLine(e.Exception.Message);
                sw.WriteLine(e.Exception.TargetSite);
                sw.Close();
            }
            catch
            {
            }
        }

        public static void Restart()
        {
            SingleInstance<App>.Cleanup();
            Current.Shutdown();
            Process.Start(Common.Path() + "IBT.Updater.exe");
        }

        private static bool _shuttingDown;
        public static void ShutdownSafe(bool runLauncher = true)
        {
            if (_shuttingDown) return;
            _shuttingDown = true;
            try
            {
                var process = Process.GetProcessesByName("PanaceaLauncher").FirstOrDefault();
                if (process == null && runLauncher)
                {
                    Process.Start(Common.Path() + "..\\Applications\\Launcher\\PanaceaLauncher.exe");
                }
            }
            catch
            {
            }
            SingleInstance<App>.Cleanup();
            Current.Shutdown();
        }
        public override bool SignalExternalCommandLineArgs(IList<string> args)
        {
            return true;
        }
        static string[] Args;
        private async void App_OnStartup(object sender, StartupEventArgs e)
        {
            Args = e.Args;
            string runtimePathFile = (await PanaceaRegistry.GetServerInformation(false)).RuntimePath;

            if (!string.IsNullOrEmpty(runtimePathFile))
            {
                try
                {
                    var runtimePath = runtimePathFile;
                    if(runtimePathFile.EndsWith("\\"))
                        runtimePath = Path.GetDirectoryName(runtimePathFile);

                    if (runtimePath + "\\Updater" != Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                    {

                        Directory.CreateDirectory(runtimePath + "\\Updater");
                        await
                            Common.MoveDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                                runtimePath + "\\Updater", false, false);
                        ShutdownSafe();
                        Process.Start(runtimePath + "\\Applications\\SystemSetup\\SystemSetup.exe", "/automatic");

                    }
                    else
                    {
                        ShowWindow();
                    }
                }
                catch
                {
                }
            }
            else
            {
                ShowWindow();
            }
            
        }

        void ShowWindow()
        {
            if (!Args.Contains("noupdate=1")){
                try
                {
                    KillProcesses("PanaceaLauncher", "Panacea", "SystemSetup", "PanaceaRegistrator", "ServerCommunicator",
                        "WebBrowser.SubProcess", "Skype", "VlcMediaPlayer", "WebBrowserGeckoEngineSubProcess", "CefSharp.BrowserSubproces");

                }
                catch (Exception)
                {
                    //ignore
                }
            }
            MainWindow w=new MainWindow();
            w.Show();
            w.Activate();
        }

        public static void KillProcesses(params string[] names)
        {
            if (Debugger.IsAttached) return;
            foreach (var process in names.SelectMany(name => Process.GetProcessesByName(name)))
            {
                process?.Kill();
            }
        }
    }
}