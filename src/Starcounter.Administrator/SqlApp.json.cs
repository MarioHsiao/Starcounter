using System;
using System.IO;
using System.Net;
using Starcounter;
using System.Collections.Specialized;
using System.Web;
using Newtonsoft.Json;

namespace StarcounterAppsLogTester {
    partial class SqlApp : Puppet {

        /// <summary>
        /// TODO: This is just a concept code
        /// </summary>
        /// <param name="action"></param>
        void Handle(Input.Execute action) {

            Console.WriteLine("Executing SQL command in the database {0}{1}", Environment.NewLine, this.Query);

            try {
                string jsonResult = this.PostQuery(this.Query);
                this.Output = jsonResult;
                this.Exception = null;
            }
            catch (Exception e) {
                this.Exception = e.ToString();
            }

        }

        /// <summary>
        /// TODO: This is just a concept code
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private string PostQuery(string query) {

            try {

                System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                byte[] buffer = encoding.GetBytes(query);


                //string URI = string.Format("http://localhost:{0}/sql", 8282);
                string URI = string.Format("http://localhost:{0}/__sql/{1}", this.Port, this.DatabaseName);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URI);
                request.Method = "POST";
                request.ContentLength = buffer.Length;

                ((HttpWebRequest)request).UserAgent = ".NET Framework Example Client";
                Stream dataStream = request.GetRequestStream();

                dataStream.Write(buffer, 0, buffer.Length);
                dataStream.Close();

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                Stream responseStream = response.GetResponseStream();

                StreamReader reader = new StreamReader(responseStream);
                string responseFromServer = reader.ReadToEnd();

                response.Close();
                return responseFromServer;
            }
            catch (Exception e) {
                throw e;
            }


        }


    }
}

