
namespace Starcounter.Templates.Interfaces {

    public interface IAppListTemplate : IParentTemplate {

        IAppTemplate Type { get; set; }

     //   IAppTemplate Set<T>() where T : IAppTemplate, new();

    }

}
