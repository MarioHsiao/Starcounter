using System;
using System.IO;

namespace Starcounter.Internal
{
    /// <summary>
    /// Guard utility methods, usable to guard any public API for correct input
    /// with a minimum of fuzz.
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Guard the given object is not null.
        /// </summary>
        /// <param name="value">The object to guard.</param>
        /// <param name="parameterName">The name of the parameter that reference
        /// the given object.</param>
        public static void NotNull(object value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName, $"Parameter {parameterName} can not be null.");
            }
        }

        /// <summary>
        /// Guard the given <c>string</c> is not null or empty.
        /// </summary>
        /// <param name="value">The object to guard.</param>
        /// <param name="parameterName">The name of the parameter that reference
        /// the given object.</param>
        public static void NotNullOrEmpty(string value, string parameterName)
        {
            Guard.NotNull(value, parameterName);

            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(parameterName, $"Parameter {parameterName} can not be empty.");
            }
        }

        /// <summary>
        /// Guard that <c>directory</c> is not null, not empty and contains the path
        /// to an existing directory.
        /// </summary>
        /// <param name="directory">The directory to verify.</param>
        /// <param name="parameterName">The name of the parameter that reference
        /// the given directory.</param>
        public static void DirectoryExists(string directory, string parameterName)
        {
            Guard.NotNullOrEmpty(directory, parameterName);
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Directory {directory}, given by {parameterName}, does not exist.");
            }
        }

        /// <summary>
        /// Guard that <c>file</c> is not null, not empty and contains the path
        /// to an existing file.
        /// </summary>
        /// <param name="file">The file to verify.</param>
        /// <param name="parameterName">The name of the parameter that reference
        /// the given file.</param>
        public static void FileExists(string file, string parameterName)
        {
            Guard.NotNullOrEmpty(file, parameterName);
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"File {file}, given by {parameterName}, does not exist.", file);
            }
        }

        /// <summary>
        /// Guard that <c>file</c> is not null, not empty and contains the path
        /// to an existing file. If the file is path-rooted, it need to exist. If
        /// it's relative, it need to exist in the given directory.
        /// </summary>
        /// <param name="file">The file to verify.</param>
        /// <param name="directory">The directory the file could be stored in.</param>
        /// <param name="parameterName">The name of the parameter that reference
        /// the given file.</param>
        public static void FileExistsInDirectory(string file, string directory, string parameterName)
        {
            Guard.DirectoryExists(directory, parameterName);
            Guard.NotNullOrEmpty(file, parameterName);

            if (Path.IsPathRooted(file))
            {
                if (File.Exists(file))
                {
                    return;
                }
            }

            var full = Path.Combine(directory, file);
            if (!File.Exists(full))
            {
                throw new FileNotFoundException($"File {file} in directory {directory}, given by {parameterName}, does not exist.", file);
            }
        }

        /// <summary>
        /// Guard that <c>candidate</c> and <c>constraint</c> is not null and that
        /// <c>constraint</c> is assignable from <c>candidate</c>.
        /// </summary>
        /// <param name="candidate">The candidate to guard.</param>
        /// <param name="constraint">The constraint to guard for.</param>
        /// <param name="parameterName">Name of the parameter <c>candidate</c> is
        /// referenced from.</param>
        public static void IsAssignableFrom(Type candidate, Type constraint, string parameterName)
        {
            Guard.NotNull(candidate, parameterName);
            Guard.NotNull(constraint, parameterName);

            if (!constraint.IsAssignableFrom(candidate))
            {
                throw new ArgumentException($"Type {constraint.Name} is not assignable from {candidate.Name}, specified by {parameterName}.", parameterName);
            }
        }

        /// <summary>
        /// Guard <c>candidate</c> is not null and is not abstract.
        /// </summary>
        /// <param name="candidate">The candidate to validate.</param>
        /// <param name="parameterName">Name of parameter referencing the candidate.</param>
        public static void IsNotAbstract(Type candidate, string parameterName)
        {
            Guard.NotNull(candidate, parameterName);

            if (candidate.IsAbstract)
            {
                throw new ArgumentException($"Type {candidate.Name}, specified by {parameterName}, can not be abstract.", parameterName);
            }
        }

        /// <summary>
        /// Guard <c>candidate</c> is not null and has a public default constructor.
        /// </summary>
        /// <param name="candidate">The candidate to validate.</param>
        /// <param name="parameterName">Name of parameter referencing the candidate.</param>
        public static void HasPublicDefaultConstructor(Type candidate, string parameterName)
        {
            Guard.NotNull(candidate, parameterName);

            var defaultCtor = candidate.GetConstructor(Type.EmptyTypes);
            if (defaultCtor == null)
            {
                throw new ArgumentException($"Type {candidate.Name}, specified by {parameterName}, does not have a public default constructor.", parameterName);
            }
        }
    }
}
