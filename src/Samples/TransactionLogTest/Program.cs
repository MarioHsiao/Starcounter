using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Starcounter;
using System.Diagnostics;
using Starcounter.TransactionLog;


namespace TransactionLogTest
{
    [Database]
    public class TestClassBase
    {
        public string base_string;
    }

    [Database]
    public class TestClass : TestClassBase
    {
        public string str_field;
        public Binary bin_field;
        public long long_field;
        public ulong ulong_field;
        public decimal dec_field;
        public float float_field;
        public double double_field;
        public TestClass ref_field;
        public string null_str_field;
        public long? null_long_field;
    };

    [Database]
    public class TestClassBase2 : TestClassBase
    {
    }

    [Database]
    public class TestClassBase3 : TestClassBase2
    {
    }

    [Database]
    public class TestClassBase4 : TestClassBase
    {
    }

    [Database]
    public class IntTest
    {
        public long val;
    }


    class Program
    {
        static void check_create_entry()
        {
            // ARRANGE
            ILogManager log_manager = new LogManager();

            using (ILogReader log_reader = log_manager.OpenLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir))
            {
                var cts = new CancellationTokenSource();

                RewindLog(log_reader, cts);
                ulong t_record_key = 0;

                Db.Transact(() =>
                {
                    var t = new TestClass
                    {
                        base_string = "Str0",
                        bin_field = new Binary(new byte[1] { 42 }),
                        dec_field = 42.24m,
                        double_field = -42.42,
                        float_field = 42.42f,
                        long_field = -42,
                        str_field = "Str",
                        ulong_field = ulong.MaxValue,
                        null_str_field = null
                    };
                    t.ref_field = t;
                    t_record_key = t.GetObjectNo();
                });

                // ACT
                LogReadResult lr = log_reader.ReadAsync(cts.Token).Result;

                //CHECK
                Trace.Assert(lr.TransactionData.Creates.Count() == 1);

                var create_entry = lr.TransactionData.Creates.First();
                Trace.Assert(create_entry.Table == typeof(TestClass).FullName);
                Trace.Assert(create_entry.Key.ObjectID == t_record_key);

                Trace.Assert((string)(create_entry.Columns.Where(c => c.Name == "base_string").Single().Value) == "Str0");
                Trace.Assert((create_entry.Columns.Where(c => c.Name == "bin_field").Single().Value as byte[]).SequenceEqual(new byte[1] { 42 }));
                Trace.Assert((decimal)(create_entry.Columns.Where(c => c.Name == "dec_field").Single().Value) == 42.24m);
                Trace.Assert((double)(create_entry.Columns.Where(c => c.Name == "double_field").Single().Value) == -42.42);
                Trace.Assert((float)(create_entry.Columns.Where(c => c.Name == "float_field").Single().Value) == 42.42f);
                Trace.Assert((long)(create_entry.Columns.Where(c => c.Name == "long_field").Single().Value) == -42);
                Trace.Assert((string)(create_entry.Columns.Where(c => c.Name == "str_field").Single().Value) == "Str");
                Trace.Assert((ulong)(create_entry.Columns.Where(c => c.Name == "ulong_field").Single().Value) == ulong.MaxValue);
                Trace.Assert(((Reference)(create_entry.Columns.Where(c => c.Name == "ref_field").Single().Value)).ObjectID == t_record_key);
            }

        }

        static void check_create_entry_for_inherited_table<T> () where T : new()
        {
            // ARRANGE
            ILogManager log_manager = new LogManager();

            using (ILogReader log_reader = log_manager.OpenLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir))
            {
                var cts = new CancellationTokenSource();

                RewindLog(log_reader, cts);

                Db.Transact(() =>
                {
                    new T();
                });

                // ACT
                LogReadResult lr = log_reader.ReadAsync(cts.Token).Result;

                //CHECK
                Trace.Assert(lr.TransactionData.Creates.Count() == 1);
                Trace.Assert(lr.TransactionData.Creates.First().Table == typeof(T).FullName);
            }
        }

        static void check_create_entry_for_inherited_tables()
        {
            check_create_entry_for_inherited_table<TestClassBase>();
            check_create_entry_for_inherited_table<TestClassBase2>();
            check_create_entry_for_inherited_table<TestClassBase3>();
            check_create_entry_for_inherited_table<TestClassBase4>();
        }

        static void check_update_entry()
        {
            // ARRANGE
            ILogManager log_manager = new LogManager();

            using (ILogReader log_reader = log_manager.OpenLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir))
            {
                var cts = new CancellationTokenSource();

                RewindLog(log_reader, cts);

                TestClass t = null;
                Db.Transact(() =>
                {
                    t = new TestClass
                    {
                        null_str_field = "str",
                        null_long_field = 42
                    };
                });


                Db.Transact(() =>
                {
                    t.null_str_field = null;
                    t.null_long_field = null;
                });

                // ACT
                log_reader.ReadAsync(cts.Token).Wait(); //skip creating transaction
                var lr = log_reader.ReadAsync(cts.Token).Result; //deal with update transaction


                //CHECK
                var update_entry = lr.TransactionData.Updates.Single();
                Trace.Assert(update_entry.Table == typeof(TestClass).FullName);
                Trace.Assert(update_entry.Key.ObjectID == t.GetObjectNo());

                Trace.Assert(update_entry.Columns.Where(c => c.Name == "null_str_field").Single().Value == null);
                Trace.Assert(update_entry.Columns.Where(c => c.Name == "null_long_field").Single().Value == null);

            }

        }


        static void check_positioning()
        {
            // ARRANGE
            ulong t_record_key = 0;
            LogPosition new_record_position = new LogPosition();

            ILogManager log_manager = new LogManager();

            using (ILogReader log_reader = log_manager.OpenLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir))
            {
                var cts = new CancellationTokenSource();

                //rewind to the end of log
                LogReadResult lr;
                do
                {
                    lr = log_reader.ReadAsync(cts.Token, false).Result;
                    if ( lr != null)
                        new_record_position = lr.ContinuationPosition;
                }
                while (lr != null);
            }

            Db.Transact(() =>
            {
                var t = new TestClass();
                t_record_key = t.GetObjectNo();
            });


            //ACT
            using (ILogReader log_reader = log_manager.OpenLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir, new_record_position))
            {
                //CHECK
                var cts = new CancellationTokenSource();

                LogReadResult lr;
                lr = log_reader.ReadAsync(cts.Token).Result;

                Trace.Assert(lr.TransactionData.Creates.First().Key.ObjectID == t_record_key);
                Trace.Assert(lr.ContinuationPosition.CommitID > new_record_position.CommitID);
            }
        }

        static void check_filtering()
        {
            // ARRANGE
            ILogManager log_manager = new LogManager();

            using (ILogReader log_reader = log_manager.OpenLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir, t=>t!=typeof(TestClassBase).FullName))
            {
                using (ILogReader log_reader2 = log_manager.OpenLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir, t => t != typeof(TestClass).FullName))
                {
                    using (ILogReader log_reader3 = log_manager.OpenLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir, t => false))
                    {

                        var cts = new CancellationTokenSource();

                        RewindLog(log_reader, cts);
                        RewindLog(log_reader2, cts);
                        RewindLog(log_reader3, cts);


                        ulong key1 = 0;
                        ulong key2 = 0;

                        Db.Transact(() =>
                        {
                            key1 = new TestClass().GetObjectNo();
                            key2 = new TestClassBase().GetObjectNo();
                        });

                        // ACT
                        var lr = log_reader.ReadAsync(cts.Token).Result;
                        var lr2 = log_reader2.ReadAsync(cts.Token).Result;
                        var lr3 = log_reader3.ReadAsync(cts.Token).Result;

                        //CHECK
                        Trace.Assert(lr.TransactionData.Creates.Count() == 1);
                        Trace.Assert(lr.TransactionData.Creates.Single().Key.ObjectID == key1);

                        Trace.Assert(lr2.TransactionData.Creates.Count() == 1);
                        Trace.Assert(lr2.TransactionData.Creates.Single().Key.ObjectID == key2);

                        Trace.Assert(lr3.TransactionData.Creates.Count() == 0);
                    }
                }
            }
        }

        private static void RewindLog(ILogReader log_reader, CancellationTokenSource cts)
        {
            while ( log_reader.ReadAsync(cts.Token, false).Result != null )
                ;
        }

        static void check_apply_create()
        {
            //arrange

            ulong last_key = 0;
            Db.Transact(() =>
            {
                var t = new TestClass();
                last_key = t.GetObjectNo();
            });

            ulong new_record_key = last_key + 1;

            TransactionData td = new TransactionData {
                                    Updates = new List<UpdateRecordEntry>(),
                                    Deletes = new List<DeleteRecordEntry>(),
                                    Creates = new List<CreateRecordEntry> {
                                        new CreateRecordEntry {
                                            Table = typeof(TestClass).FullName,
                                            Key = new Reference { ObjectID=new_record_key },
                                            Columns = new ColumnUpdate[]{
                                                new ColumnUpdate { Name="base_string", Value="Str" },
                                                new ColumnUpdate { Name="bin_field", Value=new byte[1] { 42 } },
                                                new ColumnUpdate { Name="dec_field", Value=42.24m },
                                                new ColumnUpdate { Name="double_field", Value=-42.42 },
                                                new ColumnUpdate { Name="float_field", Value=42.42f },
                                                new ColumnUpdate { Name="long_field", Value=-42L },
                                                new ColumnUpdate { Name="str_field", Value=null },
                                                new ColumnUpdate { Name="ulong_field", Value=ulong.MaxValue },
                                                new ColumnUpdate { Name="ref_field", Value=new Reference { ObjectID = last_key } }
                                            } } } };

            //act

            Db.Transact(() =>
            {
                new LogApplicator().Apply(td);
            });

            //check
            Db.Transact(() =>
            {
                var t = Db.SQL<TestClass>("select t from TestClass t").Where(x=>x.GetObjectNo()==new_record_key).Single();
                Trace.Assert(t.base_string == "Str");
                Trace.Assert(t.bin_field.ToArray().SequenceEqual(new byte[1] { 42 }));
                Trace.Assert(t.dec_field == 42.24m);
                Trace.Assert(t.double_field == -42.42);
                Trace.Assert(t.float_field == 42.42f);
                Trace.Assert(t.long_field == -42L);
                Trace.Assert(t.str_field == null);
                Trace.Assert(t.ulong_field == ulong.MaxValue);
                Trace.Assert(t.ref_field.GetObjectNo() == last_key);
            });
        }

        static void check_apply_create_in_nonexistent_table()
        {
            //arrange

            TransactionData td = new TransactionData
            {
                Creates = new List<CreateRecordEntry>(){
                                    new CreateRecordEntry {
                                        Table = "NoSuchTable.768C17AE_65F1_4E6B_97BD_B6A98E427848",
                                        Key = new Reference {ObjectID=1},
                                        Columns = new ColumnUpdate[] { }
                                    } },
                Deletes = new List<DeleteRecordEntry>(),
                Updates = new List<UpdateRecordEntry>()
            };

            //act

            try
            {
                Db.Transact(() =>
                {
                    new LogApplicator().Apply(td);
                    Trace.Assert(false, "Shouldn't be here");
                });
            }
            catch (Starcounter.DbException e)
            {
                Trace.Assert(e.ErrorCode == Error.SCERRTABLENOTFOUND);
            }

        }

        static void check_apply_create_of_nonexistent_column()
        {
            //arrange
            ulong last_key = 0;
            Db.Transact(() =>
            {
                var t = new TestClass();
                last_key = t.GetObjectNo();
            });

            ulong new_record_key = last_key + 1;

            TransactionData td = new TransactionData
            {
                Creates = new List<CreateRecordEntry>(){
                                    new CreateRecordEntry {
                                        Table = typeof(TestClass).FullName,
                                        Key = new Reference {ObjectID=new_record_key},
                                        Columns = new ColumnUpdate[] {
                                            new ColumnUpdate { Name="NoSuchColumn768C17AE_65F1_4E6B_97BD_B6A98E427848", Value="Str" }
                                        }
                                    } },
                Deletes = new List<DeleteRecordEntry>(),
                Updates = new List<UpdateRecordEntry>()
            };

            //act

            try
            {
                Db.Transact(() =>
                {
                    new LogApplicator().Apply(td);
                    Trace.Assert(false, "Shouldn't be here");
                });
            }
            catch (Starcounter.DbException e)
            {
                Trace.Assert(e.ErrorCode == Error.SCERRCOLUMNNOTFOUND);
            }
        }

        static void check_apply_update()
        {
            //arrange

            ulong key = 0;
            Db.Transact(() =>
            {
                var t = new TestClass();
                key = t.GetObjectNo();
            });

            TransactionData td = new TransactionData
            {
                Creates = new List<CreateRecordEntry>(),
                Deletes = new List<DeleteRecordEntry>(),
                Updates = new List<UpdateRecordEntry> {
                                        new UpdateRecordEntry {
                                            Table = typeof(TestClass).FullName,
                                            Key = new Reference { ObjectID=key },
                                            Columns = new ColumnUpdate[]{
                                                new ColumnUpdate { Name="base_string", Value="Str" }
                                            } } }
            };

            //act

            Db.Transact(() =>
            {
                new LogApplicator().Apply(td);
            });

            //check
            Db.Transact(() =>
            {
                var t = Db.SQL<TestClass>("select t from TestClass t").Where(x => x.GetObjectNo() == key).Single();
                Trace.Assert(t.base_string == "Str");
            });
        }

        static void check_apply_update_to_nonexistent_record()
        {
            //arrange

            ulong key = 0;
            Db.Transact(() =>
            {
                var t = new TestClass();
                key = t.GetObjectNo();
            });

            TransactionData td = new TransactionData
            {
                Creates = new List<CreateRecordEntry>(),
                Deletes = new List<DeleteRecordEntry>(),
                Updates = new List<UpdateRecordEntry> {
                                        new UpdateRecordEntry {
                                            Table = typeof(TestClass).FullName,
                                            Key = new Reference { ObjectID=key+1 },
                                            Columns = new ColumnUpdate[]{
                                            } } }
            };

            //act

            try
            {
                Db.Transact(() =>
                {
                    new LogApplicator().Apply(td);
                    Trace.Assert(false, "Shouldn't be here");
                });
            }
            catch( Starcounter.DbException e )
            {
                Trace.Assert(e.ErrorCode == Error.SCERRRECORDNOTFOUND);
            }
        }

        static void check_apply_delete()
        {
            //arrange

            ulong key = 0;
            Db.Transact(() =>
            {
                var t = new TestClass();
                key = t.GetObjectNo();
                Trace.Assert(Db.SQL<TestClass>("select t from TestClass t").Where(x => x.GetObjectNo() == key).Any());
            });

            TransactionData td = new TransactionData
            {
                Creates = new List<CreateRecordEntry>(),
                Updates = new List<UpdateRecordEntry>(),
                Deletes = new List<DeleteRecordEntry>(){
                                    new DeleteRecordEntry {
                                              Table = typeof(TestClass).FullName,
                                              Key = new Reference { ObjectID=key }
                                    } }
            };

            //act

            Db.Transact(() =>
            {
                new LogApplicator().Apply(td);
            });

            //check
            Db.Transact(() =>
            {
                Trace.Assert( !Db.SQL<TestClass>("select t from TestClass t").Where(x => x.GetObjectNo() == key).Any() );
            });

        }

        static void check_apply_delete_of_nonexistent_record()
        {
            //arrange

            ulong key = 0;
            Db.Transact(() =>
            {
                var t = new TestClass();
                key = t.GetObjectNo();
            });

            TransactionData td = new TransactionData
            {
                Creates = new List<CreateRecordEntry>(),
                Updates = new List<UpdateRecordEntry>(),
                Deletes = new List<DeleteRecordEntry>(){
                                    new DeleteRecordEntry {
                                              Table = typeof(TestClass).FullName,
                                              Key = new Reference { ObjectID=key+1 }
                                    } }
            };

            //act

            try
            {
                Db.Transact(() =>
                {
                    new LogApplicator().Apply(td);
                    Trace.Assert(false, "Shouldn't be here");
                });
            }
            catch (Starcounter.DbException e)
            {
                Trace.Assert(e.ErrorCode == Error.SCERRRECORDNOTFOUND);
            }
        }

        static public void CreateIndex(string index_name, string query)
        {
            bool index_exist = false;
            Db.Transact(() => index_exist = Db.SQL("SELECT i FROM Starcounter.Metadata.\"Index\" i WHERE Name = ?", index_name).Any());
            if (!index_exist)
                Db.SQL(query);
        }

        static void Main(string[] args)
        {
            CreateIndex("IntTestKey", "CREATE INDEX IntTestKey ON IntTest (val)");

            Db.Transact(() =>
            {
                if (!Db.SQL<IntTest>("SELECT i FROM IntTest i").Any())
                {
                    new IntTest { val = long.MinValue };
                    new IntTest { val = -1 };
                    System.Console.Out.WriteLine("insert new values");
                }
                else
                {
                    System.Console.Out.WriteLine("found values");
                }
            });

            Db.Transact(() =>{
                var r = Db.SQL<IntTest>("SELECT i FROM IntTest i ORDER BY val").ToArray();

                System.Console.Out.WriteLine(r[0].val);
                System.Console.Out.WriteLine(r[1].val);

                Trace.Assert(r[0].val < r[1].val);
            });

#if false

            check_create_entry();
            check_create_entry_for_inherited_tables();
            check_update_entry();
            check_positioning();
            check_filtering();
            check_apply_create();
            check_apply_create_in_nonexistent_table();
            check_apply_create_of_nonexistent_column();
            check_apply_update();
            check_apply_update_to_nonexistent_record();
            check_apply_delete();
            check_apply_delete_of_nonexistent_record();
#endif
            Environment.Exit(0);
        }
    }
}


