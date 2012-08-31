
using Starcounter;
using System;

namespace hello
{
    class Program
    {
        static void Main(string[] args)
        {
//            MyMusic.Mucho m = new MyMusic.Mucho();

            Console.WriteLine("Hello world (on database thread in database process)!");

            TableDef t = null;

            Db.Transaction(() =>
            {
                t = Db.LookupTable("MyMusic.Mucho");
            });

            if (t == null)
            {
                t = new TableDef(
                    "MyMusic.Mucho",
                    0xFFFF,
                    new ColumnDef[] {
                        new ColumnDef("Name", ColumnDef.TYPE_STRING, true),
                        new ColumnDef("Number", ColumnDef.TYPE_INT64, false)
                        }
                    );
                Db.CreateTable(t);
            }
        }
    }
}
