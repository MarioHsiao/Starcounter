

using Starcounter.Templates;
using Starcounter.Templates.Interfaces;
using System;
using System.Collections.Generic;
namespace Starcounter.Internal.Application.CodeGeneration {
    public class CodeGenerationModule : ITemplateCodeGeneratorModule {

        private Dictionary<Type,Func<string,Template,DomGenerator,DomGenerator>> Generators;

        public static Dictionary<Type,NPredefinedType> FixedAppTypes;
        public static Dictionary<Type, NPredefinedType> FixedTemplateTypes;
        public static Dictionary<Type, NPredefinedType> FixedMetaDataTypes;

        public CodeGenerationModule() {
            Generators = new Dictionary<Type, Func<string, Template, DomGenerator, DomGenerator>>();
            FixedAppTypes = new Dictionary<Type, NPredefinedType>();
            FixedTemplateTypes = new Dictionary<Type, NPredefinedType>();
            FixedMetaDataTypes = new Dictionary<Type, NPredefinedType>();

            CreateFixedDomNodes(typeof(AppTemplate), "App", false, "AppTemplate", "AppMetadata");
            CreateFixedDomNodes(typeof(StringProperty), "string", true, "StringProperty", "StringMetadata");
            CreateFixedDomNodes(typeof(BoolProperty), "bool", true, "BoolProperty", "BoolMetadata");
            CreateFixedDomNodes(typeof(DecimalProperty), "decimal", true, "DecimalProperty", "DecimalMetadata");
            CreateFixedDomNodes(typeof(DoubleProperty),"double", true, "DoubleProperty", "DoubleMetadata" );
            CreateFixedDomNodes(typeof(IntProperty),"int", true, "IntProperty", "IntMetadata");
            CreateFixedDomNodes(typeof(ListTemplate),"Listing", false, "ListingProperty", "ListingMetadata");
            CreateFixedDomNodes(typeof(ActionProperty),"Action", true, "ActionProperty", "ActionMetadata" );
        }

        public void CreateFixedDomNodes(Type type, string appType, bool isPrimitive, string templateType, string metadataType) {
            FixedAppTypes[type] = new NPredefinedType() { FixedClassName = appType, IsPrimitive = isPrimitive };
            FixedTemplateTypes[type] = new NPredefinedType() { FixedClassName = templateType, IsPrimitive = isPrimitive };
            FixedMetaDataTypes[type] = new NPredefinedType() { FixedClassName = metadataType, IsPrimitive = isPrimitive };
        }

        public ITemplateCodeGenerator CreateGenerator(string dotNetLanguage, IAppTemplate template, object metadata) {
            var gen = new DomGenerator(this, (AppTemplate)template);
            return new CSharpGenerator(gen.GenerateDomTree((AppTemplate)template, (CodeBehindMetadata)metadata));
        }

    }
}
