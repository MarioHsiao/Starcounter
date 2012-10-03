
using System.Collections.Generic;
namespace Starcounter.Templates.Interfaces {
    public interface IPropertyTemplates : IList<ITemplate> {

        ITemplate this[string id] { get; }

    }
}
