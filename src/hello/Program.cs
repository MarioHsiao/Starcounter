
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
                    new PropertyDef("Name", DbTypeCode.String, true),
                    new PropertyDef("Number", DbTypeCode.Int64, false)
                    },
                new TypeLoader(AppDomain.CurrentDomain.BaseDirectory + "MyMusic.dll", "MyMusic.Mucho"),
                t
                );
            Bindings.RegisterTypeDef(typeDef);

            Starcounter.Internal.Fix.ResetTheQueryModule();
        }
    }
}
