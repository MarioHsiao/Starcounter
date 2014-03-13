
using System.IO;
using Starcounter.Internal.JsonPatch;
using Starcounter.Templates;

namespace Starcounter.Internal.XSON.Tests {
    internal static class Helper {
        internal const string PATCH = "{{\"op\":\"replace\",\"path\":\"{0}\",\"value\":{1}}}";

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

        internal static string Jsonify(string input) {
            return '"' + input + '"';
        }

        internal static Input<Json, Property<T>, T> CreateInput<T>(Json pup, Property<T> prop, T value) {
            return new Input<Json, Property<T>, T>() {
                App = pup,
                Template = prop,
                Value = value
            }; 
        }
    }
}
