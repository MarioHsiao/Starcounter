using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using P4API;
using System.IO;

namespace RapidMinds.BuildSystem.Common
{
    public class PerforceClient
    {
        public string Server { get; protected set; }
        public int Port { get; protected set; }
        public string User { get; protected set; }
        public string Password { get; protected set; }
        public string Depot { get; protected set; }

        public PerforceClient(string server, int port, string username, string password, string depot)
        {
            if (string.IsNullOrEmpty(server)) throw new ArgumentException("Invalid server", "server");
            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) throw new ArgumentOutOfRangeException("Invalid port", "port");
            if (string.IsNullOrEmpty(username)) throw new ArgumentException("Invalid user", "user");
            //            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Invalid password", "password");
            if (string.IsNullOrEmpty(depot)) throw new ArgumentException("Invalid depot", "depot");

            this.Server = server;
            this.Port = port;
            this.User = username;
            this.Password = password;
            this.Depot = depot;
        }


        public void SetLabel(String title, Version version)
        {
            if (version == null) throw new ArgumentNullException("version");

            // C:\perforce\Starcounter\Dev\Yellow\Main>p4 tag -l TestLabel2 //RapidMinds/SpeedGrid/SpeedGrid/...

            // C:\perforce\Starcounter\Dev\Yellow\Main>p4 tag -l TestLabel2 //RapidMinds/SpeedGrid/SpeedGrid/...@<REVISION>


            P4Connection connection = new P4Connection();
            connection.User = this.User;
            connection.Password = this.Password;
            connection.Port = string.Format("{0}:{1}", this.Server, this.Port);

            try
            {
                connection.Connect();
                string labelName = string.Format("{0} build-{1}", title, version.ToString());
                P4RecordSet set1 = connection.Run("tag", "-l", labelName, this.Depot);
                int filesAddedToLabel = set1.Records.Length;
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                connection.Disconnect();
            }
        }

        public void RemoveLabel(String title, Version version)
        {
            if (version == null) throw new ArgumentNullException("version");

            // Delete label
            // C:\perforce\Starcounter\Dev\Yellow\Main>p4 label -d TestLabel3

            // (DO NOT USE )Remove files in a label
            // C:\perforce\Starcounter\Dev\Yellow\Main>p4 tag -d -l TestLabel2 //RapidMinds/SpeedGrid/SpeedGrid/...

            P4Connection connection = new P4Connection();
            connection.User = this.User;
            connection.Password = this.Password;
            connection.Port = string.Format("{0}:{1}", this.Server, this.Port);

            try
            {
                connection.Connect();
                string labelName = string.Format("build-{0}", version.ToString());

                P4RecordSet set1 = connection.Run("label", "-d", labelName);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                connection.Disconnect();
            }
        }

        public void GenerateChangeLog(string outputFile)
        {
            P4Connection connection = new P4Connection();
            connection.User = this.User;
            connection.Password = this.Password;
            connection.Port = string.Format("{0}:{1}", this.Server, this.Port);

            try
            {
                connection.Connect();


                IList<PerforceLabel> labels = this.GetLabels(connection);

                PerforceLabel lable1 = null;
                PerforceLabel lable2 = null;
                List<PerforceChange> changes;

                TextWriter textWriter = new StreamWriter(outputFile);

                List<PerforceLabel> sortedLables = labels.OrderByDescending(item => item.Version).ToList<PerforceLabel>();

                for (int i = 0; i < sortedLables.Count; i++)
                {
                    lable1 = sortedLables[i];


                    // Header
                    textWriter.WriteLine("v{0}  ({1})", lable1.Version, lable1.Update.ToShortDateString());
                    textWriter.WriteLine("=".PadRight(40, '='));
                    textWriter.WriteLine();

                    //Console.WriteLine("v{0}  ({1})", lable1.Version, lable1.Update.ToShortDateString());
                    //Console.WriteLine("=".PadRight(40, '='));
                    //Console.WriteLine();

                    if (i >= (sortedLables.Count - 1))
                    {
                        break;
                    }

                    lable2 = sortedLables[i + 1];

                    changes = this.GetChangedBetweenLabled(connection, lable1, lable2);

                    if (changes.Count > 0)
                    {

                        foreach (PerforceChange change in changes)
                        {
                            textWriter.WriteLine(" * {0}", change.Description);
                            textWriter.WriteLine();

                            //Console.WriteLine(" * {0}", change.Description);
                            //Console.WriteLine();
                        }
                    }

                }

                textWriter.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                connection.Disconnect();
            }
        }

        /// <summary>
        /// Gets All perforce labels in a depot
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        private List<PerforceLabel> GetLabels(P4Connection connection)
        {
            List<PerforceLabel> list = new List<PerforceLabel>();

            P4RecordSet labelSet = connection.Run("labels", this.Depot);

            foreach (P4Record record in labelSet.Records)
            {
                PerforceLabel perforceLabel = this.RecordToLabel(connection, record);
                if (perforceLabel.Version == null)
                {
                    // Skipp non version lables
                    continue;
                }
                list.Add(perforceLabel);

            }

            return list;

        }


        private List<PerforceChange> GetChangedBetweenLabled(P4Connection connection, PerforceLabel lable1, PerforceLabel lable2)
        {

            List<PerforceChange> list = new List<PerforceChange>();

            P4RecordSet set1 = connection.Run("changes", "-m", "1", "-s", "submitted", "@" + lable1.Lable);
            P4Record record1 = set1.Records[0] as P4Record;
            PerforceChange change1 = this.RecordToChange(connection, record1);


            P4RecordSet set2 = connection.Run("changes", "-m", "1", "-s", "submitted", "@" + lable2.Lable);
            P4Record record2 = set2.Records[0] as P4Record;
            PerforceChange change2 = this.RecordToChange(connection, record2);


            if (change1.Change == change2.Change)
            {
                return list;
            }

            int from = Math.Min(change1.Change, change2.Change);
            int to = Math.Max(change1.Change, change2.Change);


            // p4 changes //depot/...@201,@250
            P4RecordSet set3 = connection.Run("changes", "-l", "-s", "submitted", this.Depot + "@" + (from + 1) + ",@" + to);    // TODO: Lägsta värdet först

            foreach (P4Record record in set3.Records)
            {
                PerforceChange perforceChange = this.RecordToChange(connection, record);
                list.Add(perforceChange);
            }


            return list;
        }

        private PerforceChange RecordToChange(P4Connection connection, P4Record record)
        {
            string change = record["change"] as String;
            string status = record["status"] as String;
            string desc = record["desc"] as String;
            string client = record["client"] as String;
            string time = record["time"] as String;
            string user = record["user"] as String;

            DateTime timeDate = connection.ConvertDate(int.Parse(time));
            int changeInt = int.Parse(change);

            desc = desc.TrimEnd(new char[] { '\n', '\r' });

            return new PerforceChange(changeInt, status, desc, client, timeDate, user);

        }


        private PerforceLabel RecordToLabel(P4Connection connection, P4Record record)
        {
            string label = record["label"] as String;
            string access = record["Access"] as String;
            string update = record["Update"] as String;
            string description = record["Description"] as String;
            string owner = record["Owner"] as String;
            string options = record["Options"] as String;

            DateTime accessDate = connection.ConvertDate(int.Parse(access));
            DateTime updateDate = connection.ConvertDate(int.Parse(update));

            return new PerforceLabel(label, accessDate, updateDate, description, owner, options);

        }

    }

    public class PerforceLabel
    {

        public string Lable { get; protected set; }
        public DateTime Access { get; protected set; }
        public DateTime Update { get; protected set; }
        public string Description { get; protected set; }
        public string Owner { get; protected set; }
        public string Options { get; protected set; }
        public Version Version { get; protected set; }


        public PerforceLabel(string label, DateTime access, DateTime update, string description, string owner, string options)
        {
            this.Lable = label;
            this.Access = access;
            this.Update = update;
            this.Description = description;
            this.Owner = owner;
            this.Options = options;

            string versionStr = this.Lable.Substring(6); // build-1.0.59.0

            try
            {
                this.Version = new Version(versionStr);
            }
            catch (Exception)
            {
                this.Version = null;
            }


        }


    }

    public class PerforceChange
    {

        public int Change { get; protected set; }
        public string Status { get; protected set; }
        public string Description { get; protected set; }
        public string Client { get; protected set; }
        public DateTime Time { get; protected set; }
        public string User { get; protected set; }



        public PerforceChange(int change, string status, string description, string client, DateTime time, string user)
        {
            this.Change = change;
            this.Status = status;
            this.Description = description;
            this.Client = client;
            this.Time = time;
            this.User = user;
        }


    }

}
