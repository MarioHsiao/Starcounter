
namespace Starcounter {
    public class App<T> : App where T : Entity {
        public new T Data { get; set; }
    }
}