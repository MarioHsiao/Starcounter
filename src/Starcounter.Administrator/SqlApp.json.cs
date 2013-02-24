//#define FAKERESULT

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

            Console.WriteLine("Executing SQL command in the database {0}{1}{2}", this.DatabaseName, Environment.NewLine, this.Query);

            try {

#if FAKERESULT
                //string jsonResult = "\"Result\" : {Columns: [{\"ID\":\"Mucho1\",\"Title\":\"Column 1\"},{\"ID\":\"Mucho99\",\"Title\":\"Column 2\"}],\"Items\": [{\"ID\":\"Mucho1\",\"Value\":1},{\"ID\":\"Mucho2\",\"Value\":2},{\"ID\":\"Mucho3\",\"Value\":3},{\"ID\":\"Mucho4\",\"Value\":4},{\"ID\":\"Mucho5\",\"Value\":5},{\"ID\":\"Mucho6\",\"Value\":6},{\"ID\":\"Mucho7\",\"Value\":7},{\"ID\":\"Mucho8\",\"Value\":8},{\"ID\":\"Mucho9\",\"Value\":9},{\"ID\":\"Mucho10\",\"Value\":10},{\"ID\":\"Mucho11\",\"Value\":11},{\"ID\":\"Mucho12\",\"Value\":12},{\"ID\":\"Mucho13\",\"Value\":13},{\"ID\":\"Mucho14\",\"Value\":14}],\"Count\" : 14,\"Offset\" : 0}";
                string jsonResult = "{ Columns:[{ ID:'col1', Title:'Column 1'},{ ID:'col2', Title:'Column 2'}], Items:[{Test:'abc',Num:1},{Test:'def',Num:2}], Count:2, Offset:4 }";
#else
                string jsonResult = this.PostQuery(this.Query);
#endif

                object obj = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResult);

                this.Output = jsonResult;

                /*
                                //Newtonsoft.Json.Linq.JArray results = JsonConvert.DeserializeObject<dynamic>(jsonResult);
                                var results = JsonConvert.DeserializeObject<dynamic>(jsonResult);

                                foreach (var acol in results.Columns) {

                                    string id = acol.ID;
                                    string title = acol.Title;
                                    Console.WriteLine("ID:{0} Title:{1}", id, title);
                                    ColumnsApp columnsApp = new ColumnsApp();
                                    columnsApp.ID = id;
                                    columnsApp.Title = title;
                                    this.Columns.Add(columnsApp);
                                }

                //                var it = results.Items;
                //                this.ItemsStr = ((Newtonsoft.Json.Linq.JArray)it).ToString();

                                //foreach (var acol in results.Items) {

                                //    //string id = acol["Test"];
                                //    //string title = acol["Num"];
                                //    //Console.WriteLine("ID:{0} Value:{1}", id, title);

                                //    ItemsApp itemsApp = new ItemsApp();

                   
                                //    //itemsApp.Data = acol;

                                //    this.Items.Add(itemsApp);
                                //}

                                this.Items = results.Items;
                                this.Count = results.Count;
                                this.Offset = results.Offset;

                                //var id = results.Id;
                                //var name = results.Name;
                                //Newtonsoft.Json.Linq.JArray col = results.Columns;
                                //foreach (var obj2 in results[0].Children()) {
                                //    Console.WriteLine(obj2.ToString());

                                //}
                */
            }
            catch (Exception e) {
                this.Output = e.ToString();
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

                UriBuilder uribuilder = new UriBuilder();

                NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);
                queryString["offset"] = "value1";
                queryString["rows"] = "value2";


                string URI = string.Format("http://localhost:{0}/{1}/__sql", this.Port, this.DatabaseName);
                if (queryString.HasKeys()) {
                    URI = URI + "?" + queryString.ToString(); // URL-encoded
                }

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
                Console.WriteLine(e.ToString());
                return e.ToString(); // TODO
            }


        }



    }
}

