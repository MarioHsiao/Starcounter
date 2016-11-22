using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tpcc
{
    public struct TransactionDefinition
    {
        public Func<Task> self_execute;
        public string uri;
        public string body;
        public bool serialization_required;
    }

    public static class LoadEmulation
    {
        //generate transactions set according to 5.2.4.2.2
        public static IEnumerable<TransactionDefinition> CreateSet(TpccValuesGenerator gen, int w_id, int d_id)
        {
            return Enumerable.Range(1, 10).Select((_) => CreateNewOrderTransaction(gen, w_id))
                   .Concat(Enumerable.Range(1, 10).Select((_) => CreatePaymentTransaction(gen, w_id)))
                   .Concat(Enumerable.Range(1, 1).Select((_) => CreateOrderStatusTransaction(gen, w_id)))
                   .Concat(Enumerable.Range(1, 1).Select((_) => CreateDeliveryTransaction(gen, w_id, d_id)))
                   .Concat(Enumerable.Range(1, 1).Select((_) => CreateStockLevelTransaction(gen, w_id, d_id)));
        }

        public static IEnumerable<TransactionDefinition> CreateDeck(TpccValuesGenerator gen, int w_id, int d_id, int sets_number)
        {
            return gen.shuffle(
                        Enumerable.Repeat(CreateSet(gen, w_id, d_id), sets_number)
                                  .SelectMany(x => x)
                   );
        }

        public static TransactionDefinition CreateNewOrderTransaction(TpccValuesGenerator gen, int w_id)
        {
            var input = tpcc.Transactions.NewOrderTransaction.InputData.Generate(gen, w_id);
            return new TransactionDefinition
            {
                self_execute = async () => { await tpcc.Transactions.NewOrderTransaction.Execute(input); },
                uri = "/do/neworder",
                body = new NewOrder_Input { Data = input }.ToJson(),
                serialization_required = true
            };
        }

        public static TransactionDefinition CreatePaymentTransaction(TpccValuesGenerator gen, int w_id)
        {
            var input = tpcc.Transactions.PaymentTransaction.InputData.Generate(gen, w_id);
            return new TransactionDefinition
            {
                self_execute = async () => { await tpcc.Transactions.PaymentTransaction.Execute(input); },
                uri = "/do/payment",
                body = new Payment_Input { Data = input }.ToJson(),
                serialization_required = true
            };
        }

        public static TransactionDefinition CreateOrderStatusTransaction(TpccValuesGenerator gen, int w_id)
        {
            var input = tpcc.Transactions.OrderStatusTransaction.InputData.Generate(gen, w_id);
            return new TransactionDefinition
            {
                self_execute = async () => { await tpcc.Transactions.OrderStatusTransaction.Execute(input); },
                uri = "/do/orderstatus",
                body = new OrderStatus_Input { Data = input }.ToJson(),
                serialization_required = true
            };
        }

        public static TransactionDefinition CreateDeliveryTransaction(TpccValuesGenerator gen, int w_id, int d_id)
        {
            var input = tpcc.Transactions.DeliveryTransaction.InputData.Generate(gen, w_id, d_id);
            return new TransactionDefinition
            {
                self_execute = async ()  => { await tpcc.Transactions.DeliveryTransaction.Execute(input); },
                uri = "/do/delivery",
                body = new Delivery_Input { Data = input }.ToJson(),
                serialization_required = false
            };
        }

        public static TransactionDefinition CreateStockLevelTransaction(TpccValuesGenerator gen, int w_id, int d_id)
        {
            var input = tpcc.Transactions.StockLevelTransaction.InputData.Generate(gen, w_id, d_id);
            return new TransactionDefinition
            {
                self_execute = async () => { await tpcc.Transactions.StockLevelTransaction.Execute(input); },
                uri = "/do/stocklevel",
                body = new StockLevel_Input{ Data = input }.ToJson(),
                serialization_required = true
            };
        }
    }

    public class DeckPerTerminal
    {
        public IEnumerable<TransactionDefinition> trasactions { get; private set; }
    }
}
