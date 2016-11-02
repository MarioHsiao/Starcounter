using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Starcounter.Apps.Package {
    public partial class Archive {

        //public static Archive Load(string file) {

        //    Archive archive = new Archive();

        //    using (FileStream stream = new FileStream(file, FileMode.Open)) {

        //        using (ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Read)) {

        //            ZipArchiveEntry entry = zipArchive.GetEntry("package.config");
        //            archive.Config = PackageConfigFile.Deserialize(entry.Open());
        //            archive.VerifyConfiguration();
        //        }
        //    }

        //    archive.FileName = file;

        //    return archive;

        //}


        public static void Install(string host, ushort port, string databaseName, string file) {

            if (host == null) throw new ArgumentNullException("host");
            if (databaseName == null) throw new ArgumentNullException("databaseName");
            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
                throw new ArgumentException("port");
            }
            if (file == null) throw new ArgumentNullException("file");


            if (!File.Exists(file)) {
                throw new InputErrorException(string.Format("Archive file not found ({0})", file));
            }

            string fileName = Path.GetFileName(file);
            long fileSize = new FileInfo(file).Length;

            try {
                string uri = string.Format("ws://{0}:{1}/api/admin/databases/{2}/applicationuploadws", host, port, databaseName);
                UploadFile(uri, file).Wait();
            }
            catch (Exception e) {

                WebSocketException webSocketException = e.InnerException as WebSocketException;

                if (webSocketException != null) {

                    WebException webException = webSocketException.InnerException as WebException;
                    if (webException != null) {

                        HttpWebResponse httpWebResponse = webException.Response as HttpWebResponse;

                        if (httpWebResponse != null) {
                            if (httpWebResponse.StatusCode == HttpStatusCode.NotFound) {
                                throw new InputErrorException(string.Format("Database name not found ({0})", databaseName));
                            }
                        }
                    }

                    string errorMessage = new Win32Exception(webSocketException.NativeErrorCode).Message;
                    throw new InputErrorException(errorMessage);
                }
            }
        }


        private static async Task UploadFile(string uri, string file) {

            ClientWebSocket webSocket = null;

            try {
                webSocket = new ClientWebSocket();
                await webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);

                byte[] buffer = new byte[1024]; // Packet size

                Stream stream = System.IO.File.OpenRead(file);
                ArraySegment<byte> segment;
                while (stream.Position != stream.Length) {
                    int numBytes = stream.Read(buffer, 0, buffer.Length);
                    segment = new ArraySegment<byte>(buffer, 0, numBytes);
                    await webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, CancellationToken.None);
                }

                //byte[] inputBuffer = new byte[1024]; // Packet size
                //ArraySegment<byte> inputSegment = new ArraySegment<byte>(inputBuffer, 0, inputBuffer.Length);
                //await webSocket.ReceiveAsync(inputSegment, CancellationToken.None);

                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "close text", CancellationToken.None);
            }
            finally {
                if (webSocket != null) {


                    WebSocketCloseStatus? s = webSocket.CloseStatus;
                    string t = webSocket.CloseStatusDescription;

                    webSocket.Dispose();
                }
            }
        }

        //await Task.WhenAll(Receive(webSocket));

        //private static async Task Receive(ClientWebSocket webSocket) {
        //    byte[] buffer = new byte[1024];
        //    while (webSocket.State == WebSocketState.Open) {
        //        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //        if (result.MessageType == WebSocketMessageType.Close) {
        //            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        //        }
        //        else {
        //            //LogStatus(true, buffer, result.Count);
        //        }
        //    }
        //}
    }
}
