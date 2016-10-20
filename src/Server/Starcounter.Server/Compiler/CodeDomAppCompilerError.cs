
using System.CodeDom.Compiler;

namespace Starcounter.Server.Compiler
{
    /// <summary>
    /// Exposed a CodeDom based <c>CompilerError</c> as an
    /// <c>IAppCompilerSourceError</c>.
    /// </summary>
    internal sealed class CodeDomAppCompilerError : IAppCompilerSourceError
    {
        readonly CompilerError inner;

        public CodeDomAppCompilerError(CompilerError error)
        {
            inner = error;
        }

        int IAppCompilerSourceError.Column {
            get {
                return inner.Column;
            }
        }

        string IAppCompilerSourceError.Description {
            get {
                return inner.ErrorText;
            }
        }

        string IAppCompilerSourceError.File {
            get {
                return inner.FileName;
            }
        }

        string IAppCompilerSourceError.Id {
            get {
                return inner.ErrorNumber;
            }
        }
    }
}
