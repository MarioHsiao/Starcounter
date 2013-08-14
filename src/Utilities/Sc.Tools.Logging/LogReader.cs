
using System;
using System.IO;
using System.Text;

namespace Sc.Tools.Logging {

    internal interface ILogReader {

        void Open(string directoryPath, int bufferSize);
		void Open(string directoryPath, int bufferSize, int startFileNumber, long startFilePosition);
        void Close();
        LogEntry Next();
    }

    internal abstract class LogReaderImpl {

        static string[] SEVERITY_STRING_TABLE = new string[] {
            "Debug",
            "SuccessAudit",
            "FailureAudit",
            "Notice",
            "Warning",
            "Error",
            "Critical"
            };
        
        static Severity[] SEVERITY_CODE_TABLE = new Severity[] {
            Severity.Debug,
            Severity.SuccessAudit,
            Severity.FailureAudit,
            Severity.Notice,
            Severity.Warning,
            Severity.Error,
            Severity.Critical
            };

        protected internal bool IsIgnoredChar(int c) {
            if (c == (byte)'\r') return true;
            return false;
        }

        protected internal DateTime ParseDateTime(string s) {
            try {
                return DateTime.ParseExact(s, "yyyyMMddTHHmmss", System.Globalization.CultureInfo.CurrentCulture);
            }
            catch {
                throw new ArgumentException();
            }
        }

        protected internal Severity ParseSeverity(string s) {
            var st = SEVERITY_STRING_TABLE;
            var ct = SEVERITY_CODE_TABLE;
            for (var i = 0; i < st.Length; i++) {
                if (s.Equals(st[i], StringComparison.OrdinalIgnoreCase)) {
                    return ct[i];
                }
            }
            throw new ArgumentException();
        }

        protected internal int ParseErrorCode(string s) {
            try {
                if (s != "-") return int.Parse(s);
                else return 0;
            }
            catch {
                throw new ArgumentException();
            }
        }
    }

    internal class LogReaderReverse : LogReaderImpl, ILogReader {

        internal const String FILE_NAME_FILTER = "starcounter.??????????.log";

        private static Int32 CompareLogFileInfosDesc(FileInfo a, FileInfo b)
        {
            return -String.CompareOrdinal(a.Name, b.Name);
        }

        FileInfo[] files;
        int nextFile;
        
        FileStream f;
        long filePos;

        byte[] buffer;
        int bufferPos;
        
        byte[] local = new byte[1024];

		long initialFilePosition;

        public void Open(string directoryPath, int bufferSize) {
			Open(directoryPath, bufferSize, 0, -1);
        }

		public void Open(string directoryPath, int bufferSize, int startFileNumber, long startFilePosition) {
			var directory = new DirectoryInfo(directoryPath);
			files = directory.GetFiles(FILE_NAME_FILTER);
			Array.Sort<FileInfo>(files, new Comparison<FileInfo>(CompareLogFileInfosDesc));
			nextFile = startFileNumber;
			initialFilePosition = startFilePosition;

			f = null;
			filePos = 0;

			buffer = new byte[bufferSize];
			bufferPos = -1;
		}

        public void Close() {
            if (f != null) {
                f.Close(); f = null;
            }
            buffer = null;
        }

        public LogEntry Next() {
        start:
            // Find end of last complete entry.

            for (; ; ) {
                var b = ReadByte();
                if (b != 10) {
                    if (b < 0) return null;
                }
                else break;
            }

            // Load message into temp buffer until beginning of message is
            // located.

            var localPos = local.Length;

            for (; ; ) {
                var b = PeekByte();
                if (b != 10 && b >= 0) {
                    StepByte();

                    if (!IsIgnoredChar(b)) {

                        if (PeekByte() == '\\')
                        {
                            switch (b)
                            {
                            case 'n':
                                b = '\n';
                                StepByte();
                                break;
                            case 'r':
                                b = '\r';
                                StepByte();
                                break;
                            case 't':
                                b = '\t';
                                StepByte();
                                break;
                            case '\\':
                                StepByte();
                                break;
                            };
                        }

                        if (localPos == 0) {
                            var newLocal = new byte[local.Length * 2];
                            Buffer.BlockCopy(local, 0, newLocal, local.Length, local.Length);
                            localPos = local.Length;
                            local = newLocal;
                            newLocal = null;
                        }

                        local[--localPos] = (byte)b;
                    }
                }
                else break;
            }

            var fields = new string[5];
            var fieldIndex = 0;
            string message = null;

            for (var offset = localPos; localPos < local.Length; localPos++) {
                if (local[localPos] == 32) {
                    fields[fieldIndex++] = Encoding.UTF8.GetString(local, offset, localPos - offset);
                    
                    localPos++;

                    if (fieldIndex == fields.Length) {
                        if (localPos < local.Length) {
                            message = Encoding.UTF8.GetString(local, localPos, local.Length - localPos);
                        }
                        break;
                    }
                    else offset = localPos;
                }
            }

            if (message == null) goto start; // Incomplete or badly formatted record.

            try {
                return new LogEntry(
                    ParseDateTime(fields[0]),
                    ParseSeverity(fields[1]),
                    fields[2],
                    fields[3],
                    ParseErrorCode(fields[4]),
                    message
                    );
            }
            catch (ArgumentException) {
                goto start; // Badly formatted record.
            }
        }

        private int PeekByte() {
            if (bufferPos >= 0) return buffer[bufferPos];

            while (filePos == 0) {
                if (nextFile == files.Length) return -1;

                f = new FileStream(
                    files[nextFile++].FullName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                    );
				if (initialFilePosition == -1) {
					filePos = f.Length;
				} else {
					filePos = initialFilePosition;
					initialFilePosition = -1;
				}
            }

            var oldFilePos = filePos;
            filePos -= buffer.Length;
            if (filePos < 0) filePos = 0;
            int bytesToRead = (int)(oldFilePos - filePos);

            f.Position = filePos;
            f.Read(buffer, 0, bytesToRead);
            bufferPos = bytesToRead - 1;

            if (filePos == 0) {
                f.Close(); f = null;
            }

            return buffer[bufferPos];
        }

        private void StepByte() {
            bufferPos--;
        }

        private int ReadByte() {
            var b = PeekByte();
            if (b >= 0) StepByte();
            return b;
        }
    }

    public class LogReader {

        ILogReader inner;

        public void Open(string directoryPath, ReadDirection direction, int bufferSize) {
			Open(directoryPath, direction, bufferSize, 0, -1);
        }

		public void Open(string directoryPath, 
						 ReadDirection direction, 
						 int bufferSize, 
						 int startFileNumber, 
						 long startFilePosition) {
			inner = new LogReaderReverse();
			inner.Open(directoryPath, bufferSize, startFileNumber, startFilePosition);
		}

        public void Close() {
            inner.Close();
            inner = null;
        }

        public LogEntry Next() {
            return inner.Next();
        }
    }
}
