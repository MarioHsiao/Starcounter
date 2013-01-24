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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;


// http://www.iconfinder.com/search/?q=iconset%3AHand_Drawn_Web_Icon_Set

namespace Starcounter.InstallerWPF.DemoSequence
{

    [TemplatePart(Name = "PART_Content", Type = typeof(ContentPresenter))]
    public class WebApplicationDemoControl : BaseDemoControl
    {
        #region Properties
        public override string Image
        {
            get
            {
                return "images/internetdocument_128x128.png";
            }
        }
        #endregion


        #region Commands

        #region Next
        public static RoutedCommand NextRoutedCommand = new RoutedCommand();

        private void CanExecute_Next_Command(object sender, CanExecuteRoutedEventArgs e)
        {
            WebApplicationDemoControl webApplicationDemoControl = sender as WebApplicationDemoControl;
            if (webApplicationDemoControl == null)
            {
                return;
            }

            e.CanExecute = webApplicationDemoControl.Executable_ProcessStarter != null || webApplicationDemoControl.VisualStudio_ProcessStarter != null || webApplicationDemoControl.VisualStudio_ProjectListener != null;
            e.Handled = true;
        }

        private void Executed_Next_Command(object sender, ExecutedRoutedEventArgs e)
        {

            WebApplicationDemoControl webApplicationDemoControl = sender as WebApplicationDemoControl;
            if (webApplicationDemoControl == null)
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
                        if (webApplicationDemoControl.ProjectProcess != null && webApplicationDemoControl.ProjectProcess.HasExited == false)
                        {
                            webApplicationDemoControl.ProjectProcess.Kill();
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
            if (webApplicationDemoControl.Executable_ProcessStarter != null)
            {
                webApplicationDemoControl.Executable_ProcessStarter.Abort();
            }

            // Sourcecode
            if (webApplicationDemoControl.VisualStudio_ProcessStarter != null)
            {
                webApplicationDemoControl.VisualStudio_ProcessStarter.Abort();
            }

            if (webApplicationDemoControl.VisualStudio_ProjectListener != null)
            {
                webApplicationDemoControl.VisualStudio_ProjectListener.Abort();
            }

            webApplicationDemoControl.OnComplete(new CompletedEventArgs());

            e.Handled = true;

        }


        #endregion

        #endregion

        static WebApplicationDemoControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WebApplicationDemoControl), new FrameworkPropertyMetadata(typeof(WebApplicationDemoControl)));
        }


        public WebApplicationDemoControl()
        {
            this.DataContext = this;
            // register abort command
            CommandManager.RegisterClassCommandBinding(typeof(WebApplicationDemoControl), new CommandBinding(WebApplicationDemoControl.NextRoutedCommand, Executed_Next_Command, CanExecute_Next_Command));

        }



        public void Start(Settings settings)
        {

            try
            {
                if (settings.PostDemoType == PostDemoTypeEnum.PREBUILT)
                {
                    string executable = System.IO.Path.Combine(settings.InstallationPath, @"SamplesAndDemos/VsSamples/Vs2010/HelloWorld.exe"); // TODO: This dosent exist 
                    this.Start_PreCompiled(executable);
                }
                else if (settings.PostDemoType == PostDemoTypeEnum.VS2010)
                {
                    string projectfile = System.IO.Path.Combine(settings.InstallationPath, @"SamplesAndDemos/VsSamples/Vs2010/HelloWorld.csproj");
                    this.Start_SourceProject(projectfile, VisualStudioVersion.VS2010);
                }
                else if (settings.PostDemoType == PostDemoTypeEnum.VS2012)
                {
                    string projectfile = System.IO.Path.Combine(settings.InstallationPath, @"SamplesAndDemos/VsSamples/Vs2012/HelloWorld.csproj");
                    this.Start_SourceProject(projectfile, VisualStudioVersion.VS2012);
                }
            }
            catch (Exception e)
            {
                this.OnComplete(new CompletedEventArgs(e));
            }

        }



        #region PreCompiled

        ProcessStarter Executable_ProcessStarter;

        private void Start_PreCompiled(string executable)
        {
            if (executable == null) throw new ArgumentNullException("executable");

            if (this.Executable_ProcessStarter != null) throw new InvalidOperationException("Project already running");

            if (string.IsNullOrEmpty(executable))
            {
                throw new ArgumentNullException("executable");
            }

            if (!File.Exists(executable))
            {
                throw new FileNotFoundException(executable);

            }

            this.Title = "Web Application";
            this.Description = "This application will start a web application demo";


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
                    //this.TemplateName = "Next";
                    //this.OnComplete(new CompletedEventArgs());
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

            this.Title = "Web Application";
            this.Description = "The code you will have in your Visual Studio window will demonstrated a web application.";
            string processName = "WebDev.WebServer40";  // TODO: Check for more ?
            string openfile = "hello.html"; // NOTE: No support for filename with spaces


            // 1. Start listening for process
            // Important to start this before so we can get the corret blacklist
            this.VisualStudio_ProjectListener = new VisualStudioProjectListener();
            this.VisualStudio_ProjectListener.Progress += new VisualStudioProjectListener.ProgressEventHandler(VisualStudio_ProjectListener_Progress);
            this.VisualStudio_ProjectListener.Complete += new VisualStudioProjectListener.CompleteEventHandler(VisualStudio_ProjectListener_Complete);
            this.VisualStudio_ProjectListener.Execute(ProjectType.VisualStudioWebApplication, processName);

            // 2. Start Visual studio with project
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

            //if (e.HasError)
            //{
            //    this.OnComplete(new CompletedEventArgs(e.Error));
            //}

            //// No need to listen for more events on VisualStudio
            //if (this.VisualStudio_ProcessStarter != null)
            //{
            //    this.VisualStudio_ProcessStarter.Abort();
            //    this.VisualStudio_ProcessStarter = null;
            //}
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

                    this.VisualStudio_ProjectListener.Abort();
                    this.OnComplete(new CompletedEventArgs());
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

            //this.VisualStudio_ProjectListener = null;

            //// No need to listen for more events on VisualStudio
            //if (this.VisualStudio_ProcessStarter != null)
            //{
            //    this.VisualStudio_ProcessStarter.Abort();
            //    this.VisualStudio_ProcessStarter = null;
            //}

            //if (e.HasError)
            //{
            //    this.OnComplete(new CompletedEventArgs(e.Error));
            //}

            CommandManager.InvalidateRequerySuggested();

        }

        #endregion



    }
}
