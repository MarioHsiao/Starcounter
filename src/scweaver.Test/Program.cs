
using NUnit.Framework;
using System;
using System.Reflection;

namespace scweaver.Test {
    
    /// <summary>
    /// A program class with an entrypoint, allowing the tests in this
    /// assembly to be run from the OS shell, by turning it into an exe.
    /// </summary>
    public static class Program {

        /// <summary>
        /// Executes all test methods of all test classes in this assembly.
        /// </summary>
        /// <remarks>
        /// Allows this assembly to be turned into an executable and
        /// execute tests from the OS shell.
        /// </remarks>
        public static void Main() {

            foreach (var testFixtureType in Assembly.GetExecutingAssembly().GetTypes()) {

                if (testFixtureType.IsDefined(typeof(TestFixtureAttribute))) {
                    var fixture = testFixtureType.GetConstructor(Type.EmptyTypes).Invoke(null);
                    foreach (var item in testFixtureType.GetMethods()) {
                        if (item.ReturnType == typeof(void) &&
                            item.GetParameters().Length == 0 &&
                            item.GetCustomAttribute(typeof(TestAttribute)) != null) {
                            item.Invoke(fixture, null);
                        }
                    }
                }
            }
        }
    }
}