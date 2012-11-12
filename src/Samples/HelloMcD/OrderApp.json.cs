using System;
using Starcounter;

namespace HelloMcd {
    partial class OrderApp : App<Order> {
        protected override void OnData() {
            // TODO:
            // Remove when databinding is in place.
            this.OrderNo = (int)Data.OrderNo;

            if (Items.Count == 0)
                Items.Add(new OrderItem() { Order = this.Data, Quantity = 1 });
        }

        void Handle(Input.Items.Product._Search search) {
            // TODO:
            // Parent field on input is always null...should be removed or return App I guess.

            // TODO:
            // Databinding and allow = between lists and sqlresult.

            // This is what we want to write:
            //        search.App._Options = SQL("SELECT Product FROM Product WHERE Description LIKE ?", search.Value + "%");

            // And this is what we write now:
            for (int i = search.App._Options.Count - 1; i >= 0; i--) {
                search.App._Options.RemoveAt(i);
            }

            SqlResult result = SQL("SELECT p FROM Product p WHERE Description LIKE ?", search.Value + "%");
            foreach (Product p in result) {
                var opt = search.App._Options.Add();
                opt.Data = p;
                opt.Description = p.Description;
            }
        }

        void Handle(Input.Items.Product._Options.Pick pick) {
            ItemsApp itemApp = (ItemsApp)pick.App.Parent.Parent.Parent;
//            if (itemApp.Quantity != 10) {
                ((App)pick.App.Parent.Parent).Data = pick.App.Data;
                this.Items.Add(new OrderItem() { Order = this.Data, Quantity = 1 }); // Add a new empty row;
//            }
        }

        void Handle(Input.Save save) {
            Commit();
        }

        void Handle(Input.Cancel cancel) {
            Abort();
        }

        [Json.Items.Product]
        partial class OrderItemProductApp : App<Product> {
            protected override void OnData() {
                this._Search = Data.Description;

                // TODO:
                // Remove when databinding is in place.
                ItemsApp item = (ItemsApp)this.Parent;
                item.Price = Data.Price;
                item.Quantity = 10;

                OrderApp order = (OrderApp)item.Parent.Parent;
                // TODO:
                // Make total work.
//                order.Total = order.Data.Total;
            }
        }
    }
}