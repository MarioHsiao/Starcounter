using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Starcounter;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Starcounter.TransactionLog;
using System.Threading;

namespace rl_34_poc
{
    [Database]
    public class checklist_entry
    {
        public string item;
        public bool done;
        public checklist owner;

        public static void create_index()
        {

            if (!Db.Transact(() => Db.SQL("SELECT i FROM Starcounter.Metadata.\"Index\" i WHERE Name = ?", "checklist_entry_unique_constraint").Any()))
                Db.SQL("CREATE UNIQUE INDEX checklist_entry_unique_constraint ON checklist_entry (owner,item)");
        }

    }

    [Database]
    public class checklist
    {
        public string name;
        private bool _closed;
        public string checklist_state_on_first_close;
        public DateTime time_of_first_close;

        public bool closed
        {
            get { return _closed; }
            set {
                _closed = value;
                if ((checklist_state_on_first_close == null) && value)
                {
                    time_of_first_close = DateTime.Now;
                    checklist_state_on_first_close = new checklist_output { Data = this }.ToJson();
                }
            }
        }

        public checklist_entry get_entry(string item)
        {
            return Db.SQL<checklist_entry>("SELECT e FROM checklist_entry e WHERE e.item=? AND e.owner=?", item, this).Single();
        }

        public IEnumerable<checklist_entry> entries
        {
            get
            {
                return Db.SQL<checklist_entry>("SELECT e FROM checklist_entry e WHERE e.owner=?", this);
            }
        }

        public checklist_entry add_entry(string item)
        {
            return new checklist_entry { item = item, done = false, owner = this };
        }

        public static checklist find_checklist(string name)
        {
            return Db.SQL<checklist>("SELECT c FROM checklist c WHERE c.name=?", name).Single();
        }

        public static void create_index()
        {

            if ( !Db.Transact(() => Db.SQL("SELECT i FROM Starcounter.Metadata.\"Index\" i WHERE Name = ?", "checklist_unique_constraint").Any()))
                Db.SQL("CREATE UNIQUE INDEX checklist_unique_constraint ON checklist (name)");

        }
    }

    static class LogExtensions
    {
        public static IEnumerable<TransactionData> TransactionLog(string db_name, string log_dir)
        {
            var lm = new LogManager();
            using (var log_reader = lm.OpenLog(db_name, log_dir))
            {
                var cts = new CancellationTokenSource();

                //rewind to the end of log
                LogReadResult lr;
                do
                {
                    lr = log_reader.ReadAsync(cts.Token, false).Result;

                    if (lr != null)
                    {
                        yield return lr.transaction_data;
                    }
                }
                while (lr != null);
            }
        }

        public static IEnumerable<column_update[]> FilterByObjectId(this IEnumerable<TransactionData> log, ulong object_id)
        {
            foreach (var td in log)
            {
                var filtered_creates = td.creates.Where(c => c.key.object_id == object_id);
                if (filtered_creates.Any())
                    yield return filtered_creates.Single().columns;

                var filtered_updates = td.updates.Where(c => c.key.object_id == object_id);
                if (filtered_updates.Any())
                    yield return filtered_updates.Single().columns;
            }
        }

        public static IEnumerable<T> TakeWhileInclusive<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            foreach (T item in source)
            {
                if (predicate(item))
                {
                    yield return item;
                }
                else
                {
                    yield return item;

                    yield break;
                }
            }
        }

        public static Dictionary<string, object> UpdateObjectState(Dictionary<string, object> state, column_update[] update)
        {
            foreach (var u in update)
            {
                state[u.name] = u.value;
            }

            return state;
        }

    }


    public class Program
    {
        static string get_checklist_state_at_moment_of_first_closing(ulong checklist_object_id)
        {
            return (string)LogExtensions.TransactionLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir)
                                                .FilterByObjectId(checklist_object_id)
                                                .TakeWhileInclusive(u => !u.Where(cu => cu.name == "_closed" && (ulong)cu.value != 0).Any())
                                                .Aggregate(new Dictionary<string, object>(), (state, update) => LogExtensions.UpdateObjectState(state, update))["checklist_state_on_first_close"];
        }

        public static int Main(string[] args)
        {
            checklist.create_index();
            checklist_entry.create_index();

            Handle.POST("/newchecklist?name={?}", (string name) =>
            {
                return Db.Transact<checklist_output>(() => { 
                    return new checklist_output { Data = new checklist { name = name, closed = false } };
                });
            });

            Handle.POST("/newentry?checklist={?}&item={?}", (string name, string item) =>
            {
                return Db.Transact<checklist_output>(() => {
                    var c = checklist.find_checklist(name);
                    new checklist_entry() { item = item, done = false, owner = c };
                    return new checklist_output { Data = c };
                });
            });

            Handle.POST("/markentrydone?checklist={?}&item={?}", (string name, string item) =>
            {
                return Db.Transact<checklist_output>(() => {
                    var c = checklist.find_checklist(name);
                    c.get_entry(item).done = true;
                    return new checklist_output { Data = c };
                });
            });


            Handle.POST("/closechecklist?name={?}", (string name) =>
            {
                return Db.Transact<checklist_output>(() => {
                    var c = checklist.find_checklist(name);
                    c.closed = true;
                    return new checklist_output { Data = c };
                });
            });

            Handle.POST("/cheatchecklist?name={?}", (string name) =>
            {
                return Db.Transact<checklist_output>(() => {
                    var c = checklist.find_checklist(name);
                    c.checklist_state_on_first_close = "!!! cheated !!!";
                    c.time_of_first_close = DateTime.Now;
                    return new checklist_output { Data = c };
                });
            });

            Handle.GET("/checkliststateonfirstclose?name={?}", (string name) =>
            {
                return get_checklist_state_at_moment_of_first_closing(checklist.find_checklist(name).GetObjectNo());
            });



            return 0;
        }
    }
}
