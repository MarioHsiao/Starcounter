using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Roslyn.Compilers.CSharp;

namespace Starcounter.Internal.Application.CodeGeneration
{
    public class JsonMapInfo
    {
        public String Namespace;
        public String ClassName;
        public List<String> ParentClasses;
        public String JsonMapName;
    }

    public class CodeBehindMetadata
    {
        public static readonly CodeBehindMetadata Empty 
            = new CodeBehindMetadata("", new List<JsonMapInfo>());

        public readonly String RootNamespace;
        public readonly List<JsonMapInfo> JsonPropertyMapList;

        public CodeBehindMetadata(String ns, List<JsonMapInfo> list)
        {
            RootNamespace = ns;
            JsonPropertyMapList = list;
        }
    }

    public static class RoslynParserHelper
    {
        public static CodeBehindMetadata GetCodeBehindMetadata(string className,
                                                               string codeBehindFilename)
        {
            SyntaxNode root;
            SyntaxTree tree;   
            String ns;
            List<JsonMapInfo> mapList = new List<JsonMapInfo>();

            if (!File.Exists(codeBehindFilename)) return new CodeBehindMetadata(null, mapList);

            tree = SyntaxTree.ParseFile(codeBehindFilename);
            root = tree.GetRoot();

            ns = GetNamespaceForClass(className, root);
            FillListWithJsonMapInfo(className, root, mapList);

            return new CodeBehindMetadata(ns, mapList);
        }
        
        private static string GetNamespaceForClass(string className, SyntaxNode root)
        {
            ClassDeclarationSyntax cd = FindClassDeclarationFor(className, root);

            if (cd == null)
                throw new Exception("No class with name " + className + " found in code.");

            return FindNamespaceForClassDeclaration(cd);
        }

        private static String FindNamespaceForClassDeclaration(ClassDeclarationSyntax cd)
        {
            StringBuilder nsBuilder;
            NamespaceDeclarationSyntax ns;

            nsBuilder = new StringBuilder();
            ns = cd.FirstAncestorOrSelf<NamespaceDeclarationSyntax>(_ => { return true; });
            while (ns != null)
            {
                nsBuilder.Insert(0, ns.Name.GetText());

                if (ns.Parent != null)
                    ns = ns.Parent.FirstAncestorOrSelf<NamespaceDeclarationSyntax>(_ => { return true; });
                else
                    ns = null;

                if (ns != null) nsBuilder.Insert(0, '.');
            }
            return nsBuilder.ToString();
        }

        private static ClassDeclarationSyntax FindClassDeclarationFor(string className, SyntaxNode current)
        {
            ClassDeclarationSyntax cd;
            ClassDeclarationSyntax ret = null;

            if (current.Kind == SyntaxKind.ClassDeclaration)
            {
                cd = (ClassDeclarationSyntax)current;
                if (cd.Identifier != null && cd.Identifier.ValueText.Equals(className))
                    return cd;
            }

            foreach (SyntaxNode child in current.ChildNodes())
            {
                ret = FindClassDeclarationFor(className, child);
                if (ret != null) break;
            }
            return ret;
        }

        private static void FillListWithJsonMapInfo(String className, SyntaxNode node, List<JsonMapInfo> list)
        {
            AttributeSyntax attribute;
            
            if (node.Kind == SyntaxKind.Attribute)
            {
                attribute = (AttributeSyntax)node;
                if (IsJsonMapAttribute(attribute, className))
                {
                    list.Add(GetInfoFrom(attribute));
                    return;
                }
            }

            foreach (SyntaxNode child in node.ChildNodes())
            {
                FillListWithJsonMapInfo(className, child, list);
            }
        }

        private static JsonMapInfo GetInfoFrom(AttributeSyntax attributeNode)
        {
            ClassDeclarationSyntax classDecl;
            ClassDeclarationSyntax parentClassDecl;
            JsonMapInfo info = new JsonMapInfo();
            List<String> parentClassNames = new List<String>();

            info.JsonMapName = attributeNode.Name.ToString();
            
            // Find the class the attribute was declared on.
            classDecl = FindClass(attributeNode.Parent);

            // If the class is an inner class we need to get the full name of all classes
            // to be able to connect the generated code in the same structure as the 
            // codebehind.
            parentClassDecl = FindClass(classDecl.Parent);
            while (parentClassDecl != null)
            {
                parentClassNames.Add(parentClassDecl.Identifier.ValueText);
                parentClassDecl = FindClass(parentClassDecl.Parent);
            }

            info.Namespace = FindNamespaceForClassDeclaration(classDecl);
            info.ClassName = classDecl.Identifier.ValueText;
            info.ParentClasses = parentClassNames;

            return info;
        }

        private static Boolean IsJsonMapAttribute(AttributeSyntax attribute, String className)
        {
            String attributeName = attribute.Name.ToString();

            if (attributeName.StartsWith(className)) return true;
            return false;
        }

        private static ClassDeclarationSyntax FindClass(SyntaxNode fromNode)
        {
            return fromNode.FirstAncestorOrSelf<ClassDeclarationSyntax>(_ => { return true; });
        }
    }
}
