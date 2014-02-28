using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.CSharp;
using Starcounter.XSON.Metadata;

namespace Starcounter.XSON.Compiler.Mono {
    internal class MonoCSharpEnumerator {
        private string filePathNote;
        private string Code;
        private Tokenizer tokenizer;
        private CSharpToken currentToken;
        private CSharpToken previousToken;
        private Stack<string> nsStack;
        private Stack<string> classStack;
        private Stack<CSharpToken> tokenStack;

        internal MonoCSharpEnumerator(string codebehind, string filePathNote) {
            this.filePathNote = filePathNote;
            this.Code = codebehind;
            currentToken = CSharpToken.UNDEFINED;
            previousToken = CSharpToken.UNDEFINED;
            nsStack = new Stack<string>();
            classStack = new Stack<string>();
            tokenStack = new Stack<CSharpToken>();

            CreateMonoTokenizer();
        }

        internal bool MoveNext() {
            previousToken = currentToken;
            currentToken = (CSharpToken)tokenizer.token();
            if (currentToken == CSharpToken.EOF)
                currentToken = CSharpToken.UNDEFINED;
            return (currentToken != CSharpToken.UNDEFINED);
        }

        internal string Value {
            get {
                var locatedToken = tokenizer.Value as Tokenizer.LocatedToken;
                if (locatedToken != null)
                    return locatedToken.Value;

                return null;
            }
        }

        internal CSharpToken Token {
            get {
                return currentToken;
            }
        }

        internal CSharpToken PreviousToken {
            get {
                return previousToken;
            }
        }

        internal CSharpToken Peek() {
            return (CSharpToken)tokenizer.peek_token();
        }

        internal CodeBehindClassInfo LastFoundJsonAttribute { get; set; }

        internal string CurrentNamespace {
            get {
                string ns = null;
                foreach (string partialNs in nsStack) {
                    if (ns == null)
                        ns = partialNs;
                    else
                        ns += '.' + partialNs;
                }
                return ns;
            }
        }

        internal string CurrentClass {
            get {
                if (classStack.Count > 0)
                    return classStack.Peek();
                return null;
            }
        }

        internal string FullClassName {
            get {
                string fullClass = CurrentNamespace;
                foreach (string partialClass in classStack) {
                    if (fullClass == null)
                        fullClass = partialClass;
                    else
                        fullClass += '.' + partialClass;
                }
                return fullClass;
            }
        }

        internal List<string> ClassList {
            get {
                return classStack.ToList<string>();
            }
        }

        internal void PushClass(string cn) {
            tokenStack.Push(CSharpToken.CLASS);
            classStack.Push(cn);
        }

        internal void PushNamespace(string ns) {
            tokenStack.Push(CSharpToken.NAMESPACE);
            nsStack.Push(ns);
        }

        internal void PushBlock() {
            tokenStack.Push(CSharpToken.UNDEFINED);
        }

        internal void PopBlock() {
            CSharpToken token = tokenStack.Pop();
            if (token == CSharpToken.NAMESPACE)
                nsStack.Pop();
            else if (token == CSharpToken.CLASS)
                classStack.Pop();
        }

        private void CreateMonoTokenizer() {
            List<SourceFile> sfList = new List<SourceFile>();
            sfList.Add(new SourceFile(Path.GetFileNameWithoutExtension(filePathNote), filePathNote, 0));
            Location.Initialize(sfList);

            CompilerSettings settings = new CompilerSettings();
            CompilerContext ctx = new CompilerContext(settings, new ConsoleReportPrinter());
            ModuleContainer module = new ModuleContainer(ctx);
            CompilationSourceFile file = new CompilationSourceFile(module, null);

//            string code = File.ReadAllText(filePath);
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(Code));
            SeekableStreamReader seekable = new SeekableStreamReader(stream, Encoding.UTF8, null);
            tokenizer = new Tokenizer(seekable, file, new ParserSession());
            tokenizer.PropertyParsing = false;
            tokenizer.TypeOfParsing = false;
            tokenizer.EventParsing = false;
        }
    }
}
