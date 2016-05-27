
namespace MySampleNamespace {
    /// <summary>
    /// Classes can not map to multiple nodes
    /// </summary>
    [Foo_json]
    [Foo_json.Bar]
    public partial class Incorrect4 {
        public string Name { get; set; }
    }
}