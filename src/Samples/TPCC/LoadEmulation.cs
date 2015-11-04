using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tpcc
{
  public class TransactionsSet
  {
    public IEnumerable<Action> transactions { get; private set; }

    //generate transactions set according to 5.2.4.2.2
    public TransactionsSet(TpccValuesGenerator gen, int w_id, int d_id)
    {
      transactions = Enumerable.Range(1, 10).Select((_) => CreateNewOrderTransaction(gen, w_id))
             .Concat(Enumerable.Range(1, 10).Select((_) => CreatePaymentTransaction(gen, w_id)))
             .Concat(Enumerable.Range(1, 1).Select((_) => CreateOrderStatusTransaction(gen, w_id)))
             .Concat(Enumerable.Range(1, 1).Select((_) => CreateDeliveryTransaction(gen, w_id, d_id)))
             .Concat(Enumerable.Range(1, 1).Select((_) => CreateStockLevelTransaction(gen, w_id, d_id)));
    }

    private Action CreateNewOrderTransaction(TpccValuesGenerator gen, int w_id)
    {
      var input = tpcc.Transactions.NewOrderTransaction.InputData.Generate(gen, w_id);
      return () => { tpcc.Transactions.NewOrderTransaction.Execute(input); };
    }

    private Action CreatePaymentTransaction(TpccValuesGenerator gen, int w_id)
    {
      var input = tpcc.Transactions.PaymentTransaction.InputData.Generate(gen, w_id);
      return () => { tpcc.Transactions.PaymentTransaction.Execute(input); };
    }

    private Action CreateOrderStatusTransaction(TpccValuesGenerator gen, int w_id)
    {
      var input = tpcc.Transactions.OrderStatusTransaction.InputData.Generate(gen, w_id);
      return () => { tpcc.Transactions.OrderStatusTransaction.Execute(input); };
    }

    private Action CreateDeliveryTransaction(TpccValuesGenerator gen, int w_id, int d_id)
    {
      var input = tpcc.Transactions.DeliveryTransaction.InputData.Generate(gen, w_id, d_id);
      return () => { tpcc.Transactions.DeliveryTransaction.Execute(input); };
    }

    private Action CreateStockLevelTransaction(TpccValuesGenerator gen, int w_id, int d_id)
    {
      var input = tpcc.Transactions.StockLevelTransaction.InputData.Generate(gen, w_id, d_id);
      return () => { tpcc.Transactions.StockLevelTransaction.Execute(input); };
    }
  }

  public class DeckPerTerminal
  {
    public IEnumerable<Action> trasactions { get; private set; }
    public DeckPerTerminal(TpccValuesGenerator gen, int w_id, int d_id, int sets_number)
    {
      trasactions = gen.shuffle(
                          Enumerable.Repeat(new TransactionsSet(gen, w_id, d_id).transactions, sets_number)
                                    .SelectMany(x => x)
                    );
    }
  }
}
