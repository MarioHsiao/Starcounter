
using System;

namespace Starcounter.Internal {

    public static class MapInvoke {

        public static void POST(string name, ulong key) {
            var target = MakeURI(name, key);
            Console.WriteLine(target);
        }

        public static void PUT(string name, ulong key) {
            var target = MakeURI(name, key);
            Console.WriteLine(target);
        }

        public static void DELETE(string name, ulong key) {
            var target = MakeURI(name, key);
            Console.WriteLine(target);
        }

        static string MakeURI(string name, ulong key) {
            var s = name.Replace('.', '/').ToLowerInvariant();
            return string.Format("{0}/{1}", s, key.ToString());
        }
    }
}