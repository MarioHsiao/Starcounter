
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
#if false
            Db.Transaction(() =>
            {
                Object o;
                o = Db.SQL("SELECT m FROM MyMusic.Mucho m WHERE m.Number = ? AND m.Name = ?", 7, "Nisse").First;
                o = null;
            });
#endif

            Db.Transaction(() =>
            {
                MyMusic.Mucho m;
                m = (MyMusic.Mucho)Db.SQL("SELECT m FROM MyMusic.Mucho m WHERE m.Number = ? AND m.Name = ?", 7, "Nisse").First;
                if (m == null)
                {
                    m = new MyMusic.Mucho();
                    m.Name = "Nisse";
                    m.Number = 7;
                }
                m = null;

                MyMusic.Album album;
                album = (MyMusic.Album)Db.SQL("SELECT a FROM MyMusic.Album a WHERE a.Name = ? AND a.Label = ?", "Nisse", "Nisse").First;
                if (album == null)
                {
                    album = new MyMusic.Album("Nisse", "Nisse", DateTime.Now);
                }
                album = null;

                MyMusic.Song song;
                song = (MyMusic.Song)Db.SQL("SELECT a FROM MyMusic.Song a WHERE a.Name = ?", "Nisse").First;
                if (song == null)
                {
                    song = new MyMusic.Song("Nisse", null, null, DateTime.Now);
                }
                song = null;

                MyMusic.RatedSong ratedSong;
                ratedSong = (MyMusic.RatedSong)Db.SQL("SELECT a FROM MyMusic.RatedSong a WHERE a.Name = ?", "Nisse2").First;
                if (ratedSong == null)
                {
                    ratedSong = new MyMusic.RatedSong("Nisse2", null, null, DateTime.Now, 4);
                }
                ratedSong = null;
            });
#endif
        }
    }
}
