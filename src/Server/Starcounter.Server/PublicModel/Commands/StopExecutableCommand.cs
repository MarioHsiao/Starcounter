
namespace Starcounter.Server.PublicModel.Commands {
    /// <summary>
    /// A command representing the request to stop an executable.
    /// </summary>
    public sealed class StopExecutableCommand : DatabaseCommand {
        /// <summary>
        /// The executable to be stopped.
        /// </summary>
        public readonly string Executable;

        /// <summary>
        /// Initializes a new <see cref="StopExecutableCommand"/>.
        /// </summary>
        /// <param name="engine">The server engine where the command will execute.</param>
        /// <param name="database">The name of the database the executable runs in.</param>
        /// <param name="executable">The identity of the executable.</param>
        public StopExecutableCommand(ServerEngine engine, string database, string executable)
            : base(engine, null, "Stopping {0}", executable) {
            this.Executable = executable;
            this.DatabaseUri = ScUri.MakeDatabaseUri(ScUri.GetMachineName(), this.Engine.Name, database);
        }
    }
}