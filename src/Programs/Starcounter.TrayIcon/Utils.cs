using Starcounter.Advanced;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Tools {

    /// <summary>
    /// 
    /// </summary>
    public class Utils {


        /// <summary>
        /// 
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static Image MakeGrayscale(Image original) {
            Image newBitmap = new Bitmap(original.Width, original.Height);
            Graphics g = Graphics.FromImage(newBitmap);
            ColorMatrix colorMatrix = new ColorMatrix(
                new float[][] 
                {
                    new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                    new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                    new float[] {.114f, .114f, .114f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}
                });

            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);
            g.DrawImage(
                original,
                new Rectangle(0, 0, original.Width, original.Height),
                0, 0, original.Width, original.Height,
                GraphicsUnit.Pixel, attributes);

            g.Dispose();
            return newBitmap;
        }

        //public static BitmapSource GenerateBitmapSource(Visual visual, double renderWidth, double renderHeight) {
        //    var bmp = new RenderTargetBitmap((int)renderWidth, (int)renderHeight, 96, 96, PixelFormats.Pbgra32);
        //    var dv = new DrawingVisual();
        //    using (DrawingContext dc = dv.RenderOpen()) {
        //        dc.DrawRectangle(new VisualBrush(visual), null, new Rect(0, 0, renderWidth, renderHeight));   
        //    }
        //    bmp.Render(dv);
        //    return bmp;
        //}


        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static bool GetPort(out ushort port, out string error) {

            port = 0;
            error = null;

            string file = "personal.xml";
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
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <param name="serverDir"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool ReadConfiguration(string file, out string serverDir, out string error) {

            string result;
            serverDir = null;
            error = null;

            bool success = ReadConfigFile(file, "server-dir", out result, out error);
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
        /// 
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
        /// 
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
                error = string.Format("Missing configuration file {0}.", file);
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


    }
}
