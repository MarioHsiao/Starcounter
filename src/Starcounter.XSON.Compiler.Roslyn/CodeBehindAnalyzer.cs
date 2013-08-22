﻿// ***********************************************************************
// <copyright file="CodeBehindAnalyzer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Roslyn.Compilers.CSharp;
using Starcounter.XSON.Metadata;

namespace Starcounter.XSON.Compiler.Roslyn {
    /// <summary>
    /// Class CodeBehindAnalyzer
    /// </summary>
    internal static class CodeBehindAnalyzer {
        /// <summary>
        /// Parses the specified c# file using Roslyn and builds a metadata
        /// structure used to generate code for json Apps.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="codeBehindFilename">The code behind filename.</param>
        /// <returns>CodeBehindMetadata.</returns>
        internal static CodeBehindMetadata Analyze(string className, string codeBehindFilename) {
            bool autoBindToDataObject = false;
            ClassDeclarationSyntax classDecl;
            List<CodeBehindClassInfo> mapList = new List<CodeBehindClassInfo>();
            List<InputBindingInfo> inputList = new List<InputBindingInfo>();
            string ns = null;
            string genericArg = null;
            string jsonInstanceName = null;
            SyntaxNode root;
            SyntaxTree tree;

            if (!File.Exists(codeBehindFilename))
                return CodeBehindMetadata.Empty;

            // TODO:
            // We need to specify this somewhere else then here. Whenever the json-name changes
            // this class stops working. 
            jsonInstanceName = "Json";
            
            tree = SyntaxTree.ParseFile(codeBehindFilename);
            root = tree.GetRoot();

            classDecl = FindClassDeclarationFor(className, root);
            if (IsTypedJsonClass(classDecl)) {
                autoBindToDataObject = IsBoundToEntity(classDecl, jsonInstanceName, out genericArg);
                ns = GetNamespaceForClass(className, root);
                FillListWithJsonMapInfo(className, root, jsonInstanceName, mapList);
                FillListWithHandleInputInfo(root, inputList);
            }

            var meta = new CodeBehindMetadata();
            meta.JsonPropertyMapList = mapList;
            meta.RootClassInfo.Namespace = ns;
            meta.RootClassInfo.GenericArg = genericArg;
            meta.RootClassInfo.AutoBindToDataObject = autoBindToDataObject;
            meta.RootClassInfo.InputBindingList = inputList;
            return meta;
        }

        private static bool IsTypedJsonClass(ClassDeclarationSyntax classDecl) {
            SimpleNameSyntax simpleName;
            if (classDecl == null || classDecl.BaseList == null)
                return false;

            foreach (var ts in classDecl.BaseList.Types) {
                simpleName = ts as SimpleNameSyntax;
                if (simpleName != null && simpleName.Identifier.ValueText == "Json")
                    return true;
            }
            return false;            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="classDecl"></param>
        /// <param name="jsonInstanceName"></param>
        /// <param name="genericArgument"></param>
        /// <returns></returns>
        private static bool IsBoundToEntity(ClassDeclarationSyntax classDecl, string jsonInstanceName, out string genericArgument) {
            GenericNameSyntax gns;
            IdentifierNameSyntax ins;
            SeparatedSyntaxList<TypeSyntax> baseTypes = classDecl.BaseList.Types;

            foreach (var ts in baseTypes){
                if (ts.Kind == SyntaxKind.GenericName){
                    gns = (GenericNameSyntax)ts;
                    if (jsonInstanceName.Equals(gns.Identifier.ValueText)) {
                        if (gns.TypeArgumentList.Arguments.Count == 1) {
                            ins = (IdentifierNameSyntax)gns.TypeArgumentList.Arguments[0];
                            genericArgument = ins.Identifier.ValueText;
                            return true;
                        }
                    }
                }
            }

            genericArgument = null;
            return false;
        }

        /// <summary>
        /// Gets the namespace for the class with the specified shortname.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="root">The root.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.Exception">No class with name </exception>
        private static string GetNamespaceForClass(string className, SyntaxNode root) {
            ClassDeclarationSyntax cd = FindClassDeclarationFor(className, root);

            if (cd == null)
                throw new Exception("No class with name " + className + " found in code.");

            return FindNamespaceForClassDeclaration(cd);
        }

        /// <summary>
        /// Finds the namespace for class declaration.
        /// </summary>
        /// <param name="cd">The cd.</param>
        /// <returns>String.</returns>
        private static String FindNamespaceForClassDeclaration(ClassDeclarationSyntax cd) {
            StringBuilder nsBuilder;
            NamespaceDeclarationSyntax ns;

            nsBuilder = new StringBuilder();
            ns = cd.FirstAncestorOrSelf<NamespaceDeclarationSyntax>(_ => { return true; });
            while (ns != null) {
                nsBuilder.Insert(0, ns.Name.GetText());

                if (ns.Parent != null)
                    ns = ns.Parent.FirstAncestorOrSelf<NamespaceDeclarationSyntax>(_ => { return true; });
                else
                    ns = null;

                if (ns != null)
                    nsBuilder.Insert(0, '.');
            }
            return nsBuilder.ToString().TrimEnd();
        }

        /// <summary>
        /// Finds the Roslyn ClassDeclatation node for a class with the specified shortname.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="current">The current.</param>
        /// <returns>ClassDeclarationSyntax.</returns>
        private static ClassDeclarationSyntax FindClassDeclarationFor(string className, SyntaxNode current) {
            ClassDeclarationSyntax cd;
            ClassDeclarationSyntax ret = null;

            if (current.Kind == SyntaxKind.ClassDeclaration) {
                cd = (ClassDeclarationSyntax)current;
                if (cd.Identifier != null && cd.Identifier.ValueText.Equals(className))
                    return cd;
            }

            foreach (SyntaxNode child in current.ChildNodes()) {
                ret = FindClassDeclarationFor(className, child);
                if (ret != null)
                    break;
            }
            return ret;
        }

        /// <summary>
        /// Searches through the syntaxtree and adds all found info where an method
        /// that ís called 'Handle' with one parameter of type Input is declared.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="list">The list.</param>
        private static void FillListWithHandleInputInfo(SyntaxNode node, List<InputBindingInfo> list) {
            MethodDeclarationSyntax methodDecl;

            if (node.Kind == SyntaxKind.MethodDeclaration) {
                methodDecl = (MethodDeclarationSyntax)node;
                if (methodDecl.Identifier.ValueText.Equals("Handle", StringComparison.CurrentCultureIgnoreCase)) {
                    list.Add(GetHandleInputInfoFrom(methodDecl));
                    return;
                }
            }

            foreach (SyntaxNode child in node.ChildNodes()) {
                FillListWithHandleInputInfo(child, list);
            }
        }

        /// <summary>
        /// Creates a HandleInputInfo object and sets the fields with values from
        /// the specified MethodDeclaration node.
        /// </summary>
        /// <param name="methodNode">The method node.</param>
        /// <returns>InputBindingInfo.</returns>
        /// <exception cref="System.Exception">No return values are allowed in an app Handle method.</exception>
        private static InputBindingInfo GetHandleInputInfoFrom(MethodDeclarationSyntax methodNode) {
            ClassDeclarationSyntax classDecl;
            String className;
            String classNs;
            String paramType;

            String returnType = methodNode.ReturnType.ToString();
            if (!"void".Equals(returnType))
                throw new Exception("No return values are allowed in an app Handle method.");

            paramType = null;
            foreach (ParameterSyntax par in methodNode.ParameterList.ChildNodes()) {
                if (paramType != null)
                    throw new Exception("Only one parameter is allowed on an json Handle method.");
                paramType = par.Type.ToString();
            }

            classDecl = FindClass(methodNode.Parent);
            className = classDecl.Identifier.ValueText;
            classNs = FindNamespaceForClassDeclaration(classDecl);

            return new InputBindingInfo() {
                DeclaringClassNamespace = classNs,
                DeclaringClassName = className,
                FullInputTypeName = paramType
            };
        }

        /// <summary>
        /// Searches through the syntaxtree and adds all found info where an attribute
        /// that starts with the classname is found.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="node">The node.</param>
        /// <param name="jsonInstanceName"></param>
        /// <param name="list">The list.</param>
        private static void FillListWithJsonMapInfo(String className, SyntaxNode node, string jsonInstanceName, List<CodeBehindClassInfo> list) {
            AttributeSyntax attribute;

            if (node.Kind == SyntaxKind.Attribute) {
                attribute = (AttributeSyntax)node;
                if (IsJsonMapAttribute(attribute)) {
                    list.Add(GetJsonMapInfoFrom(attribute, jsonInstanceName));
                    return;
                }
            }

            foreach (SyntaxNode child in node.ChildNodes()) {
                FillListWithJsonMapInfo(className, child, jsonInstanceName, list);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="attributeNode"></param>
        /// <param name="jsonInstanceName"></param>
        /// <returns></returns>
        private static CodeBehindClassInfo GetJsonMapInfoFrom(AttributeSyntax attributeNode, string jsonInstanceName) {
            bool autoBindToDataObject;
            ClassDeclarationSyntax classDecl;
            ClassDeclarationSyntax parentClassDecl;
            List<string> parentClassNames = new List<string>();
            string genericArg;

            // Find the class the attribute was declared on.
            classDecl = FindClass(attributeNode.Parent);
            autoBindToDataObject = IsBoundToEntity(classDecl, jsonInstanceName, out genericArg);
            
            // If the class is an inner class we need to get the full name of all classes
            // to be able to connect the generated code in the same structure as the 
            // codebehind.
            parentClassDecl = FindClass(classDecl.Parent);
            while (parentClassDecl != null) {
                parentClassNames.Add(parentClassDecl.Identifier.ValueText);
                parentClassDecl = FindClass(parentClassDecl.Parent);
            }

            var jmi = CodeBehindClassInfo.EvaluateAttributeString(attributeNode.Name.ToString());
            jmi.Namespace = FindNamespaceForClassDeclaration(classDecl);
            jmi.ClassName = classDecl.Identifier.ValueText;
            jmi.GenericArg = genericArg;
            jmi.AutoBindToDataObject = autoBindToDataObject;
            jmi.ParentClasses = parentClassNames;
            return jmi;
        }

        /// <summary>
        /// Checks if the specified attribute node is an json map attribute. I.e
        /// starts with the classname.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        private static Boolean IsJsonMapAttribute(AttributeSyntax attribute) {
            String attributeName = attribute.Name.ToString();

            if (attributeName.StartsWith("json."))
                return true;
            return false;
        }

        /// <summary>
        /// Finds the class.
        /// </summary>
        /// <param name="fromNode">From node.</param>
        /// <returns>ClassDeclarationSyntax.</returns>
        private static ClassDeclarationSyntax FindClass(SyntaxNode fromNode) {
            return fromNode.FirstAncestorOrSelf<ClassDeclarationSyntax>(_ => { return true; });
        }
    }
}
