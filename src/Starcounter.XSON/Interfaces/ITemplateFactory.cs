using Starcounter.Templates;

namespace Starcounter.XSON.Interfaces {
    /// <summary>
    /// 
    /// </summary>
    public interface ITemplateFactory {
        Template AddObject(Template parent, string name, string dotNetName, ISourceInfo sourceInfo);

        Template AddString(Template parent, string name, string dotNetName, string value, ISourceInfo sourceInfo);

        Template AddInteger(Template parent, string name, string dotNetName, long value, ISourceInfo sourceInfo);

        Template AddDecimal(Template parent, string name, string dotNetName, decimal value, ISourceInfo sourceInfo);

        Template AddDouble(Template parent, string name, string dotNetName, double value, ISourceInfo debugInfo);

        Template AddBoolean(Template parent, string name, string dotNetName, bool value, ISourceInfo sourceInfo);

        Template AddArray(Template parent, string name, string dotNetName, ISourceInfo sourceInfo);

        // REMOVE
        //object AddEventProperty(object parent, string name, string dotNetName, int value, ISourceInfo debugInfo);

        // REMOVE
        //object AddCargoProperty(object parent, string name, string dotNetName, int value, ISourceInfo debugInfo);

        // REMOVE?
        //object AddMetaProperty(object template, ISourceInfo debugInfo);

        //object GetMetaTemplate(object template, ISourceInfo debugInfo);
        Template GetMetaTemplate(Template template, ISourceInfo sourceInfo);

        //object GetMetaTemplateForProperty(object template, string name, ISourceInfo debugInfo);
        Template GetMetaTemplate(Template parent, string propertyName, ISourceInfo debugInfo);

        void SetEditableProperty(Template template, bool b, ISourceInfo debugInfo);
        void SetClassProperty(Template template, string className, ISourceInfo debugInfo);
        void SetIncludeProperty(Template template, string className, ISourceInfo debugInfo);
        void SetNamespaceProperty(Template template, string namespaceName, ISourceInfo debugInfo);
        void SetBindProperty(Template template, string path, ISourceInfo debugInfo);
    }
}
