using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.ComponentModel;

namespace Starcounter.InstallerWPF.DemoSequence
{

    public class ProcessStarter
    {

        #region Properties

        private Dispatcher _dispatcher;
        private bool bAbort = false;

        #endregion

        #region Events

        #region Complete Event

        public delegate void CompleteEventHandler(object sender, CompletedEventArgs e);

        public event CompleteEventHandler Complete;
        protected virtual void OnComplete(CompletedEventArgs e)
        {
            if (Complete != null)
            {
                Complete(this, e);
            }

            if (this.CompleteCallback != null)
            {
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate
                {
                    ((EventHandler<CompletedEventArgs>)this.CompleteCallback)(this, e);
                }));
            }
        }

        #endregion

        #region Progress Event

        public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);
        public event ProgressEventHandler Progress;
        protected virtual void OnProgress(ProgressEventArgs e)
        {
            if (Progress != null)
            {
                Progress(this, e);
            }

            if (this.ProgressCallback != null)
            {
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate
                {
                    ((EventHandler<ProgressEventArgs>)this.ProgressCallback)(this, e);
                }));
            }

        }
        //        public delegate void ProgressEventHandler(object sender, ProgressEventArgs2 e);

        #endregion

        private EventHandler<CompletedEventArgs> CompleteCallback;
        private EventHandler<ProgressEventArgs> ProgressCallback;

        #endregion

        public ProcessStarter()
        {

            this._dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
        }

        public ProcessStarter(EventHandler<CompletedEventArgs> completeCallback)
        {
            this._dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            this.CompleteCallback = completeCallback;
        }

        public ProcessStarter(EventHandler<CompletedEventArgs> completeCallback, EventHandler<ProgressEventArgs> progressCallback)
        {
            this._dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            this.CompleteCallback = completeCallback;
            this.ProgressCallback = progressCallback;
        }

        public void Execute(ProcessStartInfo startInfo)
        {
            if (startInfo == null) throw new NullReferenceException("startInfo");

            this.bAbort = false;

            ThreadPool.QueueUserWorkItem(this.ProcessHandlingThread, startInfo);
        }


        private void ProcessHandlingThread(object state)
        {
            ProcessStartInfo startInfo = state as ProcessStartInfo;
            if (startInfo == null) throw new InvalidCastException("ProcessStartInfo");

            try
            {

                Status currentStatus = Status.Unknown;

                // Start Visual Studio
                Process process = new Process();
                process.StartInfo = startInfo;

                process.Start();

                if (currentStatus != Status.Starting)
                {
                    currentStatus = Status.Starting;
                    this.OnProcessStatusChanged(currentStatus, process);
                }


                while (true)
                {
                    if (this.bAbort)
                    {
                        break;
                    }

                    if (process.HasExited)
                    {


                        // Exited
                        if (currentStatus != Status.Exited)
                        {
                            currentStatus = Status.Exited;
                            this.OnProcessStatusChanged(currentStatus);
                        }
                        break;
                    }
                    else
                    {
                        process.Refresh();

                        try
                        {
                            if (process.MainWindowHandle != IntPtr.Zero)
                            {
                                // Started
                                if (currentStatus != Status.Started)
                                {
                                    currentStatus = Status.Started;
                                    this.OnProcessStatusChanged(currentStatus, process);
                                }

                            }

                        }
                        catch (InvalidOperationException)
                        {
                            // Ignore
                        }
                    }

                    Thread.Sleep(200);
                }



                // Done
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate
                {
                    this.OnComplete(new CompletedEventArgs());
                }));
            }
            catch (Win32Exception e)
            {
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate
                {
                    this.OnComplete(new CompletedEventArgs(e));
                }));
            }
            catch (Exception e)
            {
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate
                {
                    this.OnComplete(new CompletedEventArgs(e));
                }));

            }

        }

        private void OnProcessStatusChanged(Status status)
        {
            //Console.WriteLine("OnProcessStatusChanged:{0}", status.ToString());

            this._dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate
            {
                this.OnProgress(new ProgressEventArgs(status));
            }));
        }

        private void OnProcessStatusChanged(Status status, Process process)
        {
            //if (process == null || process.HasExited)
            //{
            //    Console.WriteLine("OnProcessStatusChanged:{0}", status.ToString());
            //}
            //else
            //{
            //    Console.WriteLine("OnProcessStatusChanged:{0} - {1}", process.Id, status.ToString());
            //}

            this._dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate
            {
                this.OnProgress(new ProgressEventArgs(status, process));
            }));

        }


        public void Abort()
        {
            this.bAbort = true;
        }


    }


    /// <summary>
    /// 
    /// </summary>
    public class CompletedEventArgs : EventArgs
    {
        public bool HasData
        {
            get
            {
                return this.Data != null;
            }
        }

        public object Data { get; protected set; }

        public bool HasError
        {
            get
            {
                return this.Error != null;
            }
        }

        public Exception Error { get; protected set; }


        public CompletedEventArgs()
        {
        }

        public CompletedEventArgs(object data)
        {
            this.Data = data;
        }

        public CompletedEventArgs(Exception e)
        {
            this.Error = e;
        }

        public CompletedEventArgs(Exception e, object data)
        {
            this.Error = e;
            this.Data = data;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        public Process Process { get; protected set; }
        public Status Status { get; protected set; }

        public ProgressEventArgs()
        {
        }

        public ProgressEventArgs(Status status, Process process)
        {
            this.Process = process;
            this.Status = status;
        }

        public ProgressEventArgs(Status status)
        {
            this.Status = status;
        }
    }

    public enum Status
    {
        Unknown,
        Starting,
        Started,
        Exited
    }

}
