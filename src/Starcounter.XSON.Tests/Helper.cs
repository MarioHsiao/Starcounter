
using System.IO;
using Starcounter.Internal.JsonPatch;
using Starcounter.Templates;

namespace Starcounter.Internal.XSON.Tests {
    internal static class Helper {
        internal static AppAndTemplate CreateSampleApp() {
            dynamic template = TObject.CreateFromJson(File.ReadAllText("SampleApp.json"));
            dynamic app = new Json() { Template = template };

            app.FirstName = "Cliff";
            app.LastName = "Barnes";

            var itemApp = app.Items.Add();
            itemApp.Description = "Take a nap!";
            itemApp.IsDone = false;

            itemApp = app.Items.Add();
            itemApp.Description = "Fix Apps!";
            itemApp.IsDone = true;

            return new AppAndTemplate(app, template);
        }
    }
}
