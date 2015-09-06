
using Starcounter.Binding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Starcounter.Legacy {

    public sealed class LegacyContext {
        static object _sync = new object();
        static Dictionary<Application, LegacyContext> contexts = new Dictionary<Application, LegacyContext>();
        
        public string[] ExistingClasses { get; private set; }
        public string[] NewClasses { get; private set; }

        public static LegacyContext GetContext(Application application) {
            LegacyContext context = null;
            lock (_sync) {
                var found = contexts.TryGetValue(application, out context);
                if (!found) {
                    throw new Exception(
                        "No legacy context is available; this information is only accessible within the scope of the application entrypoint.");
                }
            }
            return context;
        }

        internal static void Enter(Application application, TypeDef[] allDefinitions, TypeDef[] newDefinitions) {
            var existingDefs = allDefinitions.Except(newDefinitions).ToArray();
            var context = new LegacyContext();
            context.ExistingClasses = Array.ConvertAll<TypeDef, string>(existingDefs, d => d.Name);
            context.NewClasses = Array.ConvertAll<TypeDef, string>(newDefinitions, d => d.Name);

            lock (_sync) {
                contexts[application] = context;
            }
        }

        internal static void Exit(Application application) {
            lock (_sync) {
                contexts.Remove(application);
            }
        }
    }
}
