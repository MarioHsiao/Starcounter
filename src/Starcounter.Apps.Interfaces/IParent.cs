
namespace Starcounter.Templates.Interfaces {
    public interface IAppNode {

        /// <summary>
        /// The schema element of this app instance
        /// </summary>
        IParentTemplate Template { get; set; }
        IAppNode Parent { get; set; }

    }
}
