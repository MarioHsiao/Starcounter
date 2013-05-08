using System;

namespace Starcounter.Templates.Interfaces {
    public interface ICompilerService {
        object Compile(string code);
        object Compile(string code, params string[] assemblyRefs);
        object AnalyzeCodeBehind(string className, string codeBehindFile);
    }
}
