
using Starcounter.Templates.Interfaces;
namespace Starcounter.Internal {
    public interface ITemplateCodeGeneratorModule {
        ITemplateCodeGenerator CreateGenerator( string lang, IAppTemplate template, object metadata );
    }
}
