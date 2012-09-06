
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

#if false
            Db.Transaction(() =>
            {
                MyMusic.Mucho m;
//                m = (MyMusic.Mucho)Db.SQL("SELECT m FROM MyMusic.Mucho m WHERE m.Number = ?", 7).First;
                m = (MyMusic.Mucho)Db.SQL("SELECT m FROM MyMusic.Mucho m WHERE m.Number = ? AND m.Name = ?", 7, "Nisse").First;
                if (m == null)
                {
                    m = new MyMusic.Mucho();
                    m.Name = "Nisse";
                    m.Number = 7;
                }
                m = null;

                MyMusic.Album a;
//                a = (MyMusic.Album)Db.SQL("SELECT a FROM MyMusic.Album a WHERE a.Name = ?", "Nisse").First;
                a = (MyMusic.Album)Db.SQL("SELECT a FROM MyMusic.Album a WHERE a.Name = ? AND a.Label = ?", "Nisse", "Nisse").First;
                if (a == null)
                {
                    a = new MyMusic.Album("Nisse", "Nisse", DateTime.Now);
                }
                a = null;
            });
#endif
        }
    }
}
