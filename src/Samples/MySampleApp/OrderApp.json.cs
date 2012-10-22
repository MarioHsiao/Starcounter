using Starcounter;
using System;

partial class OrderApp : App<Order> {

    protected override void OnData() {
        if (Items.Count == 0 ) //!Items[Items.Count-1].IsEmpty() )
            Items.Add( new OrderItem() { Order = this.Data, Quantity = 1 });        
    }

    void Handle(Input.Items.Product._Search search) {
        search.Parent._Options = SQL("SELECT Product FROM Product WHERE Description LIKE ?", search.Value + "%");
    }

//    void Handle(Input.Items.Product._Options.Pick pick) {
//     //   pick.Parent.Parent.Data = pick.Parent.Data;
//    }

//    void Handle( Input.Save save ) {
//        Commit();
//    }

//    void Handle( Input.Cancel cancel ) {
//        Abort();
//    }

//    [OrderApp.Json.Items.Product]
//    partial class OrderItemProductApp : App<Product> {
//        protected override void OnData() {
//            this._Search = Data.Description;
//        }
//    }
}
