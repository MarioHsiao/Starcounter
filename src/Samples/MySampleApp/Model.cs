using Starcounter;
using System.Collections.Generic;


public class Something : Entity {
    public virtual bool IsEmpty() {
        return false;
    }
}

public class Product : Something {
//    public ProductGroup Group;
    public string Image;
    public string ProductId;
    public string Description;
    public decimal Price;
}

public class SequentialNumber : Entity {
    public string Id;
    public long LastUsedNumber;

    public static long GetNextNumber( string id ) {
        SequentialNumber x = null;
        App.Transaction(delegate
        {
            x = App.SQL("SELECT SequentialNumber WHERE Id=?", id).First;
            if (x == null) {
                x = new SequentialNumber();
            }
            x.LastUsedNumber++;
        });
        return x.LastUsedNumber;
    }
}

public class Order : Something {
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

    /*
    public decimal Total {
        get {
            return (decimal)SQL("SELECT Sum(Price+QuantityNotIncludedElsewhere) FROM OrderItem WHERE Order=?", this).First;
        }
    }
    public IEnumerable<OrderItem> OrderedItems {
        get {
            return (IEnumerable<OrderItem>)SQL("SELECT OrderItem FROM OrderItem WHERE Order=? AND !(Product is Menu)", this);
        }
    }
    public IEnumerable<OrderItem> Menues {
        get {
            return (IEnumerable<OrderItem>)SQL("SELECT OrderItem FROM OrderItem WHERE Order=? AND (Product is Menu)", this);
        }
    }
    public IEnumerable<OrderItem> ChargedItems {
        get {
            return (IEnumerable<OrderItem>)SQL("SELECT OrderItem FROM OrderItem WHERE Order=? AND Amount<>?", this, 0);
        }
    }
     */

    public void PlaceOrder() {
        if (OrderNo == 0)
            OrderNo = SequentialNumber.GetNextNumber("Order");
    }
/*
    public void RecalculateMenues() {
        foreach (var menu in Menues)
            menu.Delete();
        var menues = SQL("SELECT Menu FROM Menu");
        foreach (var menu in menues) {
            int matched = menu.MatchOrderItems(Items);
            if (matched > 0) {
                MoveToMenu(menu, matched);
            }
        }
    }

    public void MoveToMenu(Menu menu, int quant) {
        // TODO!
    }
 */
}

public class OrderItem: Something {
    public Order Order;
    public Product Product;
    public decimal Price;
    public int Quantity;

    /*    private int _Quantity;

        public int Quantity {
            get { return _Quantity; }
            set {
                _Quantity = value;
                Order.RecalculateMenues();
            }
        }

        public int QuantityNotIncludedElsewhere;

        public int QuantityIncludedElsewhere {
            get { return Quantity - QuantityNotIncludedElsewhere; }
        }

        public decimal Amount {
            get {
                return QuantityNotIncludedElsewhere * Price;
            }
        }
     */

    public override bool IsEmpty() {
        return (Product == null);
    }
}
/*
public class Menu : Product {
    public decimal Discount {
        get {
            decimal regularTotal = (decimal)SQL("SELECT Sum(Product.Price) FROM MenuItem WHERE Menu=?", this ).First;
            decimal discountedTotal = Price;
            decimal discount = (  discountedTotal / regularTotal );
            return discount * 100;
        }
    }

    public int MatchOrderItems(IEnumerable<OrderItem> items) {
        // TODO!
        return 0;
    }
}

public class ProductGroup : Entity {
    public string Description;
}

public class MenuItem : Entity {
    public Menu Menu;
    public ProductGroup ProductGroup;
}
*/