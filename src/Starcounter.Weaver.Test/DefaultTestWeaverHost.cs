namespace Starcounter.Weaver.Test
{
    /// <summary>
    /// Test host, just implementing empty stubs. Subclass if we need to
    /// act on weaver output.
    /// </summary>
    internal class DefaultTestWeaverHost : IWeaverHost
    {
        public virtual void WriteDebug(string message, params object[] parameters)
        {
        }

        public virtual void WriteError(uint code, string message, params object[] parameters)
        {
        }

        public virtual void WriteInformation(string message, params object[] parameters)
        {
        }

        public virtual void WriteWarning(string message, params object[] parameters)
        {
        }
    }
}