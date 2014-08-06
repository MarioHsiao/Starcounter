using Microsoft.Win32;
using System;
using System.Text;
using System.Windows;

namespace PickFileDialog {

    /// <summary>
    /// Let the user pick a file and writes the picked file to console
    /// External programs may use this to retrive a selected file
    /// </summary>
    public partial class App : Application {

        public App() {

            string[] args = Environment.GetCommandLineArgs();

            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "Application files (*.exe)|*.exe|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.Multiselect = false;

            if (args.Length > 1) {
                openFileDialog.Title = args[1];
            }

            string responsebody = "[";
            int exitCode = 1;
            if (openFileDialog.ShowDialog() == true) {

                int cnt = 0;
                foreach (string filename in openFileDialog.FileNames) {
                    if (cnt != 0) {
                        responsebody += ",";
                    }
                    responsebody += string.Format("{{\"file\":\"{0}\"}}", EscapeStringValue(filename));
                    cnt++;
                }
                exitCode = 0;
            }

            responsebody += "]";

            Console.Write(responsebody);

            this.Shutdown(exitCode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string EscapeStringValue(string value) {
            const char BACK_SLASH = '\\';
            const char SLASH = '/';
            const char DBL_QUOTE = '"';

            var output = new StringBuilder(value.Length);
            foreach (char c in value) {
                switch (c) {
                    case SLASH:
                        output.AppendFormat("{0}{1}", BACK_SLASH, SLASH);
                        break;

                    case BACK_SLASH:
                        output.AppendFormat("{0}{0}", BACK_SLASH);
                        break;

                    case DBL_QUOTE:
                        output.AppendFormat("{0}{1}", BACK_SLASH, DBL_QUOTE);
                        break;

                    default:
                        output.Append(c);
                        break;
                }
            }

            return output.ToString();
        }


    }
}
