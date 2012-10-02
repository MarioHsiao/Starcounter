
using Starcounter.Templates.Interfaces;

namespace Starcounter.Templates.Interfaces {

    /// <summary>
    /// Represents a node in the schema tree.
    /// </summary>
    public interface ITemplate {

        /// <summary>
        /// The parent of this template, if any. I.e. if this template represents a property in a JSON style object, the parent
        /// is the JSON style object. Alternatively, if this template represents an array element in a JSON style array,
        /// the parent points to the JSON style array.
        /// </summary>
        IParentTemplate Parent {
            get;
            set;
        }

        bool Sealed { get; }

        bool Visible { get; set; }
        bool Enabled { get; set; }

        /// <summary>
        /// Each template with a parent has an internal position amongst its siblings
        /// </summary>
        int Index { get; }

        /// <summary>
        /// The property name of this element used in the parent. I.e. if this template represents a JSON style object, 
        /// this is the Name in the Name/Value pair of the hosting JSON style object as defined at www.json.org.
        /// </summary>
        string Name {
            get;
            set;
        }

        /// <summary>
        /// This is a transformed version of the Name property in a format allowed by all of the folowing languages
        /// Java/NET/C/C++/Ruby/ObjectiveC. This means that any use of $ in the Name will be stripped out. The Name will
        /// still contain the original property Name as used in a remote view model.
        /// </summary>
        string PropertyName {
            get;
            set;
        }

    }
}
