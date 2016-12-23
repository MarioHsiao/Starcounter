using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Starcounter;
using System.Diagnostics;


namespace DbTransactTest
{
    [Database]
    public class TestBase
    {
        public int i;
    }

    class Program
    {
        static void Main(string[] args)
        {

            TestBase x = null;

            //ARRANGE
            //initialize x=1
            Db.Transact(
                () =>{x = new TestBase { i = 1 };}
            );

            TransactionToken tt = null;

            try
            {

                //this transaction to be cloned. check x.i is still 1 and take transaction token
                Db.Transact(
                    () =>
                    {
                        System.Diagnostics.Debug.Assert(x.i == 1);
                        tt = Db.CurrentTransactionToken;
                    }
                );

                //change x.i to 2 and check it
                Db.Transact(() => { x.i = 2; });
                Db.Transact(() => { System.Diagnostics.Debug.Assert(x.i == 2); });


                //ACT and CHECK. clone transaction that saw x.i=1 and check it
                Db.Transact(() =>
                {
                    System.Diagnostics.Debug.Assert(x.i == 1);
                }, 0, new Db.Advanced.TransactOptions { source_token = tt });
            }
            finally
            {
                if (tt != null)
                    tt.Dispose();



                //check no way to duplicate transaction from Disposed token

                try
                {
                    Db.Transact(() =>
                    {
                    }, 0, new Db.Advanced.TransactOptions { source_token = tt });

                    //shouldn't be here
                    System.Diagnostics.Debug.Assert(false);
                }
                catch (System.ObjectDisposedException) { }

            }


            Environment.Exit(0);
        }
    }
}


