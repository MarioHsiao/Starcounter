using Starcounter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildHelpers
{
    enum ModeType
    {
        None,
        UploadBigFile
    };

    enum ReturnCodes
    {
        Success,
        WrongPutResponse,
        CantSendFile
    }

    class Program
    {
        static Int32 Main(string[] args)
        {
            ModeType mode = new ModeType();

            foreach (String arg in args)
            {
                if (arg.StartsWith("-Mode="))
                {
                    String modeString = arg.Substring("-Mode=".Length);

                    if (modeString == "UploadBigFile")
                        mode = ModeType.UploadBigFile;
                }
            }

            if (mode == ModeType.UploadBigFile)
            {
                String uploadUri = null, pathToFile = null;

                for (Int32 i = 0; i < args.Length; i++)
                {
                    if (args[i].StartsWith("-UploadUri="))
                    {
                        uploadUri = args[i].Substring("-UploadUri=".Length);

                        if (uploadUri.StartsWith("\""))
                            uploadUri = uploadUri.Substring(1, uploadUri.Length - 2);
                    }
                    else if (args[i].StartsWith("-PathToFile="))
                    {
                        pathToFile = args[i].Substring("-PathToFile=".Length);
                        if (pathToFile.StartsWith("\""))
                            pathToFile = pathToFile.Substring(1, pathToFile.Length - 2);
                    }
                }

                Response resp = X.POST(uploadUri, (String) null, null);
                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine("Problems obtaining resource from \"" + uploadUri + "\" Status description: " + resp.Body);
                    return (Int32) ReturnCodes.WrongPutResponse;
                }

                String resId = resp.Body;
               
                using (FileStream fs = File.OpenRead(pathToFile))
                {
                    Int32 readBytes;
                    Byte[] tempBuf = new Byte[1024 * 1024];

                    while ((readBytes = fs.Read(tempBuf, 0, tempBuf.Length)) > 0)
                    {
                        if (readBytes < tempBuf.Length)
                        {
                            Byte[] truncatedBuf = new Byte[readBytes];
                            Array.Copy(tempBuf, truncatedBuf, readBytes);

                            resp = X.PUT(uploadUri + "/" + resId, truncatedBuf, "UploadSettings: Final\r\n");
                        }
                        else
                        {
                            resp = X.PUT(uploadUri + "/" + resId, tempBuf, null);
                        }

                        // Checking response status code.
                        if (!resp.IsSuccessStatusCode)
                            return (Int32)ReturnCodes.CantSendFile;
                    }
                }
            }

            Console.WriteLine("Operation completed successfully!");

            return (Int32) ReturnCodes.Success;
        }
    }
}
