using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Administrator.Server.Utilities {
    /// <summary>
    /// Source: http://stackoverflow.com/questions/3753758/creating-virtual-hard-drive
    /// </summary>
    public class Subst {
        public static void MapDrive(char letter, string path) {
            if (!DefineDosDevice(0, devName(letter), path))
                throw new Win32Exception();
        }
        public static void UnmapDrive(char letter) {
            if (!DefineDosDevice(2, devName(letter), null))
                throw new Win32Exception();
        }
        public static void UnmapExactDrive(char letter, string path) {
            if (!DefineDosDevice(2 | 4, devName(letter), path))
                throw new Win32Exception();
        }
        public static string GetDriveMapping(char letter) {
            var sb = new StringBuilder(259);
            if (QueryDosDevice(devName(letter), sb, sb.Capacity) == 0) {
                // Return empty string if the drive is not mapped
                int err = Marshal.GetLastWin32Error();
                if (err == 2) return "";
                throw new Win32Exception();
            }
            return sb.ToString().Substring(4);
        }

        public static char? GetFreeDriveLetter() {
            char c = 'A';
            while (c != 'Z') {

                string result = GetDriveMapping(c);
                if (string.IsNullOrEmpty(result)) {
                    return c;
                };
                c++;
            }
            return null;
        }
        private static string devName(char letter) {
            return new string(char.ToUpper(letter), 1) + ":";
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DefineDosDevice(int flags, string devname, string path);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int QueryDosDevice(string devname, StringBuilder buffer, int bufSize);
    }

}
