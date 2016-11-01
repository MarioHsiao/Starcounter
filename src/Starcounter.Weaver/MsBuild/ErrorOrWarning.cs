namespace Starcounter.Weaver.MsBuild
{
    internal class ErrorOrWarning
    {
        public uint ErrorCode;
        public string Message;

        public bool IsWarning {
            get {
                return ErrorCode == 0;
            }
        }

        public string Serialize()
        {
            return $"{ErrorCode}:{Message}";
        }

        public static ErrorOrWarning Deserialize(string serialized)
        {
            var e = new ErrorOrWarning();
            var index = serialized.IndexOf(":");
            e.ErrorCode = uint.Parse(serialized.Substring(0, index));
            e.Message = serialized.Substring(index + 1);
            return e;
        }
    }
}
