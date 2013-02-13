
using Starcounter.Advanced;
namespace Starcounter {
    public class App : Obj {


        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String" /> to <see cref="App" />.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator App(string str) {
            return new App() { Media = str };
        }

    }

    public class App<T> : Obj<T> where T : IBindable {

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String" /> to <see cref="App" />.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator App<T>(string str) {
            return new App<T>() { Media = str };
        }
    }
}
