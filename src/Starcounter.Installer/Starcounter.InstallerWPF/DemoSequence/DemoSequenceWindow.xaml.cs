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
using System.Windows.Shapes;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

// Free icons
// http://www.iconfinder.com/search/3/?q=iconset%3Aoxygen

namespace Starcounter.InstallerWPF.DemoSequence
{
    /// <summary>
    /// Interaction logic for DemoSequenceWindow.xaml
    /// </summary>
    public partial class DemoSequenceWindow : Window
    {
        #region Properties

        public Settings Settings { get; set; }
        #endregion

        #region Commands

        #region Close
        public static RoutedCommand CloseRoutedCommand = new RoutedCommand();

        private void CanExecute_Close_Command(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = true;
        }

        private void Executed_Close_Command(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region GoToPage

        private void CanExecute_GoToPage_Command(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = true;
        }

        private void Executed_GoToPage_Command(object sender, ExecutedRoutedEventArgs e)
        {

            if (!string.IsNullOrEmpty(e.Parameter as string))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(e.Parameter as string));
                    e.Handled = true;
                }
                catch (Win32Exception ee)
                {
                    string message = "Can not open external browser." + Environment.NewLine + ee.Message + Environment.NewLine + e.Parameter;
                    MessageBox.Show(message, "Open Webbrowser", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        }


        #endregion


        #endregion

        public DemoSequenceWindow()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += new MouseButtonEventHandler(DemoSequenceWindow_MouseLeftButtonDown);

            this.Activated += new EventHandler(DemoSequenceWindow_Activated);
            this.Deactivated += new EventHandler(DemoSequenceWindow_Deactivated);
            this.MouseEnter += new MouseEventHandler(DemoSequenceWindow_MouseEnter);
            this.MouseLeave += new MouseEventHandler(DemoSequenceWindow_MouseLeave);
        }

        void DemoSequenceWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            if (this.IsActive == false)
            {
                this.Opacity = 0.5;
            }
           
        }

        void DemoSequenceWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Opacity = 1;
            
        }

        void DemoSequenceWindow_Deactivated(object sender, EventArgs e)
        {
            if (this.IsMouseOver == false)
            {
                this.Opacity = 0.5;
            }
            
        }

        void DemoSequenceWindow_Activated(object sender, EventArgs e)
        {
            this.Opacity = 1;
            
        }
   

        void DemoSequenceWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        public void Start(String installationPath, String postDemoType)
        {
            Settings settings = new Settings();
            settings.InstallationPath = installationPath;

            // Checking each post demo type.
            foreach (PostDemoTypeEnum e in Enum.GetValues(typeof(PostDemoTypeEnum)))
            {
                if (e.ToString() == postDemoType)
                {
                    settings.PostDemoType = e;
                    break;
                }
            }

            this.Settings = settings;

            this.Start_Benchmark_Demo();

            this.Show();
        }

        #region Benchmark Demo
        private void Start_Benchmark_Demo()
        {
            BenchmarkDemoControl benchmarkDemoControl = new BenchmarkDemoControl();
            benchmarkDemoControl.Complete += new BenchmarkDemoControl.CompleteEventHandler(benchmarkDemoControl_Complete);
            benchmarkDemoControl.Loaded += new RoutedEventHandler(benchmarkDemoControl_Loaded);

            this.PART_Content.Content = benchmarkDemoControl;
        }

        void benchmarkDemoControl_Loaded(object sender, RoutedEventArgs e)
        {
            BenchmarkDemoControl benchmarkDemoControl = sender as BenchmarkDemoControl;
            benchmarkDemoControl.Loaded -= new RoutedEventHandler(benchmarkDemoControl_Loaded);
            benchmarkDemoControl.Start(this.Settings);
        }

        void benchmarkDemoControl_Complete(object sender, CompletedEventArgs e)
        {
            if (e.HasError)
            {
                if (e.Error is FileNotFoundException)
                {
                    string message = e.Error.Message + Environment.NewLine + Environment.NewLine;
                    message += "The demonstration will continue with the next step";
                    MessageBox.Show(message, "Demonstration - File Not Found", MessageBoxButton.OK, MessageBoxImage.Error); // TODO
                }
                else if (e.Error is InvalidProgramException)
                {
                    string message = e.Error.Message + Environment.NewLine + Environment.NewLine;
                    message += "The demonstration will continue with the next step";
                    MessageBox.Show(message, "Demonstration", MessageBoxButton.OK, MessageBoxImage.Error); // TODO
                }
                else if (e.Error is Win32Exception && ((Win32Exception)e.Error).ErrorCode == -2147467259)// UAC-User denied access  
                {
                    MessageBox.Show(e.Error.Message, "Demonstration", MessageBoxButton.OK, MessageBoxImage.Error); // TODO
                }
                else
                {
                    MessageBox.Show(e.Error.ToString(), "Demonstration - Internal Error", MessageBoxButton.OK, MessageBoxImage.Error); // TODO
                }

                // Go to next step
                this.Start_WebApplication_Demo();


            }
            else
            {
                // Go to next step
                this.Start_WebApplication_Demo();
            }
        }

        #endregion

        #region Web Application Demo

        private void Start_WebApplication_Demo()
        {
            this.TheEnd();  // TODO:  Remove this when we got the WebApplication demo
            return;         // TODO:  Remove this when we got the WebApplication demo

            /*WebApplicationDemoControl webApplicationDemoControl = new WebApplicationDemoControl();
            webApplicationDemoControl.Complete += new BaseDemoControl.CompleteEventHandler(webApplicationDemoControl_Complete);
            webApplicationDemoControl.Loaded += new RoutedEventHandler(webApplicationDemoControl_Loaded);
            this.PART_Content.Content = webApplicationDemoControl;*/
        }

        void webApplicationDemoControl_Loaded(object sender, RoutedEventArgs e)
        {

            WebApplicationDemoControl webApplicationDemoControl = sender as WebApplicationDemoControl;
            webApplicationDemoControl.Loaded -= new RoutedEventHandler(webApplicationDemoControl_Loaded);
            webApplicationDemoControl.Start(this.Settings);
        }

        void webApplicationDemoControl_Complete(object sender, CompletedEventArgs e)
        {
            if (e.HasError)
            {

                if (e.Error is FileNotFoundException)
                {
                    string message = e.Error.Message + Environment.NewLine + Environment.NewLine;
                    message += "The demonstration will continue with the next step";
                    MessageBox.Show(message, "Demonstration - File Not Found", MessageBoxButton.OK, MessageBoxImage.Error); // TODO
                }
                else if (e.Error is InvalidProgramException)
                {
                    string message = e.Error.Message + Environment.NewLine + Environment.NewLine;
                    message += "The demonstration will continue with the next step";
                    MessageBox.Show(message, "Demonstration", MessageBoxButton.OK, MessageBoxImage.Error); // TODO
                }
                else if (e.Error is Win32Exception && ((Win32Exception)e.Error).ErrorCode == -2147467259)// UAC-User denied access  
                {
                    MessageBox.Show(e.Error.Message, "Demonstration", MessageBoxButton.OK, MessageBoxImage.Error); // TODO
                }
                else
                {
                    MessageBox.Show(e.Error.ToString(), "Demonstration - Internal Error", MessageBoxButton.OK, MessageBoxImage.Error); // TODO
                }

                // Go to next step
                this.TheEnd();
            }
            else
            {
                // Go to next step
                this.TheEnd();
            }
        }

        #endregion

        private void TheEnd()
        {
            this.PART_Content.Content = null; // DataContext
            this.PART_Content.ContentTemplate = this.FindResource("theend_template") as DataTemplate;
        }

    }

    public class Settings
    {
        public String InstallationPath { get; set; }
        public PostDemoTypeEnum PostDemoType { get; set; }
    }


    public enum ProjectType
    {
        VisualStudioWebApplication,     // WebDev.WebServer40
        VisualStudioEXEApplication     // .vshost.exe
    }
}
