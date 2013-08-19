// ***********************************************************************
// <copyright file="DomGenerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Collections.Generic;
using Starcounter.XSON.Metadata;

namespace Starcounter.Internal.MsBuild.Codegen {

    /// <summary>
    /// Simple code-dom generator for the Template class. In a Template tree structure,
    /// each Template will be represented by a temporary CsGen_Template object. The reason
    /// for this is to avoid cluttering the original Template code with code generation
    /// concerns while still employing a polymorphic programming model to implement the
    /// unique functionality of each type of Template (see the virtual functions).
    /// </summary>
    /// <remarks>Class nodes can easily be moved to a new parent by setting the Parent property on
    /// the node. This can been done after the DOM tree has been generated. This is used
    /// to allow the generated code structure match the code behind structure. In this way,
    /// there is no need for the programmer to have deep nesting of class declarations in
    /// JSON trees.</remarks>
    public class Gen2DomGenerator {
        internal Gen2DomGenerator(Gen2CodeGenerationModule mod, TObj template, Type defaultNewObjTemplateType, CodeBehindMetadata metadata) {
            DefaultObjTemplate = (TObj)defaultNewObjTemplateType.GetConstructor(new Type[0]).Invoke(null);
            InitTemplateClasses();
            InitMetadataClasses();
            InitValueClasses();
            CodeBehindMetadata = metadata;
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Template, AstValueClass> ValueClasses = new Dictionary<Template, AstValueClass>();
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Template, AstTemplateClass> TemplateClasses = new Dictionary<Template, AstTemplateClass>();
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Template, AstMetadataClass> MetaClasses = new Dictionary<Template, AstMetadataClass>();

        /// <summary>
        /// Initializes static members of the <see cref="AstTemplateClass" /> class.
        /// </summary>
        void InitTemplateClasses() {
            TemplateClasses[TPString] = new AstPropertyClass(this) { Template = TPString };
            TemplateClasses[TPLong] = new AstPropertyClass(this) { Template = TPLong };
            TemplateClasses[TPDecimal] = new AstPropertyClass(this) { Template = TPDecimal };
            TemplateClasses[TPDouble] = new AstPropertyClass(this) { Template = TPDouble };
            TemplateClasses[TPBool] = new AstPropertyClass(this) { Template = TPBool };
            TemplateClasses[TPAction] = new AstPropertyClass(this) { Template = TPAction };
            TemplateClasses[DefaultObjTemplate] = new AstTAppClass(this) { Template = DefaultObjTemplate };
        }

        /// <summary>
        /// Initializes static members of the <see cref="AstMetadataClass" /> class.
        /// </summary>
        void InitMetadataClasses() {
            MetaClasses[TPString] = new AstMetadataClass(this) { NTemplateClass = TemplateClasses[TPString] };
            MetaClasses[TPLong] = new AstMetadataClass(this) { NTemplateClass = TemplateClasses[TPLong] };
            MetaClasses[TPDecimal] = new AstMetadataClass(this) { NTemplateClass = TemplateClasses[TPDecimal] };
            MetaClasses[TPDouble] = new AstMetadataClass(this) { NTemplateClass = TemplateClasses[TPDouble] };
            MetaClasses[TPBool] = new AstMetadataClass(this) { NTemplateClass = TemplateClasses[TPBool] };
            MetaClasses[TPAction] = new AstMetadataClass(this) { NTemplateClass = TemplateClasses[TPAction] };
            MetaClasses[DefaultObjTemplate] = new AstMetadataClass(this) { NTemplateClass = TemplateClasses[DefaultObjTemplate] };
        }

        /// <summary>
        /// Initializes static members of the <see cref="AstValueClass" /> class.
        /// </summary>
        void InitValueClasses() {
            ValueClasses[TPString] = new AstPrimitiveType(this) { NTemplateClass = TemplateClasses[TPString] };
            ValueClasses[TPLong] = new AstPrimitiveType(this) { NTemplateClass = TemplateClasses[TPLong] };
            ValueClasses[TPDecimal] = new AstPrimitiveType(this) { NTemplateClass = TemplateClasses[TPDecimal] };
            ValueClasses[TPDouble] = new AstPrimitiveType(this) { NTemplateClass = TemplateClasses[TPDouble] };
            ValueClasses[TPBool] = new AstPrimitiveType(this) { NTemplateClass = TemplateClasses[TPBool] };
            ValueClasses[TPAction] = new AstPrimitiveType(this) { NTemplateClass = TemplateClasses[TPAction] };
            ValueClasses[DefaultObjTemplate] = new AstAppClass(this) { NTemplateClass = TemplateClasses[DefaultObjTemplate] };
        }

        /// <summary>
        /// Finds the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>NValueClass.</returns>
        public AstValueClass FindValueClass(Template template) {
            template = GetPrototype(template);
            return ValueClasses[template];
        }

        /// <summary>
        /// Finds the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>NMetadataClass.</returns>
        public AstMetadataClass FindMetaClass(Template template) {
            template = GetPrototype(template);
            return MetaClasses[template];
        }

        /// <summary>
        /// Gets the prototype.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>Template.</returns>
        public Template GetPrototype(Template template) {
            if (template is TString) {
                return TPString;
            } else if (template is TLong) {
                return TPLong;
            } else if (template is TDouble) {
                return TPDouble;
            } else if (template is TDecimal) {
                return TPDecimal;
            } else if (template is TBool) {
                return TPBool;
            } else if (template is TTrigger) {
                return TPAction;
            }
            return template;
        }

        /// <summary>
        /// Finds the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>NTemplateClass.</returns>
        public AstTemplateClass FindTemplateClass(Template template) {
            // template = GetPrototype(template);
            return TemplateClasses[template];
        }

        internal TString TPString = new TString();
        internal TLong TPLong = new TLong();
        internal TDecimal TPDecimal = new TDecimal();
        internal TObj DefaultObjTemplate = null;
        internal TDouble TPDouble = new TDouble();
        internal TBool TPBool = new TBool();
        internal TTrigger TPAction = new TTrigger();

        /// <summary>
        /// This is the main calling point to generate a dom tree for a JSON template (TJson).
        /// </summary>
        /// <param name="at">The TJson template (i.e. json tree prototype) to generate code for</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>An abstract code tree. Use CSharpGenerator to generate .CS code.</returns>
        public AstRoot GenerateDomTree(TObj at) {

            var p1 = new GeneratorPhase1() { Generator = this };
            var p2 = new GeneratorPhase2() { Generator = this };
            var p3 = new GeneratorPhase3() { Generator = this };
            var p4 = new GeneratorPhase4() { Generator = this };

            AstAppClass acn;
            AstTAppClass tcn;
            AstObjMetadata mcn;

            this.Root = p1.RunPhase1(at, out acn, out tcn, out mcn );
            p2.RunPhase2(acn,tcn,mcn);
            p3.RunPhase3(acn);
            p4.RunPhase4(acn);

            return this.Root;
            // TODOJOCKE                ConnectCodeBehindClasses(root, metadata);
            //  TODOJOCKE              GenerateInputBindings((AstTAppClass)acn.NTemplateClass, metadata);
            // CheckMissingBindingInformation(tcn);
        }

        internal AstRoot Root;
        internal CodeBehindMetadata CodeBehindMetadata;

            //return root;



        private AstBase FindRootNAppClass(AstAppClass appClassParent) {
            AstBase next = appClassParent;
            while (!(next.Parent is AstRoot))
                next = next.Parent;
            return next;
        }






        /// <summary>
        /// Finds the class where the Handle method is declared. This can be the same class
        /// as where the property is declared or a parentclass.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="info">The info.</param>
        /// <exception cref="System.Exception">Could not find the app where Handle method is declared.</exception>
        internal void FindHandleDeclaringClass(AstInputBinding binding, InputBindingInfo info) {
            Int32 parentCount = 0;
            TContainer candidate = binding.PropertyAppClass.Template;
            TObj appTemplate;
            AstAppClass declaringAppClass = null;

            while (candidate != null) {
                appTemplate = candidate as TObj;
                if (appTemplate != null) {
                    if (info.DeclaringClassName.Equals(appTemplate.ClassName)) {
                        declaringAppClass = (AstAppClass)FindValueClass(appTemplate);
                        break;
                    }
                }

                candidate = candidate.Parent;
                parentCount++;
            }

            if (declaringAppClass == null) {
                throw new Exception("Could not find the app where Handle method is declared.");
            }

            binding.DeclaringAppClass = declaringAppClass;
            binding.AppParentCount = parentCount;
        }

        /// <summary>
        /// Employed by the template code generator.
        /// </summary>
        /// <value>The global namespace.</value>
        internal string GlobalNamespace {
            get {
                Template current = DefaultObjTemplate;
                while (current.Parent != null)
                    current = (Template)current.Parent;
                return ((TObj)current).Namespace;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="messagePostFix"></param>
        /// <param name="innerException"></param>
        /// <param name="co"></param>
        internal void ThrowExceptionWithLineInfo(uint errorCode, string messagePostFix, Exception innerException, CompilerOrigin co) {
            var tuple = new Tuple<int, int>(co.LineNo, co.ColNo);
            throw ErrorCode.ToException(
                    errorCode,
                    innerException,
                    messagePostFix,
                    (msg, e) => {
                        return Starcounter.Internal.JsonTemplate.Error.CompileError.Raise<Exception>(msg, tuple, co.FileName);
                    });
        }
    }
}
