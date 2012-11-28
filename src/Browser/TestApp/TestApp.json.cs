using Starcounter;
using Starcounter.Internal;
using System.IO;
using System;

partial class TestApp : App {
    static void Main() {

        AppsBootstrapper.Bootstrap(8080,Path.GetDirectoryName(typeof(TestApp).Assembly.Location) + "\\..\\..");

        GET("/itworks", () => { return new TestApp() { View = "TestApp.html" }; });

    }

    void Handle(Input.Items.Product._Search search) {
        Console.WriteLine(search.App.Parent.Parent.Parent);
    }

    [Json.Items]
    partial class ItemsApp : App {
    }

    [Json.Items.Product]
    partial class ProductApp : App {
    }


}
