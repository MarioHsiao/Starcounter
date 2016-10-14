using System.Reflection;

namespace Starcounter.Hosting {
    /// <summary>
    /// Represents the collection of private assemblies the code host is
    /// aware of, based on loaded applications.
    /// </summary>
    internal interface IPrivateAssemblyStore {
        /// <summary>
        /// Evaluates the given <paramref name="applicationDirectory"/> to see
        /// if it is a directory previously registered with the current store.
        /// </summary>
        /// <param name="applicationDirectory">The directory to look for.</param>
        /// <returns><c>true</c> if the given directory is a known application
        /// directory; <c>false</c> otherwise.</returns>
        bool IsApplicationDirectory(string applicationDirectory);

        /// <summary>
        /// Gets the name of the assembly stored for the specified path.
        /// </summary>
        /// <param name="filePath">That path to translate to a name.</param>
        /// <returns>The assembly name</returns>
        AssemblyName GetAssembly(string filePath);

        /// <summary>
        /// Get all assemblies matching the given name, accross our ingoing
        /// application directories.
        /// </summary>
        /// <param name="assemblyName">The name to consult</param>
        /// <returns>Each assembly corresponding to the given name.</returns>
        PrivateBinaryFile[] GetAssemblies(string assemblyName);
    }
}
