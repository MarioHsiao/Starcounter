using Starcounter;

partial class Master : App {

    static void Main() {

        GET("/empty", () => {
            return new Master() { View = "master.html" };
        });

        GET("/test", () => new TestApp() );

        GET("/products/@s", (string id) => {
            var master = Get<Master>("/~master");
            master.Page = new ProductApp() {
                Data = SQL( "SELECT Product FROM Product WHERE Id=?",id ).First,
                View = "product.html"
            };
            return master;
        });
/*
        GET("/orders/{id}", (int id) => {
            var master = Get<Master>("/~master");
            master.Page = new OrderApp() {
                Data = SQL( "SELECT Order FROM Order WHERE Id=?", id ).First,
                View = "orderentry.html"
            };
            return master;
        });

        GET("/new-order", (string id) => {
            var master = Get<Master>("/~master");
            master.Page = new OrderApp() {
                Data = new Order(),
                View = "orderentry.html"
            };
            return master;
        });
 */
    }
}
