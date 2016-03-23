﻿using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Starcounter.XSON.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.XSON.PartialClassGenerator {

    internal sealed class CodeBehindFileAnalyzer : CSharpSyntaxWalker {
        public readonly RoslynCodeBehindParser Parser;
        public readonly CodeBehindMetadata Result;

        public CodeBehindFileAnalyzer(RoslynCodeBehindParser parser) {
            Parser = parser;
            Result = new CodeBehindMetadata();
        }

        public override void VisitUsingDirective(UsingDirectiveSyntax node) {
            // Capture only root/file level usings
            if (node.Parent != null && node.Parent.Kind() == SyntaxKind.CompilationUnit) {
                Result.UsingDirectives.Add(node.ToString());
            }
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node) {
            // Top level class. Pass it on.
            var classAnalysis = new ClassAnalyzer(this, node);
            classAnalysis.Visit(node);
        }
    }
}