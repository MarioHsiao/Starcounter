
namespace MySampleNamespace {
    /// <summary>
    /// This class is not incorrect in itself. The next one is.
    /// </summary>
    public partial class Incorrect4 {
        public string Name { get; set; }
    }

    /// <summary>
    /// Code-behind classes must map to objects in the JSON data.
    /// This one does not.
    /// </summary>
    public partial class DoesNotMap {
        public int Foo { get; set; }
    }
}