
using Starcounter.Templates.Interfaces;
#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

    public abstract class ListTemplate : ParentTemplate
#if IAPP
        , IListTemplate
#endif
    {
    }
}
