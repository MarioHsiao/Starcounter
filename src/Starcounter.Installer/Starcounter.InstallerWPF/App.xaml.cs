using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Runtime.CompilerServices;
using Starcounter.Controls;
using System.Xml;

namespace Starcounter.InstallerWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public App()
        {
            //System.Diagnostics.Debugger.Launch();

            // Showing main setup window.
            InitializationWindow window = new InitializationWindow();
            window.Show();
        }
    }
}