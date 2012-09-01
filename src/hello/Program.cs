
using Starcounter;
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
            try
            {
                new MyMusic.Mucho();
            }
            catch (Exception) { }

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
            });
#endif
        }

        private static void UpdateSchema()
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
                        new ColumnDef("Name", ColumnDef.TYPE_STRING, true),
                        new ColumnDef("Number", ColumnDef.TYPE_INT64, false)
                        }
                    );
                Db.CreateTable(t);

                Db.Transaction(() =>
                {
                    t = Db.LookupTable("MyMusic.Mucho");
                });

                Db.CreateIndex(t.DefinitionAddr, "Mucho1", 1); // Index on Mucho.Number
            }

            Bindings.BuildAndAddTypeBinding(t);

            Starcounter.Internal.Fix.ResetTheQueryModule();
        }
    }
}
