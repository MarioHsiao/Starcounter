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
        Template GetMetaTemplate(Template template, ISourceInfo sourceInfo);
        Template GetMetaTemplate(Template parent, string propertyName, ISourceInfo debugInfo);
        void Verify(Template template);
    }
}
