using Starcounter;
using System.Collections.Generic;

public class Product : Entity {
    public string Image;
    public string ProductId;
    public string Description;
    public decimal Price;
}

public class Order : Entity {
    public long OrderNo;

    public decimal Total {
        get {
            return (decimal)App.SQL("SELECT Sum(Price+Quantity) FROM OrderItem WHERE Order=?", this).First;
        }
    }
    public IEnumerable<OrderItem> Items {
        get {
            return (IEnumerable<OrderItem>)App.SQL("SELECT OrderItem FROM OrderItem WHERE Order=?", this);
        }
    }
}

public class OrderItem: Entity {
    public Order Order;
    public Product Product;
    public decimal Price;
    public int Quantity;
}
