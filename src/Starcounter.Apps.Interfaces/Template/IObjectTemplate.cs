using Starcounter;

namespace Starcounter.Templates.Interfaces {
    public interface IObjectTemplate : IValueTemplate {

        Entity DefaultValue { get; set; }
    }
}
