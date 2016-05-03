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


    class Program
    {
        static void check_create_entry()
        {
            // ARRANGE
            ILogManager log_manager = new LogManager();

            using (ILogReader log_reader = log_manager.OpenLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir))
            {
                var cts = new CancellationTokenSource();

                //rewind to the end of log
                LogReadResult lr;
                do
                {
                    lr = log_reader.ReadAsync(cts.Token, false).Result;
                }
                while (lr != null);

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
                lr = log_reader.ReadAsync(cts.Token).Result;

                //CHECK
                Trace.Assert(lr.transaction_data.creates.Count() == 1);

                var create_entry = lr.transaction_data.creates.First();
                Trace.Assert(create_entry.table == typeof(TestClass).FullName);
                Trace.Assert(create_entry.key.object_id == t_record_key);

                Trace.Assert((string)(create_entry.columns.Where(c => c.name == "base_string").Single().value) == "Str0");
                Trace.Assert((create_entry.columns.Where(c => c.name == "bin_field").Single().value as byte[]).SequenceEqual(new byte[1] { 42 }));
                Trace.Assert((decimal)(create_entry.columns.Where(c => c.name == "dec_field").Single().value) == 42.24m);
                Trace.Assert((double)(create_entry.columns.Where(c => c.name == "double_field").Single().value) == -42.42);
                Trace.Assert((float)(create_entry.columns.Where(c => c.name == "float_field").Single().value) == 42.42f);
                Trace.Assert((long)(create_entry.columns.Where(c => c.name == "long_field").Single().value) == -42);
                Trace.Assert((string)(create_entry.columns.Where(c => c.name == "str_field").Single().value) == "Str");
                Trace.Assert((ulong)(create_entry.columns.Where(c => c.name == "ulong_field").Single().value) == ulong.MaxValue);
                Trace.Assert(((reference)(create_entry.columns.Where(c => c.name == "ref_field").Single().value)).object_id == t_record_key);
            }

        }

        static void check_create_entry_for_inherited_table<T> () where T : new()
        {
            // ARRANGE
            ILogManager log_manager = new LogManager();

            using (ILogReader log_reader = log_manager.OpenLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir))
            {
                var cts = new CancellationTokenSource();

                //rewind to the end of log
                LogReadResult lr;
                do
                {
                    lr = log_reader.ReadAsync(cts.Token, false).Result;
                }
                while (lr != null);

                Db.Transact(() =>
                {
                    new T();
                });

                // ACT
                lr = log_reader.ReadAsync(cts.Token).Result;

                //CHECK
                Trace.Assert(lr.transaction_data.creates.Count() == 1);
                Trace.Assert(lr.transaction_data.creates.First().table == typeof(T).FullName);
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

                //rewind to the end of log
                LogReadResult lr;
                do
                {
                    lr = log_reader.ReadAsync(cts.Token, false).Result;
                }
                while (lr != null);

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
                lr = log_reader.ReadAsync(cts.Token).Result; //deal with update transaction


                //CHECK
                var update_entry = lr.transaction_data.updates.Single();
                Trace.Assert(update_entry.table == typeof(TestClass).FullName);
                Trace.Assert(update_entry.key.object_id == t.GetObjectNo());

                Trace.Assert(update_entry.columns.Where(c => c.name == "null_str_field").Single().value == null);
                Trace.Assert(update_entry.columns.Where(c => c.name == "null_long_field").Single().value == null);

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
                        new_record_position = lr.continuation_position;
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

                Trace.Assert(lr.transaction_data.creates.First().key.object_id == t_record_key);
                Trace.Assert(lr.continuation_position.commit_id > new_record_position.commit_id);
            }
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
                                    updates = new List<update_record_entry>(),
                                    deletes = new List<delete_record_entry>(),
                                    creates = new List<create_record_entry> {
                                        new create_record_entry {
                                            table = typeof(TestClass).FullName,
                                            key = new reference { object_id=new_record_key },
                                            columns = new column_update[]{
                                                new column_update { name="base_string", value="Str" },
                                                new column_update { name="bin_field", value=new byte[1] { 42 } },
                                                new column_update { name="dec_field", value=42.24m },
                                                new column_update { name="double_field", value=-42.42 },
                                                new column_update { name="float_field", value=42.42f },
                                                new column_update { name="long_field", value=-42L },
                                                new column_update { name="str_field", value=null },
                                                new column_update { name="ulong_field", value=ulong.MaxValue },
                                                new column_update { name="ref_field", value=new reference { object_id = last_key } }
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
                creates = new List<create_record_entry>(){
                                    new create_record_entry {
                                        table = "NoSuchTable.768C17AE_65F1_4E6B_97BD_B6A98E427848",
                                        key = new reference {object_id=1},
                                        columns = new column_update[] { }
                                    } },
                deletes = new List<delete_record_entry>(),
                updates = new List<update_record_entry>()
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
                creates = new List<create_record_entry>(){
                                    new create_record_entry {
                                        table = typeof(TestClass).FullName,
                                        key = new reference {object_id=new_record_key},
                                        columns = new column_update[] {
                                            new column_update { name="NoSuchColumn768C17AE_65F1_4E6B_97BD_B6A98E427848", value="Str" }
                                        }
                                    } },
                deletes = new List<delete_record_entry>(),
                updates = new List<update_record_entry>()
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
                creates = new List<create_record_entry>(),
                deletes = new List<delete_record_entry>(),
                updates = new List<update_record_entry> {
                                        new update_record_entry {
                                            table = typeof(TestClass).FullName,
                                            key = new reference { object_id=key },
                                            columns = new column_update[]{
                                                new column_update { name="base_string", value="Str" }
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
                creates = new List<create_record_entry>(),
                deletes = new List<delete_record_entry>(),
                updates = new List<update_record_entry> {
                                        new update_record_entry {
                                            table = typeof(TestClass).FullName,
                                            key = new reference { object_id=key+1 },
                                            columns = new column_update[]{
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
                creates = new List<create_record_entry>(),
                updates = new List<update_record_entry>(),
                deletes = new List<delete_record_entry>(){
                                    new delete_record_entry {
                                              table = typeof(TestClass).FullName,
                                              key = new reference { object_id=key }
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
                creates = new List<create_record_entry>(),
                updates = new List<update_record_entry>(),
                deletes = new List<delete_record_entry>(){
                                    new delete_record_entry {
                                              table = typeof(TestClass).FullName,
                                              key = new reference { object_id=key+1 }
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

        static void Main(string[] args)
        {
            check_create_entry();
            check_create_entry_for_inherited_tables();
            check_update_entry();
            check_positioning();
            check_apply_create();
            check_apply_create_in_nonexistent_table();
            check_apply_create_of_nonexistent_column();
            check_apply_update();
            check_apply_update_to_nonexistent_record();
            check_apply_delete();
            check_apply_delete_of_nonexistent_record();

            Environment.Exit(0);
        }
    }
}

