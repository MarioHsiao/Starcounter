
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
        public const int NotAGitDirectory = 8;
        // A few conditions/codes that is inherited from
        // the older WikiErrorCodes program. Not sure why
        // they all have to be set just to run, but let's
        // leave it as this for now.
        public const int AlreadyRunning = 0;
        public const int UpdateFlagNotSet = 0;
        public const int NotAReleaseBuild = 0;
        public const int PersonalBuild = 0;
    }
}
