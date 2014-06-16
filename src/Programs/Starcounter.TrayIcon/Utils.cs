using Starcounter.Advanced;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Tools {

    /// <summary>
    /// 
    /// </summary>
    public class Utils {


        /// <summary>
        /// Get starcounter system port
        /// </summary>
        /// <param name="port"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static bool GetPort(out ushort port, out string error) {

            port = 0;
            error = null;

            string file = Path.Combine(
                StarcounterEnvironment.Directories.InstallationConfiguration, 
                StarcounterEnvironment.FileNames.InstallationServerConfigReferenceFile
                );
            string serverDir;

            bool result = ReadConfiguration(file, out serverDir, out error);
            if (result == false) {
                return false;
            }

            string serverConfig = Path.Combine(serverDir, "Personal.server.config");

            result = ReadServerConfiguration(serverConfig, out port, out error);
            if (result == false) {
                return false;
            }

            return true;
        }


        /// <summary>
        /// Get Server folder
        /// </summary>
        /// <param name="file"></param>
        /// <param name="serverDir"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool ReadConfiguration(string file, out string serverDir, out string error) {

            string result;
            serverDir = null;
            error = null;

            bool success = ReadConfigFile(file, MixedCodeConstants.ServerConfigDirName, out result, out error);
            if (success) {

                serverDir = result;
                if (!Directory.Exists(serverDir)) {
                    error = string.Format("Invalid server folder {0} ", serverDir);
                    return false;
                }

                return true;
            }
            return false;
        }


        /// <summary>
        /// Get port number from server configuration file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="port"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool ReadServerConfiguration(string file, out ushort port, out string error) {

            string result;
            port = 0;
            error = null;

            bool success = ReadConfigFile(file, "SystemHttpPort", out result, out error);
            if (success) {
                if (ushort.TryParse(result, out port)) {
                    if (port > IPEndPoint.MaxPort || port < IPEndPoint.MinPort) {
                        error = string.Format("Invalid port number {0}.", port);
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Get a tag value from a xml configuration file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="tag"></param>
        /// <param name="result"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool ReadConfigFile(string file, string tag, out string result, out string error) {

            result = null;
            error = null;

            if (!File.Exists(file)) {
                error = string.Format("Missing {0} configuration file.", file);
                return false;
            }

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines) {

                if (line.StartsWith("//")) continue;



                int startIndex = line.IndexOf("<" + tag + ">", StringComparison.CurrentCultureIgnoreCase);
                if (startIndex != -1) {

                    int len = tag.Length + 2;
                    int endIndex = line.IndexOf("</" + tag + ">", startIndex + len, StringComparison.CurrentCultureIgnoreCase);

                    result = line.Substring(startIndex + len, endIndex - (startIndex + len));
                    return true;
                }
            }

            error = string.Format("Failed to find the <{0}> tag in {1}", tag, file);
            return false;
        }


        #region "Refresh Notification Area Icons"

        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT {
            /// <summary>
            /// 
            /// </summary>
            public int left;
            /// <summary>
            /// 
            /// </summary>
            public int top;
            /// <summary>
            /// 
            /// </summary>
            public int right;
            /// <summary>
            /// 
            /// </summary>
            public int bottom;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lpClassName"></param>
        /// <param name="lpWindowName"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hwndParent"></param>
        /// <param name="hwndChildAfter"></param>
        /// <param name="lpszClass"></param>
        /// <param name="lpszWindow"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lpRect"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        /// <summary>
        /// 
        /// </summary>
        public static void RefreshTrayArea() {
            IntPtr systemTrayContainerHandle = FindWindow("Shell_TrayWnd", null);
            IntPtr systemTrayHandle = FindWindowEx(systemTrayContainerHandle, IntPtr.Zero, "TrayNotifyWnd", null);
            IntPtr sysPagerHandle = FindWindowEx(systemTrayHandle, IntPtr.Zero, "SysPager", null);
            IntPtr notificationAreaHandle = FindWindowEx(sysPagerHandle, IntPtr.Zero, "ToolbarWindow32", "Notification Area");
            if (notificationAreaHandle == IntPtr.Zero) {
                notificationAreaHandle = FindWindowEx(sysPagerHandle, IntPtr.Zero, "ToolbarWindow32", "User Promoted Notification Area");
                IntPtr notifyIconOverflowWindowHandle = FindWindow("NotifyIconOverflowWindow", null);
                IntPtr overflowNotificationAreaHandle = FindWindowEx(notifyIconOverflowWindowHandle, IntPtr.Zero, "ToolbarWindow32", "Overflow Notification Area");
                RefreshTrayArea(overflowNotificationAreaHandle);
            }
            RefreshTrayArea(notificationAreaHandle);
        }


        private static void RefreshTrayArea(IntPtr windowHandle) {
            const uint wmMousemove = 0x0200;
            RECT rect;
            GetClientRect(windowHandle, out rect);
            for (var x = 0; x < rect.right; x += 5)
                for (var y = 0; y < rect.bottom; y += 5)
                    SendMessage(windowHandle, wmMousemove, 0, (y << 16) + x);
        }
        #endregion
    }
}
