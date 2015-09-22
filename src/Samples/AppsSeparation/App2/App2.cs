using System;
using Starcounter;

[Database]
public class App2ClassWithoutNamespace {
    public string App2ClassWithoutNamespaceField;
}

namespace App2 {

    [Database]
    public class App2Class {
        public string App2ClassField;
    }

    class App2 {
        static void Main() {

            String[] allowedClasses = {
                "App2Class",
                "App2.App2Class",
                "SharedDll.SharedDllClass",
                "SharedDllClass",
                "App2PrivateDll.App2PrivateDllClass",
                "App2PrivateDllClass",
                "App2ClassWithoutNamespace",
                "SharedClassWithoutNamespace"
            };

            foreach (String className in allowedClasses) {
                var x = Db.SQL("SELECT c FROM " + className + " c").First;
            }

            String[] disallowedClasses = { 
                "App1Class",
                "App1.App1Class",
                "App1PrivateDll.App1PrivateDllClass",
                "App1PrivateDllClass",
                "App1ClassWithoutNamespace"
            };

            foreach (String className in disallowedClasses) {

                Boolean accessed = false;

                try {
                    var x = Db.SQL("SELECT c FROM " + className + " c").First;
                    accessed = true;
                } catch { }

                if (accessed) {
                    throw new ArgumentException("Were able to access disallowed class: " + className);
                }
            }
        }
    }
}