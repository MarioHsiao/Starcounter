using System;
using System.IO;
using HttpStructs;
using Starcounter;
using Starcounter.Internal.Application;
using Starcounter.Internal.JsonPatch;
using Starcounter.Internal.Web;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;

namespace StarcounterAdministrator
{
    partial class ScAdmin : App
    {
        internal static IServerRuntime _runtime = null;
        internal static ServerEngine _engine;

        internal static ScAdmin CreateAdminPage()
        {
            ServerInfo info;
            ScAdmin admin = new ScAdmin();

            try
            {
                if (ScAdmin._runtime == null)
                {
                    ServerEngine engine = new ServerEngine(@".\.server\Personal\Personal.server.config");
                    engine.Setup();
                    ScAdmin._engine = engine;
                    ScAdmin._runtime = engine.Start();
                    admin.Status = "Started";
                }
                else
                {
                    admin.Status = "Running";
                }

                info = _runtime.GetServerInfo();

                admin.ServerName = info.Configuration.Name;
                admin.ServerUri = info.Uri;

                DatabaseInfo[] dbInfoArr = _runtime.GetDatabases();
                for (Int32 i = 0; i < dbInfoArr.Length; i++)
                {
                    admin.AddDatabaseInfo(dbInfoArr[i]);
                }

                CommandInfo[] cmdInfoArr = _runtime.GetCommands();
                for (Int32 i = 0; i < cmdInfoArr.Length; i++)
                {
                    admin.AddCommandInfo(cmdInfoArr[i]);
                }
            }
            catch (Exception ex)
            {
                admin.Status = ex.Message;
            }

            admin.View = "ScAdmin.html";
            admin.Header = "Starcounter Administrator";
            return admin;
        }

        private void Handle(Input.CreateDbClick click)
        {
            if (click.Value != "")
            {
                CommandInfo cmd = ExecuteCommand(new CreateDatabaseCommand(_engine, Value));
                AddCommandInfo(cmd);

                if (!cmd.HasError)
                {
                    DatabaseInfo db = _runtime.GetDatabase("sc://sc_chrhol/personal/" + Value);
                    if (db != null)
                    {
                        AddDatabaseInfo(db);
                    }
                }
            }
        }

        private void Handle(Input.StartDbClick click)
        {
            if (click.Value != "")
            {
                CommandInfo cmd = ExecuteCommand(new StartDatabaseCommand(_engine, Value));
                AddCommandInfo(cmd);
            }
        }

        private void Handle(Input.StopDbClick click)
        {
            if (click.Value != "")
            {
                CommandInfo cmd = ExecuteCommand(new StopDatabaseCommand(_engine, Value));
                AddCommandInfo(cmd);
            }
        }

        private void Handle(Input.ExecAppClick click)
        {
            if (click.Value != "")
            {
                String workingDir = @"d:\Code\Level1\src\Samples\MySampleApp\bin\debug\";//Path.GetDirectoryName(Value);
                String exePath = workingDir + Value;
                CommandInfo cmd = ExecuteCommand(new ExecAppCommand(_engine, exePath, workingDir, new String[0]));
                AddCommandInfo(cmd);
            }
        }

        private static CommandInfo ExecuteCommand(ServerCommand command)
        {
            CommandInfo ci = _runtime.Execute(command);
            _runtime.Wait(ci.Id);
            ci = _runtime.GetCommand(ci.Id);
            return ci;
        }

        private void AddCommandInfo(CommandInfo cmd)
        {
            var cmdApp = Commands.Add();
            cmdApp.Description = cmd.Description;
            cmdApp.Id = cmd.Id.Value;
            cmdApp.IsCompleted = cmd.IsCompleted.ToString();
            cmdApp.HasErrors = cmd.HasError.ToString();
        }

        private void AddDatabaseInfo(DatabaseInfo db)
        {
            var dbApp = Databases.Add();
            dbApp.Name = db.Name;
            dbApp.Uri = db.Uri;

            AppInfo[] appInfoArr = db.HostedApps;
            for (Int32 a = 0; a < appInfoArr.Length; a++)
            {
                var appApp = dbApp.Apps.Add();
                appApp.Executable = Path.GetFileName(appInfoArr[a].ExecutablePath);
            }
        }

        private static String GetCommandErrors(CommandInfo command)
        {
            String ret = "FAILURE:\n";
            for (Int32 i = 0; i < command.Errors.Length; i++)
            {
                ret += command.Errors[i].ErrorId + "\n";
                for (Int32 a = 0; a < command.Errors[i].Arguments.Length; a++)
                {
                    ret += "    " + command.Errors[i].Arguments[a] + "\n";
                }
            }
            return ret;
        }
    }
}
