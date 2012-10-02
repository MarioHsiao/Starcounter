using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;

namespace Starcounter.Internal.ExeModule
{

    /// <summary>
    /// Implements a named pipes notication to the server (Starcounter Node) with the instruction to start
    /// this module (the main method of this .EXE assembly).
    /// </summary>
    public class TellServerToStartMe
    {
        public static void TellServer()
        {
            // TODO: Establish connection to boot!
            return;

            Stopwatch sw = Stopwatch.StartNew();
            //          bool isDebug = System.Diagnostics.Debugger.IsAttached;
            //          System.Diagnostics.Debugger.Launch();
            Process[] servers;
            string fileSpec = System.Environment.GetCommandLineArgs()[0];
            if (fileSpec.EndsWith(".vshost.exe"))
                fileSpec = fileSpec.Substring(0, fileSpec.Length - 11) + ".exe";
            int timeout = 2000;
            do
            {
                servers = Process.GetProcessesByName("boot");

                if (servers.Length == 0)
                {
                    servers = Process.GetProcessesByName("Fakeway.vshost");
                }

                if (servers.Length == 0)
                {
                    servers = Process.GetProcessesByName("Fakeway");
                }

                if (servers.Length == 0)
                {
                    servers = Process.GetProcessesByName("scdbs");
                }

                if (servers.Length == 0)
                {
                    servers = Process.GetProcessesByName("scdbsw");
                }

                if (servers.Length == 0)
                {
                    MessageBox(IntPtr.Zero, "No Starcounter Host is running on this machine. Have tried Fakeway and Fakeway.vshost.", "Cannot start module", 0x00000030);
                    return;
                }
            } while (servers.Length == 0 && sw.ElapsedMilliseconds < timeout);

            if (servers.Length > 1)
            {
                MessageBox(IntPtr.Zero, "Unclear where to start this module. There are multiple Starcounter user code Hosts running on this machine.", "Cannot start module", 0x00000030);
                //                Console.WriteLine("Unclear where to start this module. There are multiple Starcounter Nodes running on this machine.x");
                return;
            }

            string appName = AppDomain.CurrentDomain.SetupInformation.ApplicationName;
            string workingDir = System.Environment.CurrentDirectory;
            string[] args = System.Environment.GetCommandLineArgs();

            RegisterApp(appName, fileSpec, workingDir, args);
        }

        public static void RegisterApp(String appName, String dllOrExePath, String workingDir, String[] startupArgs)
        {
            if (appName == null) throw new ArgumentNullException("appName");
            if (dllOrExePath == null) throw new ArgumentNullException("dllOrExePath");

            NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "StarcounterHost", PipeDirection.Out);
            try
            {
                pipeClient.Connect(15000);
            }
            catch (Exception e)
            {
                MessageBox(IntPtr.Zero, "The Starcounter Host running on this machine is not responding. " + e.Message, "Cannot start module", 0x00000030);
                return;
            }

            if (pipeClient.IsConnected && pipeClient.CanWrite)
            {
                AppExeModule exe = new AppExeModule()
                {
                    ExeFileSpec = dllOrExePath,
                    WorkingDirectory = workingDir,
                };
                exe.ParseCommandLineArgs(startupArgs);
                byte[] arrayData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(exe));
                pipeClient.Write(arrayData, 0, arrayData.Length);
            }
        }

        // Use DllImport to import the Win32 MessageBox function.
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);
    }


}