﻿
// Define MYMUSIC_MODEL to have the MyMusic data model
// hosted in hello.exe
#define MYMUSIC_MODEL

using Starcounter;
using Starcounter.Logging;
using Starcounter.Metadata;
using System;

#if MYMUSIC_MODEL
#region MyMusic code model
namespace MyMusic {
    public class Album : Entity {
        public String Name;
        public String Label;
        public Nullable<DateTime> Published;

        public Album(String name, String label, Nullable<DateTime> published) {
            Name = name;
            Label = label;
            Published = published;
        }
    }

    public class Artist : Entity {
        public String Name;
        public String Description;

        public Artist(String name, String description) {
            Name = name;
            Description = description;
        }
    }

    public class Mucho : Entity {
        public String Name;
        public Int64 Number;
    }

    public class RatedSong : Song {
        public Nullable<Int32> Rating;

        public RatedSong(String name, Artist artist, String composers, Nullable<DateTime> published, Int32 rating)
            : base(name, artist, composers, published) {
            Rating = rating;
        }
    }

    public class Song : Entity {
        public String Name;
        public String Composers;
        public Nullable<DateTime> Published;
        public Artist Artist;

        public Song(String name, Artist artist, String composers, Nullable<DateTime> published) {
            Name = name;
            Composers = composers;
            Published = published;
            Artist = artist;
        }
    }

    public class Track : Entity {
        public Song Song;
        public Album Album;
        public Nullable<Int32> Number;

        public Track(Song song, Album album, Nullable<int> number) {
            Song = song;
            Album = album;
            Number = number;
        }
    }
}
#endregion
#endif

namespace hello
{
    class HelloApp : AppNew {

        public HelloApp() {
            LogSource logSource = new LogSource("hello.app");
            logSource.LogNotice("Hello app-world (on database thread in database process)!");
        }

        static void Main(string[] args) {
            new HelloApp();
        }
    }

    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Hello world (on database thread in database process)!");
            
            LogSource logSource = new LogSource("hello");
            logSource.LogNotice("Hello world (on database thread in database process)!");

//            Starcounter.Internal.sccoredb.sccoredb_drop_index("MyMusic.Artist", "auto");

            foreach (SysTable t in Db.SQL("SELECT t FROM SysTable t")) {
                logSource.LogNotice(String.Format("SysTable: TableId={0}, Name={1}", t.TableId, t.Name));
            }
            foreach (SysColumn c in Db.SQL("SELECT c FROM SysColumn c")) {
                logSource.LogNotice(String.Format("SysColumn: TableId={0}, Index={1}, Name={2}", c.TableId, c.Index, c.Name));
            }
            foreach (SysIndex i in Db.SQL("SELECT i FROM SysIndex i")) {
                logSource.LogNotice(String.Format("SysIndex: TableId={0}, Name={1}", i.TableId, i.Name));
            }

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
