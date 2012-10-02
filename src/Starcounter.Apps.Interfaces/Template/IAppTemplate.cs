
namespace Starcounter.Templates.Interfaces {
    public interface IAppTemplate : IParentTemplate {

        T Add<T>(string name) where T : ITemplate, new();
        T Add<T>(string name, IAppTemplate type ) where T : IAppListTemplate, new();

        IPropertyTemplates Properties { get; }
    }
}
