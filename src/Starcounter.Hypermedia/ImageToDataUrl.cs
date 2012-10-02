
using System;
using System.IO;
namespace Starcounter.Internal.WebServing {
    public class ImageToDataUrl {

        public static string GetDataURL(string imgFile) {
            return "<img src=\"data:image/"
                        + Path.GetExtension(imgFile).Replace(".", "")
                        + ";base64,"
                        + Convert.ToBase64String(File.ReadAllBytes(imgFile)) + "\" />";
        }
    }
}
