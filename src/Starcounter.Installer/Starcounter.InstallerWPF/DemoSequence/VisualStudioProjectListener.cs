using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;

namespace Starcounter.InstallerWPF.DemoSequence
{


    public class VisualStudioProjectListener
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

        public VisualStudioProjectListener()
        {

            this._dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
        }

        public VisualStudioProjectListener(EventHandler<CompletedEventArgs> completeCallback)
        {
            this._dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            this.CompleteCallback = completeCallback;
        }

        public VisualStudioProjectListener(EventHandler<CompletedEventArgs> completeCallback, EventHandler<ProgressEventArgs> progressCallback)
        {
            this._dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            this.CompleteCallback = completeCallback;
            this.ProgressCallback = progressCallback;
        }

        public void Execute(ProjectType type, string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName)) throw new ArgumentNullException("processName");

            this.bAbort = false;

            ThreadPool.QueueUserWorkItem(this.ProcessHandlingThread, new object[] { type, assemblyName });
        }


        private void ProcessHandlingThread(object state)
        {
            ProjectType type = (ProjectType)((object[])state)[0];
            string processName = (string)((object[])state)[1];

            string processName_vshost = string.Format("{0}.vshost", processName);

            Status currentStatus = Status.Unknown;


            List<Process> blackList = new List<Process>();
            if (!string.IsNullOrEmpty(processName))
            {
                Process[] process = Process.GetProcessesByName(processName);
                blackList = new List<Process>(process);

                process = Process.GetProcessesByName(processName_vshost);
                foreach (Process prosess_vshost in process)
                {
                    blackList.Add(prosess_vshost);
                }
            }

            Process currentProcess = null;

            try
            {

                while (true)
                {
                    if (this.bAbort)
                    {
                        break;
                    }

                    //try
                    //{


                    // Retrive process
                    if (currentProcess == null)
                    {
                        Process[] processes = Process.GetProcessesByName(processName);
                        Process[] processes_vshost = Process.GetProcessesByName(processName_vshost);


                        Process[] processes_combined = new Process[processes.Length + processes_vshost.Length];
                        Array.Copy(processes, processes_combined, processes.Length);
                        Array.Copy(processes_vshost, 0, processes_combined, processes.Length, processes_vshost.Length);


                        foreach (Process process in processes_combined)
                        {

                            try
                            {
                                if (process.HasExited) continue;
                            }
                            catch (Win32Exception e)
                            {
                                if (e.ErrorCode == -2147467259)  // Access denied
                                {
                                    //continue;
                                    // Ignore
                                    // UAC-User denied access
                                }
                                else
                                {
                                    throw e;
                                }
                            }
                            // Check against Blacklist
                            bool bFoundInBlackList = false;
                            foreach (Process blacklisted in blackList)
                            {

                                try
                                {
                                    if (blacklisted.HasExited) continue;
                                }
                                catch (Win32Exception e)
                                {
                                    if (e.ErrorCode == -2147467259)  // Access denied
                                    {
                                        // Ignore
                                    }
                                    else
                                    {
                                        throw e;
                                    }
                                }

                                if (blacklisted.Id == process.Id)
                                {
                                    bFoundInBlackList = true;
                                    break;
                                }
                            }

                            // Not found our process
                            if (bFoundInBlackList == false)
                            {
                                if (process.ProcessName.Equals(processName_vshost) || process.ProcessName.Equals(processName))
                                {
                                    if (process.MainWindowHandle != IntPtr.Zero)
                                    {
                                        currentProcess = process;
                                    }
                                }
                                else
                                {
                                    currentProcess = process;
                                }
                            }
                        }
                    }

                    if (currentProcess != null)
                    {
                        try
                        {
                            if (currentProcess.HasExited)
                            {
                                if (currentStatus != Status.Exited)
                                {
                                    currentStatus = Status.Exited;
                                    this.OnProcessStatusChanged(currentStatus, currentProcess);
                                }
                                break;
                            }
                        }
                        catch (Win32Exception e)
                        {
                            if (e.ErrorCode == -2147467259) // Access denied
                            {
                                if (currentProcess.MainWindowHandle == IntPtr.Zero)
                                {
                                    if (currentStatus != Status.Exited)
                                    {
                                        currentStatus = Status.Exited;
                                        this.OnProcessStatusChanged(currentStatus, currentProcess);
                                    }
                                    break;
                                }

                                // Ignore
                            }
                            else
                            {
                                throw e;
                            }
                        }


                        currentProcess.Refresh();

                        if (type == ProjectType.VisualStudioEXEApplication)
                        {
                            try
                            {
                                if (currentProcess.MainWindowHandle != IntPtr.Zero)
                                {
                                    if (currentStatus != Status.Starting && currentStatus != Status.Started && currentStatus != Status.Exited)
                                    {
                                        currentStatus = Status.Starting;
                                        this.OnProcessStatusChanged(currentStatus, currentProcess);
                                    }

                                    if (currentStatus != Status.Started && currentStatus != Status.Exited)
                                    {
                                        currentStatus = Status.Started;
                                        this.OnProcessStatusChanged(currentStatus, currentProcess);
                                    }
                                }
                                //else
                                //{
                                //    if (currentStatus != Status2.Starting && currentStatus != Status2.Started && currentStatus != Status2.Exited)
                                //    {
                                //        currentStatus = Status2.Starting;
                                //        this.OnProcessStatusChanged(currentStatus, currentProcess);
                                //    }

                                //}
                            }
                            catch (InvalidOperationException)
                            {
                                // Ignore
                            }
                        }
                        else if (type == ProjectType.VisualStudioWebApplication)
                        {
                            if (currentStatus != Status.Starting && currentStatus != Status.Started && currentStatus != Status.Exited)
                            {
                                currentStatus = Status.Starting;
                                this.OnProcessStatusChanged(currentStatus, currentProcess);
                            }

                            if (currentStatus != Status.Started && currentStatus != Status.Exited)
                            {
                                currentStatus = Status.Started;
                                this.OnProcessStatusChanged(currentStatus, currentProcess);
                            }

                        }
                    }

                    Thread.Sleep(200);

                    //}
                    //catch (Win32Exception e)
                    //{
                    //    if (e.ErrorCode == -2147467259)
                    //    {
                    //        // Ignore
                    //        // UAC-User denied access
                    //    }
                    //    else
                    //    {
                    //        throw e;
                    //    }
                    //}


                }



                // Done
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate
                {
                    this.OnComplete(new CompletedEventArgs());
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
            //Console.WriteLine("VisualStudioProjectListener:{0}", status.ToString());

            this._dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate
            {
                this.OnProgress(new ProgressEventArgs(status));
            }));
        }

        private void OnProcessStatusChanged(Status status, Process process)
        {
            //if (process == null || process.HasExited)
            //{
            //    Console.WriteLine("VisualStudioProjectListener:{0}", status.ToString());
            //}
            //else
            //{
            //    Console.WriteLine("VisualStudioProjectListener:{0} - {1}", process.Id, status.ToString());
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

}
