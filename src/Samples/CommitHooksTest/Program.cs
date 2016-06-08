using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Starcounter;
using System.Diagnostics;


namespace CommitHooksTest
{
    [Database]
    public class TestClass
    {
        public string str;
    }


    class Program
    {
        static class ScopedTransaction
        {
            public static void check_commit_insert_hook()
            {
                //prepare
                int inserts = 0;
                Hook<TestClass>.CommitInsert += (s, t) => { ++inserts; };

                //act
                Db.Transact(() =>
                {
                    var t = new TestClass
                    {
                    };

                    Trace.Assert(inserts == 0);
                });

                //check
                Trace.Assert(inserts == 1);
            }

            public static void check_commit_update_hook()
            {
                //prepare
                int updates = 0;
                Hook<TestClass>.CommitUpdate += (s, t) => { ++updates; };

                Db.Transact(() =>
                {
                    var t = new TestClass
                    {
                        str = "initial value"
                    };

                });


                //act
                Db.Transact(() =>
                {
                    Db.SQL<TestClass>("SELECT t FROM TestClass t").First().str = "new value";
                    Trace.Assert(updates == 0);
                });


                //check
                Trace.Assert(updates == 1);
            }

            public static void check_before_delete_hook()
            {
                //prepare
                int deletes = 0;
                Hook<TestClass>.BeforeDelete += (s, t) => { ++deletes; };

                Db.Transact(() =>
                {
                    var t = new TestClass
                    {
                    };
                });

                Db.Transact(() =>
                {
                //act
                Db.SQL<TestClass>("SELECT t FROM TestClass t").First().Delete();

                //check
                Trace.Assert(deletes == 1);
                });
            }

            public static void check_commit_delete_hook()
            {
                //prepare
                int deletes = 0;
                Hook<TestClass>.CommitDelete += (s, t) => { ++deletes; };

                Db.Transact(() =>
                {
                    var t = new TestClass
                    {
                    };
                });

                //act
                Db.Transact(() =>
                {
                    Db.SQL<TestClass>("SELECT t FROM TestClass t").First().Delete();
                    Trace.Assert(deletes == 0);
                });

                //check
                Trace.Assert(deletes == 1);
            }

            public static void check_commit_insert_hook_ignored()
            {
                //prepare
                int inserts = 0;
                Hook<TestClass>.CommitInsert += (s, t) => { ++inserts; };

                //act
                Db.Advanced.Transact(
                    new Db.Advanced.TransactOptions { applyHooks = false },
                    () =>
                    {
                        var t = new TestClass
                        {
                        };

                        Trace.Assert(inserts == 0);
                    }
                );

                //check
                Trace.Assert(inserts == 0);
            }

            public static void check_commit_update_hook_ignored()
            {
                //prepare
                int updates = 0;
                Hook<TestClass>.CommitUpdate += (s, t) => { ++updates; };

                Db.Transact(() =>
                {
                    var t = new TestClass
                    {
                        str = "initial value"
                    };

                });


                //act
                Db.Advanced.Transact(
                    new Db.Advanced.TransactOptions { applyHooks = false },
                    () =>
                    {
                        Db.SQL<TestClass>("SELECT t FROM TestClass t").First().str = "new value";
                        Trace.Assert(updates == 0);
                    }
                );


                //check
                Trace.Assert(updates == 0);
            }

            public static void check_before_delete_hook_ignored()
            {
                //prepare
                int deletes = 0;
                Hook<TestClass>.BeforeDelete += (s, t) => { ++deletes; };

                Db.Transact(() =>
                {
                    var t = new TestClass
                    {
                    };
                });

                Db.Advanced.Transact(
                    new Db.Advanced.TransactOptions { applyHooks = false },
                    () =>
                    {
                    //act
                    Db.SQL<TestClass>("SELECT t FROM TestClass t").First().Delete();

                    //check
                    Trace.Assert(deletes == 0);
                    }
                );
            }

            public static void check_commit_delete_hook_ignored()
            {
                //prepare
                int deletes = 0;
                Hook<TestClass>.CommitDelete += (s, t) => { ++deletes; };

                Db.Transact(() =>
                {
                    var t = new TestClass
                    {
                    };
                });

                //act
                Db.Advanced.Transact(
                    new Db.Advanced.TransactOptions { applyHooks = false },
                    () =>
                    {
                        Db.SQL<TestClass>("SELECT t FROM TestClass t").First().Delete();
                        Trace.Assert(deletes == 0);
                    }
                );

                //check
                Trace.Assert(deletes == 0);
            }
        }


        static class ExplicitTransaction
        {
            public static void check_commit_insert_hook()
            {
                //prepare
                int inserts = 0;
                Hook<TestClass>.CommitInsert += (s, t) => { ++inserts; };

                //act
                using (var tran = new Transaction())
                {
                    tran.Scope(() =>
                    {
                        var t = new TestClass
                        {
                        };

                        Trace.Assert(inserts == 0);

                        tran.Commit();
                    });
                }

                //check
                Trace.Assert(inserts == 1);
            }

            public static void check_commit_update_hook()
            {
                //prepare
                int updates = 0;
                Hook<TestClass>.CommitUpdate += (s, t) => { ++updates; };

                Db.Transact(() =>
                {
                    var t = new TestClass
                    {
                        str = "initial value"
                    };

                });


                //act
                using (var tran = new Transaction())
                {
                    tran.Scope(() =>
                    {
                        Db.SQL<TestClass>("SELECT t FROM TestClass t").First().str = "new value";
                        Trace.Assert(updates == 0);
                        tran.Commit();
                    });
                }


                //check
                Trace.Assert(updates == 1);
            }

            public static void check_before_delete_hook()
            {
                //prepare
                int deletes = 0;
                Hook<TestClass>.BeforeDelete += (s, t) => { ++deletes; };

                Db.Transact(() =>
                {
                    var t = new TestClass
                    {
                    };
                });

                using (var tran = new Transaction())
                {
                    tran.Scope(() =>
                    {
                        //act
                        Db.SQL<TestClass>("SELECT t FROM TestClass t").First().Delete();

                        //check
                        Trace.Assert(deletes == 1);

                        tran.Commit();
                    });
                };
            }

            public static void check_commit_delete_hook()
            {
                //prepare
                int deletes = 0;
                Hook<TestClass>.CommitDelete += (s, t) => { ++deletes; };

                Db.Transact(() =>
                {
                    var t = new TestClass
                    {
                    };
                });

                //act
                using (var tran = new Transaction())
                {
                    tran.Scope(() =>
                    {
                        Db.SQL<TestClass>("SELECT t FROM TestClass t").First().Delete();
                        Trace.Assert(deletes == 0);

                        tran.Commit();
                    });
                }

                //check
                Trace.Assert(deletes == 1);
            }

            public static void check_commit_insert_hook_ignored()
            {
                //prepare
                int inserts = 0;
                Hook<TestClass>.CommitInsert += (s, t) => { ++inserts; };

                //act
                using (var tran = new Transaction(false, false))
                {
                    tran.Scope(() =>
                    {
                        var t = new TestClass
                        {
                        };

                        Trace.Assert(inserts == 0);

                        tran.Commit();
                    });
                }

                //check
                Trace.Assert(inserts == 0);
            }

            public static void check_commit_update_hook_ignored()
            {
                //prepare
                int updates = 0;
                Hook<TestClass>.CommitUpdate += (s, t) => { ++updates; };

                Db.Transact(() =>
                {
                    var t = new TestClass
                    {
                        str = "initial value"
                    };

                });


                //act
                using (var tran = new Transaction(false, false))
                {
                    tran.Scope(() =>
                    {
                        Db.SQL<TestClass>("SELECT t FROM TestClass t").First().str = "new value";
                        Trace.Assert(updates == 0);

                        tran.Commit();
                    });
                }


                //check
                Trace.Assert(updates == 0);
            }

            public static void check_before_delete_hook_ignored()
            {
                //prepare
                int deletes = 0;
                Hook<TestClass>.BeforeDelete += (s, t) => { ++deletes; };

                Db.Transact(() =>
                {
                    var t = new TestClass
                    {
                    };
                });

                using (var tran = new Transaction(false, false))
                {
                    tran.Scope(() =>
                    {
                        //act
                        Db.SQL<TestClass>("SELECT t FROM TestClass t").First().Delete();

                        //check
                        Trace.Assert(deletes == 0);

                        tran.Commit();
                    });
                }
            }

            public static void check_commit_delete_hook_ignored()
            {
                //prepare
                int deletes = 0;
                Hook<TestClass>.CommitDelete += (s, t) => { ++deletes; };

                Db.Transact(() =>
                {
                    var t = new TestClass
                    {
                    };
                });

                //act
                using (var tran = new Transaction(false, false))
                {
                    tran.Scope(() =>
                    {
                        Db.SQL<TestClass>("SELECT t FROM TestClass t").First().Delete();
                        Trace.Assert(deletes == 0);

                        tran.Commit();
                    });
                }

                //check
                Trace.Assert(deletes == 0);
            }
        }

        static void Main(string[] args)
        {
            ScopedTransaction.check_commit_insert_hook();
            ScopedTransaction.check_commit_update_hook();
            ScopedTransaction.check_before_delete_hook();
            ScopedTransaction.check_commit_delete_hook();

            ScopedTransaction.check_commit_insert_hook_ignored();
            ScopedTransaction.check_commit_update_hook_ignored();
            ScopedTransaction.check_before_delete_hook_ignored();
            ScopedTransaction.check_commit_delete_hook_ignored();

            ExplicitTransaction.check_commit_insert_hook();
            ExplicitTransaction.check_commit_update_hook();
            ExplicitTransaction.check_before_delete_hook();
            ExplicitTransaction.check_commit_delete_hook();

            ExplicitTransaction.check_commit_insert_hook_ignored();
            ExplicitTransaction.check_commit_update_hook_ignored();
            ExplicitTransaction.check_before_delete_hook_ignored();
            ExplicitTransaction.check_commit_delete_hook_ignored();


            Environment.Exit(0);
        }
    }
}

