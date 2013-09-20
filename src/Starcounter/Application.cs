﻿
using System;
using System.Reflection;

namespace Starcounter {

    /// <summary>
    /// Represents a Starcounter application, executing in or configured
    /// to run in, a Starcounter code host.
    /// </summary>
    public sealed class Application {
        [ThreadStatic]
        internal static Application CurrentAssigned;

        /// <summary>
        /// Gets the current application, running in the current Starcounter
        /// code host.
        /// </summary>
        /// <remarks>
        /// The current application is determined by context where this property
        /// is read. The code host has several ways to figure out the which is
        /// the current application, based on the calling code.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the application can't
        /// be resolved based on the given context.</exception>
        public static Application Current {
            get {
                var current = CurrentAssigned;
                current = current ?? GetApplication(Assembly.GetCallingAssembly());
                return current;
            }
        }

        /// <summary>
        /// Gets the <see cref="Application"/> the given <paramref name="assembly"/>
        /// represent or is part of.
        /// </summary>
        /// <param name="assembly">The assembly whose <see cref="Application"/> are
        /// being requested.</param>
        /// <returns>The application <paramref name="assembly"/> represent or runs
        /// as a part of.</returns>
        /// <exception cref="AgrumentNullException">Thrown when <paramref name="assembly"/>
        /// is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the application can't
        /// be resolved based on the given assembly.</exception>
        public static Application GetApplication(Assembly assembly) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the <see cref="Application"/> the given <paramref name="fileName"/>
        /// represent. Both short names, such as "foo.exe", and full names, such as
        /// "\path\to\foo.exe", will be considered.
        /// </summary>
        /// <param name="fileName">The file name whose <see cref="Application"/> are
        /// being requested.</param>
        /// <returns>The <see cref="Application"/> launched by the given file.
        /// <exception cref="AgrumentNullException">Thrown when <paramref name="fileName"/>
        /// is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when the application can't
        /// be resolved based on the given file.</exception>
        public static Application GetApplication(string fileName) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the name, including the full path, of the applications
        /// primary file.
        /// </summary>
        /// <remarks>
        /// An application can be launched by several types of files,
        /// including executables (.exe), source code (e.g. .cs) and
        /// libraries (.dll) and future, yet not known files, on a variety
        /// of host systems. This property is designed to return the full
        /// file name, including the path, of the file responsible for
        /// starting the current application.
        /// </remarks>
        public string FileName { get; internal set; }
        
        /// <summary>
        /// Gets the full path of the file actually loaded in the code
        /// host.
        /// </summary>
        public string LoadPath { get; internal set; }

        /// <summary>
        /// Gets the logical working directory of the current <see cref="Application"/>.
        /// </summary>
        public string WorkingDirectory { get; internal set; }
        
        /// <summary>
        /// Gets the arguments with which the current <see cref="Application"/>
        /// was started. These are the arguments passed to a possible entrypoint,
        /// semantically comparable to <see cref="Environment.CommandLine"/>.
        /// </summary>
        public string[] Arguments { get; internal set; }
    }
}
