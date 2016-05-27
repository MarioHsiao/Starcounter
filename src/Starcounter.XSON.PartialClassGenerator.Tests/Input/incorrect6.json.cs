
namespace MySampleNamespace {
    /// <summary>
    /// A class named as a root ...
    /// </summary>
    public partial class Incorrect6 {
        public string Name { get; set; }
    }

    /// <summary>
    /// ... has to be the only root class. If another class exist that claims
    /// to be a root, that will fail.
    /// </summary>
    [Incorrect6_json]
    public partial class DuplicateRoot {
    }
}