using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Starcounter;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using tpcc.Transactions.json.converters;

namespace tpcc
{
    public class Program
    {
        public class V
        {
            public int i;
        }
        public static int tr_count;
        public static ConcurrentDictionary<string, V> retry_count = new ConcurrentDictionary<string, V>();

        private static TpccValuesGenerator gen = new TpccValuesGenerator();

        private static Starcounter.Response Populate()
        {
            DataLoader.PopulateAllTables(gen);
            return Stat();
        }

        private static Starcounter.Json Stat()
        {
            var d = new dbstat()
            {
                TransactionsCount = tr_count,
                Customers = (long)Db.SlowSQL("SELECT count(*) FROM Customer").First,
                Distcicts = (long)Db.SlowSQL("SELECT count(*) FROM District").First,
                Histories = (long)Db.SlowSQL("SELECT count(*) FROM History").First,
                Items = (long)Db.SlowSQL("SELECT count(*) FROM Item").First,
                NewOrders = (long)Db.SlowSQL("SELECT count(*) FROM NewOrder").First,
                Orders = (long)Db.SlowSQL("SELECT count(*) FROM \"Order\"").First,
                OrderLines = (long)Db.SlowSQL("SELECT count(*) FROM OrderLine").First,
                Stocks = (long)Db.SlowSQL("SELECT count(*) FROM Stock").First,
                Warehouses = (long)Db.SlowSQL("SELECT count(*) FROM Warehouse").First
            };

            foreach (var t in retry_count)
            {
                d.Collisions.Add(new dbstat.CollisionsElementJson { TransactionName = t.Key, CollisionsNumber = t.Value.i });
            }

            return d;
        }

        private static Starcounter.Json DoAll_NoIO(int load_factor)
        {
            var terminals = (from w_id in Enumerable.Range(1, DataLoader.W_ID_limit)
                             from d_id in Enumerable.Range(1, DataLoader.D_ID_limit)
                             select new TerminalEmulator(gen, w_id, d_id, load_factor))
                            .ToList();

            Stopwatch timer = new Stopwatch();
            timer.Start();
            int old_tr_count = tr_count;

            var running_terminals = terminals.Select((t, i) => t.Run((byte)(i % Starcounter.Internal.StarcounterEnvironment.SchedulerCount))).ToArray();

            Task.WaitAll(running_terminals);
            timer.Stop();

            var stat = new doall_stat();

            stat.ElapsedTime = timer.Elapsed.TotalSeconds;
            stat.TransactionsPerSecond = (tr_count - old_tr_count) / timer.Elapsed.TotalSeconds;

            return stat;
        }


        public static Tuple<int,int> Client(int load_factor, int schedulers_count)
        {
            var transactions = (from w_id in Enumerable.Range(1, DataLoader.W_ID_limit)
                                  from d_id in Enumerable.Range(1, DataLoader.D_ID_limit)
                                    select LoadEmulation.CreateDeck(gen, w_id, d_id, load_factor))
                                .Take(schedulers_count) //don't take more decks than we have available schedulers
                                .Select(t=>t.ToArray())
                                .ToArray();

            var scheduler_opts = Enumerable.Range(1, schedulers_count).Select(s => new Dictionary<String, String> { { "SchedulerId", s.ToString() } }).ToArray();

            AggregationClient aggrClient = new AggregationClient("127.0.0.1", 8080, 9191, 100000);
            int total = transactions.Length*transactions[0].Length;
            int successfullResps_ = 0;
            int failedResps_ = 0;
            var tcs = new TaskCompletionSource<int>();

            //round robin among schedulers: send 1-st transaction from each deck on that deck's scheduler, then move to 2-nd and so on...
            for (int i = 0; i < transactions[0].Length; ++i)
            {
                for (int j = 0; j < transactions.Length; ++j)
                {
                    var t = transactions[j][i];
                    aggrClient.Send("POST", t.uri, t.body, t.serialization_required ? scheduler_opts[j] : null, (r) =>
                    {
                        if (r.IsSuccessStatusCode)
                            successfullResps_++;
                        else
                            failedResps_++;

                        if (successfullResps_ + failedResps_ == total)
                            tcs.SetResult(0);
                    }
                    );
                }
            }

            while (!tcs.Task.Wait(1000))
                aggrClient.SendStatistics("tpcc", successfullResps_, failedResps_);

            aggrClient.SendStatistics("tpcc", successfullResps_, failedResps_);

            aggrClient.Shutdown();

            return new Tuple<int, int>(successfullResps_, failedResps_);
        }

        public static void Server()
        {
            Customer.CreateIndex();
            District.CreateIndex();
            Item.CreateIndex();
            NewOrder.CreateIndex();
            Order.CreateIndex();
            OrderLine.CreateIndex();
            Stock.CreateIndex();
            Warehouse.CreateIndex();


            Handle.GET("/populate", () =>
            {
                return Populate();
            });

            Handle.GET("/stat", () =>
            {
                return Stat();
            });

            Handle.PUT("/resetstat", () =>
            {
                tr_count = 0;
                retry_count.Clear();
                return Stat();
            }
            );

            Handle.GET("/gen/delivery?W_ID={?}&D_ID={?}", (int w_id, int d_id) =>
            {
                return LoadEmulation.CreateDeliveryTransaction(gen, w_id, d_id).body;
            });

            Handle.POST("/do/delivery", (Request r) =>
            {
                var d = new Delivery_Input();
                d.PopulateFromJson(r.Body);

                var s = Starcounter.Internal.StarcounterEnvironment.CurrentSchedulerId;

                tpcc.Transactions.DeliveryTransaction.Execute(d.ToCLR()).ContinueWith(
                    (o)=>Starcounter.Scheduling.ScheduleTask(
                    () =>
                    r.SendResponse(new Response { BodyBytes = new Delivery_Output { Data = o.Result }.ToJsonUtf8() }, null), false, s)
                );

                return HandlerStatus.Handled;
            });

            Handle.GET("/gen/neworder?W_ID={?}", (int w_id) =>
            {
                return LoadEmulation.CreateNewOrderTransaction(gen, w_id).body;
            });

            Handle.POST("/do/neworder", (Request r) =>
            {
                var d = new NewOrder_Input();
                d.PopulateFromJson(r.Body);

                var s = Starcounter.Internal.StarcounterEnvironment.CurrentSchedulerId;

                tpcc.Transactions.NewOrderTransaction.Execute(d.ToCLR()).ContinueWith(
                    (o) => Starcounter.Scheduling.ScheduleTask(
                    () =>
                    r.SendResponse(new Response { BodyBytes = new NewOrder_Output { Data = o.Result }.ToJsonUtf8() }, null), false, s)
                );

                return HandlerStatus.Handled;
            });

            Handle.GET("/gen/orderstatus?W_ID={?}", (int w_id) =>
            {
                return LoadEmulation.CreateOrderStatusTransaction(gen, w_id).body;
            });

            Handle.POST("/do/orderstatus", (OrderStatus_Input d) =>
            {
                return new OrderStatus_Output() { Data = tpcc.Transactions.OrderStatusTransaction.Execute(d.ToCLR()).Result };
            });

            Handle.GET("/gen/payment?W_ID={?}", (int w_id) =>
            {
                return LoadEmulation.CreatePaymentTransaction(gen, w_id).body;
            });

            Handle.POST("/do/payment", (Request r) =>
            {
                var d = new Payment_Input();
                d.PopulateFromJson(r.Body);

                var s = Starcounter.Internal.StarcounterEnvironment.CurrentSchedulerId;

                tpcc.Transactions.PaymentTransaction.Execute(d.ToCLR()).ContinueWith(
                        (o) => Starcounter.Scheduling.ScheduleTask(
                       ()=>
                       r.SendResponse(new Response { BodyBytes = new Payment_Output { Data = o.Result }.ToJsonUtf8() }, null), false, s)
                );

                return HandlerStatus.Handled;
            });

            Handle.GET("/gen/stocklevel?W_ID={?}&D_ID={?}", (int w_id, int d_id) =>
            {
                return LoadEmulation.CreateStockLevelTransaction(gen, w_id, d_id).body;
            });

            Handle.POST("/do/stocklevel", (StockLevel_Input d) =>
            {
                return new StockLevel_Output() { Data = tpcc.Transactions.StockLevelTransaction.Execute(d.ToCLR()).Result };
            });

            Handle.POST("/all_no_io?load_factor={?}", (int load_factor) =>
            {
                return DoAll_NoIO(load_factor);
            });
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && string.Equals(args[0], "-client", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 2)
                {
                    System.Console.Error.WriteLine("too few args for client mode");
                    return 1;
                }

                int load_factor = int.Parse(args[1]);

                int schedulers_count = (args.Length > 2) ? int.Parse(args[2]) : Environment.ProcessorCount;

                Tuple<int,int> r = Client(load_factor, schedulers_count);
                System.Console.WriteLine("success: {0}. fail: {1}.", r.Item1, r.Item2);
            }
            else
                Server();

            return 0;
        }
    }
}
