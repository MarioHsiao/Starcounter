using System;

namespace Starcounter.Templates.Interfaces {
    public interface ICompilerService {
        object Compile(string code);
        object AnalyzeCodeBehind(string className, string codeBehindFile);
    }
}
