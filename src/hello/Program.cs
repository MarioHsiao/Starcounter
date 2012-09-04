
using Starcounter;
using Starcounter.Binding;
using System;

namespace hello
{

    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Hello world (on database thread in database process)!");

            UpdateSchema();

#if false
            Db.Transaction(() =>
            {
                MyMusic.Mucho m;
                m = (MyMusic.Mucho)Db.SQL("SELECT m FROM MyMusic.Mucho m WHERE m.Number = ?", 7).First;
                if (m == null)
                {
                    m = new MyMusic.Mucho();
                    m.Name = "Nisse";
                    m.Number = 7;
                }
                m = null;

                MyMusic.Album a;
                a = (MyMusic.Album)Db.SQL("SELECT m FROM MyMusic.Album m WHERE m.Name = ?", "Nisse").First;
                if (a == null)
                {
                    a = new MyMusic.Album("Nisse", "Nisse", DateTime.Now);
                }
                a = null;
            });
#endif
        }

        private static void UpdateSchema()
        {
            UpdateSchema_Album();
            UpdateSchema_Mucho();

            Starcounter.Internal.Fix.ResetTheQueryModule();
        }

        private static void UpdateSchema_Album()
        {
            TableDef t = null;

            Db.Transaction(() =>
            {
                t = Db.LookupTable("MyMusic.Album");
            });

#if false
            if (t != null)
            {
                Db.DropTable(t.TableId);
                t = null;
            }
#endif

            if (t == null)
            {
                t = new TableDef(
                    "MyMusic.Album",
                    new ColumnDef[] {
                        new ColumnDef("Label", DbTypeCode.String, true),
                        new ColumnDef("Name", DbTypeCode.String, true),
                        new ColumnDef("Published", DbTypeCode.DateTime, true)
                        }
                    );
                Db.CreateTable(t);

                Db.Transaction(() =>
                {
                    t = Db.LookupTable("MyMusic.Album");
                });

                Db.CreateIndex(t.DefinitionAddr, "Album1", 1); // Index on Album.Name
            }

            TypeDef typeDef = new TypeDef(
                "MyMusic.Album",
                new PropertyDef[] {
                    new PropertyDef("Label", DbTypeCode.String, true, 0),
                    new PropertyDef("Name", DbTypeCode.String, true, 1),
                    new PropertyDef("Published", DbTypeCode.DateTime, true, 2)
                    },
                new TypeLoader(AppDomain.CurrentDomain.BaseDirectory + "MyMusic.dll", "MyMusic.Album"),
                t
                );
            Bindings.RegisterTypeDef(typeDef);
        }

        private static void UpdateSchema_Mucho()
        {
            TableDef t = null;

            Db.Transaction(() =>
            {
                t = Db.LookupTable("MyMusic.Mucho");
            });

#if false
            if (t != null)
            {
                Db.DropTable(t.TableId);
                t = null;
            }
#endif

            if (t == null)
            {
                t = new TableDef(
                    "MyMusic.Mucho",
                    new ColumnDef[] {
                        new ColumnDef("Name", DbTypeCode.String, true),
                        new ColumnDef("Number", DbTypeCode.Int64, false)
                        }
                    );
                Db.CreateTable(t);

                Db.Transaction(() =>
                {
                    t = Db.LookupTable("MyMusic.Mucho");
                });

                Db.CreateIndex(t.DefinitionAddr, "Mucho1", 1); // Index on Mucho.Number
            }

            TypeDef typeDef = new TypeDef(
                "MyMusic.Mucho",
                new PropertyDef[] {
                    new PropertyDef("Name", DbTypeCode.String, true, 0),
                    new PropertyDef("Number", DbTypeCode.Int64, false, 1)
                    },
                new TypeLoader(AppDomain.CurrentDomain.BaseDirectory + "MyMusic.dll", "MyMusic.Mucho"),
                t
                );
            Bindings.RegisterTypeDef(typeDef);
        }
    }
}
