using Starcounter;
using Starcounter.Internal;
using System.IO;

partial class TestApp : App {
    static void Main() {

        AppsBootstrapper.Bootstrap(8080,Path.GetDirectoryName(typeof(TestApp).Assembly.Location) + "\\..\\..");

        GET("/itworks", () => { return new TestApp() { View = "TestApp.html" }; });

    }


}
