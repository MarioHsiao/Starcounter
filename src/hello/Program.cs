
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
            Db.Transaction(() =>
            {
                MyMusic.Mucho m = new MyMusic.Mucho();
                m.Name = "Nisse";
                m.Number = 7;
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
            }

            Db.Transaction(() =>
            {
                t = Db.LookupTable("MyMusic.Mucho");
            });

            BindingRegistry.BuildAndAddTypeBinding(t);
        }
    }
}
