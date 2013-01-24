using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Threading;
using System.Diagnostics;
using System.ComponentModel;

namespace Starcounter.InstallerWPF.DemoSequence
{

    [TemplatePart(Name = "PART_Content", Type = typeof(ContentPresenter))]
    public class BenchmarkDemoControl : BaseDemoControl
    {
        #region Properties
        public override string Image
        {
            get
            {
                return "images/chartdocument_128x128.png";
            }
        }
        #endregion

        #region Commands

        #region Next
        public static RoutedCommand NextRoutedCommand = new RoutedCommand();

        private void CanExecute_Next_Command(object sender, CanExecuteRoutedEventArgs e)
        {
            BenchmarkDemoControl benchmarkDemoControl = sender as BenchmarkDemoControl;
            if (benchmarkDemoControl == null)
            {
                return;
            }

            e.CanExecute = benchmarkDemoControl.Executable_ProcessStarter != null || benchmarkDemoControl.VisualStudio_ProcessStarter != null || benchmarkDemoControl.VisualStudio_ProjectListener != null;
            e.Handled = true;
        }

        private void Executed_Next_Command(object sender, ExecutedRoutedEventArgs e)
        {

            BenchmarkDemoControl benchmarkDemoControl = sender as BenchmarkDemoControl;
            if (benchmarkDemoControl == null)
            {
                return;
            }

            // Kill running process
            if (e.Parameter != null && e.Parameter is string)
            {
                if (((string)e.Parameter).ToLower().Equals("kill"))
                {
                    try
                    {
                        if (benchmarkDemoControl.ProjectProcess != null && benchmarkDemoControl.ProjectProcess.HasExited == false)
                        {
                            benchmarkDemoControl.ProjectProcess.Kill();
                        }
                    }
                    catch (Win32Exception ex)
                    {
                        if (ex.ErrorCode == -2147467259)  // Access denied
                        {
                        }
                    }
                }
            }

            // Precompiled
            if (benchmarkDemoControl.Executable_ProcessStarter != null)
            {
                benchmarkDemoControl.Executable_ProcessStarter.Abort();
            }

            // Sourcecode
            if (benchmarkDemoControl.VisualStudio_ProcessStarter != null)
            {
                benchmarkDemoControl.VisualStudio_ProcessStarter.Abort();
            }

            if (benchmarkDemoControl.VisualStudio_ProjectListener != null)
            {
                benchmarkDemoControl.VisualStudio_ProjectListener.Abort();
            }

            benchmarkDemoControl.OnComplete(new CompletedEventArgs());

            e.Handled = true;

        }


        #endregion

        #endregion

        static BenchmarkDemoControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BenchmarkDemoControl), new FrameworkPropertyMetadata(typeof(BenchmarkDemoControl)));
        }


        public BenchmarkDemoControl()
        {
            this.DataContext = this;
            // register abort command
            CommandManager.RegisterClassCommandBinding(typeof(BenchmarkDemoControl), new CommandBinding(BenchmarkDemoControl.NextRoutedCommand, this.Executed_Next_Command, this.CanExecute_Next_Command));

        }

        public void Start(Settings settings)
        {
            try
            {
                if (settings.PostDemoType == PostDemoTypeEnum.PREBUILT)
                {
                    string executable = System.IO.Path.Combine(settings.InstallationPath, @"SamplesAndDemos/SimpleBenchmark/PreBuilt/SimpleBenchmark.exe");
                    this.Start_PreCompiled(executable);
                }
                else if (settings.PostDemoType == PostDemoTypeEnum.VS2010)
                {
                    string projectfile = System.IO.Path.Combine(settings.InstallationPath, @"SamplesAndDemos/SimpleBenchmark/Vs2010/SimpleBenchmark.sln");
                    this.Start_SourceProject(projectfile, VisualStudioVersion.VS2010);
                }
                else if (settings.PostDemoType == PostDemoTypeEnum.VS2012)
                {
                    string projectfile = System.IO.Path.Combine(settings.InstallationPath, @"SamplesAndDemos/SimpleBenchmark/Vs2010/SimpleBenchmark.sln");
                    this.Start_SourceProject(projectfile, VisualStudioVersion.VS2012);
                }
            }
            catch (Exception e)
            {
                this.OnComplete(new CompletedEventArgs(e));
            }
        }


        #region Precompiled

        ProcessStarter Executable_ProcessStarter;

        private void Start_PreCompiled(string executable)
        {
            if (executable == null) throw new ArgumentNullException("executable");
            if (string.IsNullOrEmpty(executable)) throw new ArgumentException("executable");

            if (this.Executable_ProcessStarter != null) throw new InvalidOperationException("Project already running");

            this.Title = "Benchmark";
            this.Description = "This demo will benchmark Starcounter against SQL Server";

            if (string.IsNullOrEmpty(executable))
            {
                throw new ArgumentNullException("executable");
            }

            if (!File.Exists(executable))
            {
                throw new FileNotFoundException(executable);
            }

            //this.TemplateName = Status.Starting.ToString();

            this.Executable_ProcessStarter = new ProcessStarter();
            ProcessStartInfo startInfo = new ProcessStartInfo(executable);

            startInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(executable);

            this.Executable_ProcessStarter.Progress += new ProcessStarter.ProgressEventHandler(PreCompiled_Progress);
            this.Executable_ProcessStarter.Complete += new ProcessStarter.CompleteEventHandler(PreCompiled_Complete);
            this.Executable_ProcessStarter.Execute(startInfo);

        }

        void PreCompiled_Progress(object sender, ProgressEventArgs e)
        {

            if (e.Process != null)
            {
                this.ProjectProcess = e.Process;    // So we can kill it..
            }

            switch (e.Status)
            {
                case Status.Unknown:
                    break;
                case Status.Starting:
                    this.TemplateName = Status.Starting.ToString();
                    break;
                case Status.Started:
                    this.TemplateName = "Running";
                    //this.StartBenchmarkTimer();
                    break;
                case Status.Exited:
                    this.OnComplete(new CompletedEventArgs());
                    break;
            }

            CommandManager.InvalidateRequerySuggested();

        }

        void PreCompiled_Complete(object sender, CompletedEventArgs e)
        {
            this.Executable_ProcessStarter = null;

            if (e.HasError)
            {
                this.OnComplete(new CompletedEventArgs(e.Error));
            }
            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

        #region SourceProject

        VisualStudioProjectListener VisualStudio_ProjectListener;
        ProcessStarter VisualStudio_ProcessStarter;

        private void Start_SourceProject(string projectfile, VisualStudioVersion vsVersion)
        {
            if (projectfile == null) throw new ArgumentNullException("projectfile");

            if (this.VisualStudio_ProjectListener != null) throw new InvalidOperationException("Project already running");
            if (this.VisualStudio_ProcessStarter != null) throw new InvalidOperationException("Project already running");

            if (string.IsNullOrEmpty(projectfile))
            {
                throw new ArgumentNullException("projectfile");
            }

            if (!File.Exists(projectfile))
            {
                throw new FileNotFoundException(projectfile);
            }

            string openfile = "Main.cs"; // NOTE: No support for filename with spaces

            string assemblyName = "SimpleBenchmark";    // TODO: try to get the assembly namn 

            string extention = System.IO.Path.GetExtension(projectfile);
            if (!string.IsNullOrEmpty(extention))
            {
                if (".csproj".Equals(projectfile.ToLower()))
                {
                    assemblyName = Utils.GetVisualStudioProjectAssemblyName(projectfile);
                }
            }


            if (string.IsNullOrEmpty(assemblyName))
            {
                // TODO: ShowError.  skipp to next step (WebApp)
                string message = string.Format("Missing Assemblyname in project file {0}", projectfile);
                throw new InvalidProgramException(message);
            }

            string processName = assemblyName;
            //            string processName = string.Format("{0}.vshost", assemblyName);

            string visualStudioExecutablePath = Utils.GetVisualStudioExePath(vsVersion);


            if (string.IsNullOrEmpty(visualStudioExecutablePath))
            {
                string message = string.Format("Can not find VisualStudio installation");
                throw new InvalidProgramException(message);
            }

            if (!File.Exists(visualStudioExecutablePath))
            {
                string message = string.Format("Can not find VisualStudio installation, {0}", visualStudioExecutablePath);
                throw new InvalidProgramException(message);
            }

            this.Title = "Benchmark";
            this.Description = "This demo will benchmark Starcounter against SQL Server. The code is loaded into Visual Studio";


            // 1. Start listening for process
            // Important to start this before so we can get the correct blacklist
            this.VisualStudio_ProjectListener = new VisualStudioProjectListener();
            this.VisualStudio_ProjectListener.Progress += new VisualStudioProjectListener.ProgressEventHandler(VisualStudio_ProjectListener_Progress);
            this.VisualStudio_ProjectListener.Complete += new VisualStudioProjectListener.CompleteEventHandler(VisualStudio_ProjectListener_Complete);
            this.VisualStudio_ProjectListener.Execute(ProjectType.VisualStudioEXEApplication, processName);

            // 2. Start Visual studio with Benchmark project
            this.VisualStudio_ProcessStarter = new ProcessStarter();
            ProcessStartInfo startInfo = new ProcessStartInfo(visualStudioExecutablePath);
            startInfo.Arguments = projectfile;

            // Wrap path with " char
            if (startInfo.Arguments.Contains(' ') && startInfo.Arguments[0] != '"' && startInfo.Arguments[startInfo.Arguments.Length - 1] != '"')
            {
                startInfo.Arguments = string.Format("\"{0}\"", startInfo.Arguments);
            }

            if (!string.IsNullOrEmpty(openfile))
            {
                startInfo.Arguments += string.Format(" /command \"File.OpenFile {0}\"", openfile);
            }


            this.VisualStudio_ProcessStarter.Progress += new ProcessStarter.ProgressEventHandler(VisualStudio_ProcessStarter_Progress);
            this.VisualStudio_ProcessStarter.Complete += new ProcessStarter.CompleteEventHandler(VisualStudio_ProcessStarter_Complete);
            this.VisualStudio_ProcessStarter.Execute(startInfo);

        }

        void VisualStudio_ProcessStarter_Progress(object sender, ProgressEventArgs e)
        {

            if (e.Process != null)
            {
                this.VisualStudioProcess = e.Process;
            }


            switch (e.Status)
            {
                case Status.Unknown:
                    break;
                case Status.Starting:
                    this.TemplateName = e.Status.ToString();

                    break;
                case Status.Started:
                    this.TemplateName = e.Status.ToString();

                    break;
                case Status.Exited:
                    this.OnComplete(new CompletedEventArgs());

                    break;
            }

            CommandManager.InvalidateRequerySuggested();
        }

        void VisualStudio_ProcessStarter_Complete(object sender, CompletedEventArgs e)
        {
            this.VisualStudio_ProcessStarter = null;

            if (e.HasError)
            {
                if (this.VisualStudio_ProjectListener != null)
                {
                    this.VisualStudio_ProjectListener.Abort();
                }

                this.OnComplete(new CompletedEventArgs(e.Error));
            }

            CommandManager.InvalidateRequerySuggested();

        }

        void VisualStudio_ProjectListener_Progress(object sender, ProgressEventArgs e)
        {

            // End "VisualStudio" ProcessStarter, No need to listen for more events on VisualStudio
            if (this.VisualStudio_ProcessStarter != null)
            {
                this.VisualStudio_ProcessStarter.Abort();
            }

            if (e.Process != null)
            {
                this.ProjectProcess = e.Process;
            }
            switch (e.Status)
            {
                case Status.Unknown:
                    break;
                case Status.Starting:
                    break;
                case Status.Started:
                    this.TemplateName = "Running";
                    //this.StartBenchmarkTimer();
                    break;
                case Status.Exited:

                    this.OnComplete(new CompletedEventArgs());
                    break;
            }

            CommandManager.InvalidateRequerySuggested();
        }

        void VisualStudio_ProjectListener_Complete(object sender, CompletedEventArgs e)
        {
            this.VisualStudio_ProjectListener = null;

            // No need to listen for more events on VisualStudio
            if (this.VisualStudio_ProcessStarter != null)
            {
                this.VisualStudio_ProcessStarter.Abort();
            }

            if (e.HasError)
            {
                this.OnComplete(new CompletedEventArgs(e.Error));
            }

            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

        System.Timers.Timer BenchmarkRunningTimer;

        #region Timer

        private void StartBenchmarkTimer()
        {
            if (this.BenchmarkRunningTimer == null)
            {
                this.BenchmarkRunningTimer = new System.Timers.Timer();
                this.BenchmarkRunningTimer.Elapsed += new System.Timers.ElapsedEventHandler(BenchmarkRunningTimer_Elapsed);
                // Set the Interval to 5 seconds.
                this.BenchmarkRunningTimer.Interval = 5000;
            }
            this.BenchmarkRunningTimer.Enabled = true;
        }

        void BenchmarkRunningTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.BenchmarkRunningTimer.Enabled = false;

            this.TemplateName = "Next";

        }

        #endregion

    }
}
