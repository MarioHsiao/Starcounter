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

      var running_terminals = terminals.Select((t,i) => t.Run((byte)(i%Starcounter.Internal.StarcounterEnvironment.SchedulerCount))).ToArray();

      Task.WaitAll(running_terminals);
      timer.Stop();

      var stat = new doall_stat();

      stat.ElapsedTime = timer.Elapsed.TotalSeconds;
      stat.TransactionsPerSecond = (tr_count - old_tr_count) / timer.Elapsed.TotalSeconds;

      return stat;
    }

    public static void Main(string[] args)
    {
      System.Diagnostics.Debugger.Launch();

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
        return new Delivery_Input() { Data = tpcc.Transactions.DeliveryTransaction.InputData.Generate(gen, w_id, d_id) };
      });

      Handle.POST("/do/delivery", (Delivery_Input d) =>
      {
        return new Delivery_Output() { Data = tpcc.Transactions.DeliveryTransaction.Execute(d.ToCLR()) };
      });

      Handle.GET("/gen/neworder?W_ID={?}", (int w_id) =>
      {
        return new NewOrder_Input() { Data = tpcc.Transactions.NewOrderTransaction.InputData.Generate(gen, w_id) };
      });

      Handle.POST("/do/neworder", (NewOrder_Input d) =>
      {
        return new NewOrder_Output() { Data = tpcc.Transactions.NewOrderTransaction.Execute(d.ToCLR()) };
      });

      Handle.GET("/gen/orderstatus?W_ID={?}", (int w_id) =>
      {
        return new OrderStatus_Input() { Data = tpcc.Transactions.OrderStatusTransaction.InputData.Generate(gen, w_id) };
      });

      Handle.POST("/do/orderstatus", (OrderStatus_Input d) =>
      {
        return new OrderStatus_Output() { Data = tpcc.Transactions.OrderStatusTransaction.Execute(d.ToCLR()) };
      });

      Handle.GET("/gen/payment?W_ID={?}", (int w_id) =>
      {
        return new Payment_Input() { Data = tpcc.Transactions.PaymentTransaction.InputData.Generate(gen, w_id) };
      });

      Handle.POST("/do/payment", (Payment_Input d) =>
      {
        return new Payment_Output() { Data = tpcc.Transactions.PaymentTransaction.Execute(d.ToCLR()) };
      });

      Handle.GET("/gen/stocklevel?W_ID={?}&D_ID={?}", (int w_id, int d_id) =>
      {
        return new StockLevel_Input() { Data = tpcc.Transactions.StockLevelTransaction.InputData.Generate(gen, w_id, d_id) };
      });

      Handle.POST("/do/stocklevel", (StockLevel_Input d) =>
      {
        return new StockLevel_Output() { Data = tpcc.Transactions.StockLevelTransaction.Execute(d.ToCLR()) };
      });

      Handle.POST("/all_no_io?load_factor={?}", (int load_factor) =>
      {
        return DoAll_NoIO(load_factor);
      });
    }
  }
}
