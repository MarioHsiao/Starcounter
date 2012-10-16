using System;
using Starcounter;

namespace StarcounterAdministrator
{
    public partial class Root : App
    {
        internal static Root CreateFakeRootPage()
        {
            Root root = new Root();

            try
            {
                root.CreateFakeUser();
                root.CreateFakeInfo();
                root.CreateFakeDb();
            }
            catch (Exception ex)
            {
                messageobjApp msg = new messageobjApp();
                msg.header = "Ooops!";
                msg.message = ex.Message;
                root.messageobj = msg;
            }

            // TODO:
            // Add html
            root.View = "ScAdmin.html";
            return root;
        }


        private void CreateFakeUser()
        {
            this.user = new userApp();
            this.user.name = "Apa";
            this.user.password = "Papa";
        }

        private void CreateFakeInfo()
        {
            this.info = new infoApp();
            this.info.name = "Personal";
            this.info.description = "This is the personal server";
        }

        internal void CreateFakeDb()
        {
            this.databases = new databasesApp();

            databasesApp.listApp db = this.databases.list.Add();
            db.name = "Fake_db_1";
            db.description = "Fake database 1";
            db.id = "666";
            db.image = "a";
            db.image_folder = "b";
            db.image_size = 666;
            db.temp_folder = "c";
            db.transactionslog_folder = "d";
            db.transactionslog_size = 999;

            this.CreateFakeApplication(db);
        }

        private void CreateFakeApplication(databasesApp.listApp db)
        {
            var app = db.applications_running.Add();
            app.description = "First fake application";
            app.id = "qwerty";
            app.image = "a";
            app.name = "First";
        }
    }
}
