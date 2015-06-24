
using System;

namespace Starcounter.Internal {
    /// <summary>
    /// Implement an intermediate call level layer, in between
    /// the weaver/host and the actual mapper logic. Will be replaced
    /// in the final, optimized solution.
    /// </summary>
    public static class MapInvoke {

        public static void POST(string name, ulong key) {
            var target = MakeURI(name, key);
            Map.POST(target);
        }

        public static void PUT(string name, ulong key) {
            var target = MakeURI(name, key);
            Map.PUT(target);
        }

        public static void DELETE(string name, ulong key) {
            var target = MakeURI(name, key);
            Map.DELETE(target);
        }

        static string MakeURI(string name, ulong key) {
            return string.Format("/{0}/{1}", name, key.ToString());
        }
    }
}