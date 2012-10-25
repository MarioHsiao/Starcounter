using System;
using Starcounter;

partial class OrderApp : App<Order> {
    protected override void OnData() {
        //if (Items.Count == 0) //!Items[Items.Count-1].IsEmpty() )
        //    Items.Add(new OrderItem() { Order = this.Data, Quantity = 1 });
    }

    void Handle(Input.Items.Product._Search search) {
        // TODO:
        // Parent field on input is always null...should be
        // removed or return App I guess.

//        search.App._Options = SQL("SELECT Product FROM Product WHERE Description LIKE ?", search.Value + "%");
        var opt = search.App._Options.Add();
        opt.Description = "Sweet Bacon BBQ";
        opt.Image = "SweetBacon.jpg";

        opt = search.App._Options.Add();
        opt.Description = "Big Mac & Co";
        opt.Image = "bigmc.jpg";
    }

    void Handle(Input.Items.Product._Options.Pick pick) {
//        ((App)pick.Parent.Parent).Data = pick.Parent.Data;
        OrderItemProductApp itemApp = ((OrderItemProductApp)pick.App.Parent);
        itemApp._Search = pick.App.Description;

        this.Items.Add(); // Add a new empty row;
    }

    void Handle(Input.Save save) {
        Commit();
    }

    void Handle(Input.Cancel cancel) {
        Abort();
    }

    [Json.Items.Product]
    partial class OrderItemProductApp : App<Product> {
        //protected override void OnData() {
        //    this._Search = Data.Description;
        //}
    }
}
