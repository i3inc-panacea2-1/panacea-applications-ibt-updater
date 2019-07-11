using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace IBT.Updater.Interfaces
{
    internal abstract class Module
    {
        #region events
        internal event EventHandler<string> ProgressReport;
        internal event EventHandler<int> FunctionProgressReport;
        internal event EventHandler<int> FileDownloadProgressReport;
        internal event EventHandler FileDownloadBegin;
        internal event EventHandler FileDownloadEnd;
        #endregion

        #region event methods
        /// <summary>
        /// Max 100. Report function progress from 0 to 100
        /// </summary>
        /// <param name="val"></param>
        internal void ReportFunctionProgress(int val)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                FunctionProgressReport?.Invoke(this, val);
            });
        }

        internal void ReportProgress(string msg)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ProgressReport?.Invoke(this, msg);
            });
        }

        internal void ReportFileDownloadProgress(int val)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                FileDownloadProgressReport?.Invoke(this, val);
            });
        }

        internal void BeginFileDownload()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                FileDownloadBegin?.Invoke(this, null);
            });
        }

        internal void EndFileDownload()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                FileDownloadEnd?.Invoke(this, null);
            });
        }
        #endregion

        #region properties
        public bool RequiresUpdaterRestart { get; set; }
        #endregion

        internal virtual Task<bool> OnPreUpdate(SafeDictionary<string, object> keys)
        {
            return Task.FromResult(true);
        }

        internal virtual Task<bool> OnUpdate(SafeDictionary<string, object> keys)
        {
            return Task.FromResult(true);
        }

        internal virtual Task<bool> OnAfterUpdate(SafeDictionary<string, object> keys)
        {
            return Task.FromResult(true);
        }

    }
}
