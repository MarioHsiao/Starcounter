using System;
using Starcounter;

[Database]
public class App1ClassWithoutNamespace {
    public string App1ClassWithoutNamespaceField;
}

namespace App1 {

    [Database]
    public class App1Class {
        public string App1ClassField;
    }

    class App1 {
        static void Main() {

            String[] allowedClasses = {
                "App1Class",
                "App1.App1Class",
                "SharedDll.SharedDllClass",
                "SharedDllClass",
                "App1PrivateDll.App1PrivateDllClass",
                "App1PrivateDllClass",
                "App1ClassWithoutNamespace",
                "SharedClassWithoutNamespace"
            };

            foreach (String className in allowedClasses) {
                var x = Db.SQL("SELECT c FROM " + className + " c").First;
            }

            String[] disallowedClasses = { 
                "App2Class",
                "App2.App2Class",
                "App2PrivateDll.App2PrivateDllClass",
                "App2PrivateDllClass" };

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