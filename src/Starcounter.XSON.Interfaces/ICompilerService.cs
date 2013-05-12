using System;

namespace Starcounter.Templates.Interfaces {
    public interface ICompilerService {
        object GenerateJsonSerializer(string code, string typeName);
        object AnalyzeCodeBehind(string className, string codeBehindFile);
    }
}
