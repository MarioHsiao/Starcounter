
namespace Starcounter.Weaver
{
    /// <summary>
    /// Abstraction of the weaver runtime, possible to execute
    /// using <c>Execute()</c>.
    /// </summary>
    public interface IWeaver
    {
        /// <summary>
        /// Gets the setup used to create the weaver.
        /// </summary>
        WeaverSetup Setup { get; }

        /// <summary>
        /// Executes the weaver.
        /// </summary>
        void Execute();
    }
}