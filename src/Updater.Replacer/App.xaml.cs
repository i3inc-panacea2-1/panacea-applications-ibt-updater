using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows;
using PanaceaLib;

namespace Init
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private async void App_OnStartup(object sender, StartupEventArgs e)
        {
            try
            {
                await Task.Delay(1000);
                while (Process.GetProcessesByName("IBT.Updater").FirstOrDefault() != default(Process))
                    await Task.Delay(1000);
                await Common.MoveDirectory(Common.Path() + "tmp", Common.Path());
                //MessageBox.Show("from " + Common.Path() + "inuse" + " to " + new DirectoryInfo(Common.Path()).Parent.FullName);
                await Common.MoveDirectory(Common.Path() + "inuse", new DirectoryInfo(Common.Path()).Parent.FullName);

                Process.Start(Common.Path() +"IBT.Updater.exe");
            }
            catch (Exception)
            {
                //MessageBox.Show(ex.Message);
                //if (ex.InnerException != null) MessageBox.Show(ex.InnerException.Message);
            }
            Current.Shutdown();
        }
    }
}
