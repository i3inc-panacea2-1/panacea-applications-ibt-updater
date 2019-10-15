using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using IBT.Updater.Interfaces;
using IBT.Updater.Models;
using Ionic.Zip;
using Microsoft.Win32;
using PanaceaLib;
using ServiceStack.Text;
using IBT.Updater.Helpers;
using Module = IBT.Updater.Interfaces.Module;
using System.Management.Automation;
using Windows.Writefilters;

namespace IBT.Updater
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool Continue = false;
        private static Dictionary<string, string> _keys;

        public MainWindow()
        {
            InitializeComponent();
            Version.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
#if DEBUG
            WindowState = WindowState.Normal;
            Width = 1366;
            Height = 768;
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
#endif
        }

        private void OnProgressTotal(string message)
        {
            TotalProgressLabel.Content = message;
        }

        private void OnTextProgressFiles(string message)
        {
            FilesProgressLabel.Content = message;
        }

        private void OnProgressFiles(int val)
        {
            FilesProgressBar.Value = val;
        }

        private void BuildArgumentsDictionary()
        {
            _keys = new Dictionary<string, string>();
            foreach (string s in ConfigurationManager.AppSettings.Keys)
            {
                _keys[s] = ConfigurationManager.AppSettings[s];
            }
            foreach (var split in Environment.GetCommandLineArgs().Select(s => s.Split('=')))
            {
                if (split.Length > 2)
                {
                    for (var i = 2; i < split.Length; i++) split[1] += "=" + split[i];
                }
                if (split.Length >= 2)
                {
                    _keys[split[0]] = split[1];
                }
                else
                {
                    _keys[split[0]] = null;
                }
            }
        }

        
        List<Module> _modules;
        void PrepareModules()
        {

            _modules = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && !t.IsNested && t.FullName.StartsWith("IBT.Updater.Modules."))
                .Select(type => (Module)Activator.CreateInstance(type))
                .ToList();
            

            foreach (var module in _modules)
            {
                module.ProgressReport += (oo, ee) =>
                {
                    OnTextProgressFiles(ee);
                };

                module.FunctionProgressReport += (oo, ee) =>
                {
                    OnProgressFiles(ee);
                };

                module.FileDownloadProgressReport += (oo, ee) =>
                {
                    DownloadProgressBar.Value = ee;
                };

                module.FileDownloadBegin += (oo, ee) =>
                {
                    DownloadProgressBar.Visibility = Visibility.Visible;
                };

                module.FileDownloadEnd += (oo, ee) =>
                {
                    DownloadProgressBar.Visibility = Visibility.Hidden;
                };
            }
        }

        private async Task Load()
        {
            OnProgressTotal("Initializing...");
            var info = await TerminalIdentification.TerminalIdentificationManager.GetIdentificationInfoAsync();
            if(info?.Putik != null)
            {
                Putik.Text = info.Putik;
            }
            await Task.Run(() =>
            {
                BuildArgumentsDictionary();
            });
            OnProgressTotal("Waiting for network...");
            await Common.WaitForNetwork();

            var dict = new SafeDictionary<string, object>();
            foreach (var k in _keys.Keys)
            {
                dict.Add(k, _keys[k]);
            }
            PrepareModules();
            var totalSteps =
                _modules.Count(module => module.GetType().GetMethod("OnPreUpdate", BindingFlags.NonPublic | BindingFlags.Instance).DeclaringType == module.GetType()) +
                _modules.Count(module => module.GetType().GetMethod("OnUpdate", BindingFlags.NonPublic | BindingFlags.Instance).DeclaringType == module.GetType()) +
                _modules.Count(module => module.GetType().GetMethod("OnAfterUpdate", BindingFlags.NonPublic | BindingFlags.Instance).DeclaringType == module.GetType());
            TotalProgressBar.Maximum = totalSteps;

            try
            {
                if (!await ExecModuleFunc("OnPreUpdate", dict)
                    || !await ExecModuleFunc("OnUpdate", dict)
                    || !await ExecModuleFunc("OnAfterUpdate", dict))
                {
                    
                    return;

                }
            }
            catch (Exception ex)
            {
                OnProgressTotal($"A module reported failure ({moduleName}): {ex.Message} {ex.InnerException?.Message}");
                await Task.Delay(10000);
                App.ShutdownSafe(false);
                Process.Start(Common.Path("Updater.Replacer.exe"));
                return;
            }

            if (_modules.Any(m => m.RequiresUpdaterRestart))
            {
                App.ShutdownSafe(false);
                Process.Start(Common.Path( "Updater.Replacer.exe"));
                return;
            }
            if (await LockdownManager.IsFrozen() == false && FreezeHelper.IsFreezeEnabled())
            {
                OnProgressTotal("Rebooting to freeze");
                await Task.Delay(3500);
                LockdownManager.Freeze();
                App.ShutdownSafe(false);
                return;
            }

            OnProgressTotal("Update completed");
            StartPanacea();
        }

        string moduleName;

        async Task<bool> ExecModuleFunc(string func, SafeDictionary<string, object> dict)
        {
            foreach (var module in _modules.Where(m => m.GetType().GetMethod(func, BindingFlags.NonPublic | BindingFlags.Instance).DeclaringType == m.GetType())
                .OrderByDescending(m =>
                {
                    var attr = m.GetType().GetMethod(func, BindingFlags.NonPublic | BindingFlags.Instance).GetCustomAttribute<ExecutionPriorityAttribute>();
                    return attr?.Priority ?? 0;
                }))
            {
                var attr = module.GetType().GetCustomAttribute<ActionAttribute>();
                if (attr != null) OnProgressTotal(attr.Text);
                moduleName = module.GetType().Name;
                if (!await (Task<bool>)module.GetType().GetMethod(func, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(module, new object[] { dict })) return false;

                TotalProgressBar.Value++;

                OnProgressTotal(String.Empty);
                OnTextProgressFiles(String.Empty);
            }
            return true;

        }

        internal async Task Restart(string msg)
        {
            Closed -= MainWindow_OnClosed;
            OnProgressTotal("An error occured: " + msg);
            await Task.Delay(10000);

            using (var key = Registry.LocalMachine.OpenSubKey("Software", true))
            using (var panacea = key.CreateSubKey("Panacea"))
            using (var updater = panacea.CreateSubKey("Updater"))
            {
                var val = Int32.Parse(updater.GetValue("ContinuousFailures", "0").ToString());
                if (val < 4)
                {
                    updater.SetValue("ContinuousFailures", ++val);
                    App.Restart();
                }
                else
                {
                    updater.SetValue("ContinuousFailures", 0);
                    App.ShutdownSafe();
                    Process.Start(Common.Path("..","Applications","SystemSetup","SystemSetup.exe"), "/automatic");
                }
            }
        }

        private void StartPanacea()
        {
            App.ShutdownSafe();

            var reg = Common.Path("..", "Applications", "ServerCommunicator", "ServerCommunicator.exe");
            if (File.Exists(reg))
                Process.Start(reg);

            var panacea = Common.Path("..", "Panacea.exe");
            if (File.Exists(panacea))
            {
                Process.Start(panacea, string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Concat(new string[] { "noupdate=1" }).ToArray() ));
            }

          
        }

        #region event handlers
        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            App.ShutdownSafe();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Environment.MachineName == "IMAGE")
            {
                App.ShutdownSafe();
                Process.Start(Common.Path("..","Applications","SystemSetup","SystemSetup.exe"), "/automatic");
                return;
            }
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                Activate();
                Focus();
            }
            await Load();
        }
        #endregion
    }
}