// ***********************************************************************
// <copyright file="DomGenerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Collections.Generic;
using Starcounter.XSON.Metadata;
using TJson = Starcounter.Templates.TObject;

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
    public partial class Gen2DomGenerator {
        internal const string InstanceDataTypeName = "InstanceDataTypeName";
        internal const string Reuse = "Reuse";

        internal Gen2DomGenerator(Gen2CodeGenerationModule mod, TJson template, Type defaultNewObjTemplateType, CodeBehindMetadata metadata) {
            DefaultObjTemplate = (TJson)defaultNewObjTemplateType.GetConstructor(new Type[0]).Invoke(null);
            CodeBehindMetadata = metadata;
            AstObject = new AstOtherClass(this) {
                GlobalClassSpecifier = "object",
                NamespaceAlias = null
            };
        }

        private AstOtherClass AstObject;


        /// <summary>
        /// This is the main calling point to generate a dom tree for a JSON template (TJson).
        /// </summary>
        /// <param name="at">The TJson template (i.e. json tree prototype) to generate code for</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>An abstract code tree. Use CSharpGenerator to generate .CS code.</returns>
        public AstRoot GenerateDomTree(TJson at) {

            var p1 = new GeneratorPhase1() { Generator = this };
            var p2 = new GeneratorPhase2() { Generator = this };
            var p3 = new GeneratorPhase3() { Generator = this };
            var p4 = new GeneratorPhase4() { Generator = this };
            var p5 = new GeneratorPhase5() { Generator = this };
            var p6 = new GeneratorPhase6() { Generator = this };

            AstJsonClass acn;
            AstSchemaClass tcn;
            AstMetadataClass mcn;

            this.Root = p1.RunPhase1(at, out acn, out tcn, out mcn );
            p2.RunPhase2(acn,tcn,mcn);
            p3.RunPhase3(acn);
            p4.RunPhase4(acn);
            p5.RunPhase5(acn, tcn, mcn);
            p6.RunPhase6(acn);

            return this.Root;
            // TODOJOCKE                ConnectCodeBehindClasses(root, metadata);
            //  TODOJOCKE              GenerateInputBindings((AstTAppClass)acn.NTemplateClass, metadata);
            // CheckMissingBindingInformation(tcn);
        }

        internal AstRoot Root;
        internal CodeBehindMetadata CodeBehindMetadata;


            //return root;



        private AstBase FindRootNAppClass(AstJsonClass appClassParent) {
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
            TJson appTemplate;
            AstJsonClass declaringAppClass = null;

            while (candidate != null) {
                appTemplate = candidate as TJson;
                if (appTemplate != null) {
                    if (info.DeclaringClassName.Equals(appTemplate.ClassName)) {
                        declaringAppClass = (AstJsonClass)ObtainValueClass(appTemplate);
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

        /*
        /// <summary>
        /// Employed by the template code generator.
        /// </summary>
        /// <value>The global namespace.</value>
        internal string GlobalNamespace {
            get {
                Template current = DefaultObjTemplate;
                while (current.Parent != null)
                    current = (Template)current.Parent;
                return ((TJson)current).Namespace;
            }
        }
        */

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
