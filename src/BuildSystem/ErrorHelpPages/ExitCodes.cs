
namespace ErrorHelpPages {
    /// <summary>
    /// Defines the exit codes the program use.
    /// </summary>
    internal static class ExitCodes {
        public const int WrongArguments = 1;
        public const int ErrorCodeFileNotFound = 2;
        public const int GitExecutableNotFound = 3;
        public const int GitUnexpectedExit = 4;
        public const int LocalRepoDirNotFound = 5;
        public const int TemplateFileNotFound = 6;
        public const int TemplateBadFormat = 7;
    }
}
