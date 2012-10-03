using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace RapidMinds.BuildSystem.Common.Tasks
{

    public class BuildChangeLog
    {


        #region Properties

        public string Executable { get; protected set; }

        public string Server { get; protected set; }
        public int Port { get; protected set; }
        public string User { get; protected set; }
        public string Password { get; protected set; }
        public string Depot { get; protected set; }
        public string OutputFile { get; protected set; }

        #endregion

        public BuildChangeLog(string executable, string server, int port, string user, string password, string depot, string outputfile)
        {
            if (string.IsNullOrEmpty(executable)) throw new ArgumentException("Invalid executable", "executable");

            if (string.IsNullOrEmpty(server)) throw new ArgumentException("Invalid server", "server");
            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) throw new ArgumentOutOfRangeException("Invalid port", "port");
            if (string.IsNullOrEmpty(user)) throw new ArgumentException("Invalid user", "user");
            //            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Invalid password", "password");
            if (string.IsNullOrEmpty(depot)) throw new ArgumentException("Invalid depot", "depot");
            if (string.IsNullOrEmpty(outputfile)) throw new ArgumentException("Invalid outputfile", "outputfile");

            this.Executable = executable;
            this.Server = server;
            this.Port = port;
            this.User = user;
            this.Password = password;
            this.Depot = depot;
            this.OutputFile = outputfile;

        }


        public void Execute()
        {

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.Error.WriteLine("------ Build started: Creating changelog ------");
            Console.ResetColor();

            ExecuteCommandLineTool executeTask = new ExecuteCommandLineTool();


            StringBuilder arguments = new StringBuilder();
            arguments.Append(this.Server);
            arguments.Append(" ");
            arguments.Append(this.Port.ToString());
            arguments.Append(" ");
            arguments.Append(this.User);
            arguments.Append(" ");
            arguments.Append(this.Password);
            arguments.Append(" ");
            arguments.Append(this.Depot); // "//RapidMinds/SpeedGrid/SpeedGrid/..."


            TextWriter oldWriter = Console.Out;

            MemoryStream memoryStream = new MemoryStream();

            StreamWriter streamWriter = new StreamWriter(memoryStream);
            Console.SetOut(streamWriter);

            executeTask.Execute(this.Executable, arguments.ToString());
            Console.SetOut(oldWriter);
            streamWriter.Flush();
            streamWriter.Close();
            memoryStream.Flush();

            //StreamReader reader = new StreamReader(memoryStream);
            //while( true )
            //{
            //    string line = reader.ReadLine();
            //    if (line == null) break;

            //    if (line[0] == '@')
            //    {
            //        Console.WriteLine(line);
            //    }
            //}
            //reader.Close();
            //streamWriter.Close();
            //memoryStream.Close();

            byte[] byteContent = memoryStream.ToArray();
            memoryStream.Close();

            System.Text.Encoding enc = System.Text.Encoding.ASCII;
            string content = enc.GetString(byteContent);

            StringReader stringReader = new StringReader(content);


            TextWriter textWriter = new StreamWriter(this.OutputFile);

            while (true)
            {
                string line = stringReader.ReadLine();
                if (line == null) break;

                if (line.Length > 0 && line[0] == '@')
                {
                    string changelogLine = line.Substring(1);

                    string[] record = changelogLine.Split(';');

                    DateTime time = new DateTime( long.Parse( record[0]));
                    string change = record[1];
                    string user = record[2];
                    string desc = record[3];


                    

                    textWriter.WriteLine("{0} {1} {2}", time.ToString("d"), change, user);
                    textWriter.WriteLine("{0}"+Environment.NewLine, desc);

                    //textWriter.WriteLine(changelogLine);
                    Console.WriteLine(changelogLine);
                }
            }

            textWriter.Close();

            // Time Elapsed 00:00:00.72
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Error.WriteLine("Build succeeded.");
            Console.ResetColor();


        }


    }

}
