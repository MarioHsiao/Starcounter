
using System;
using System.Text.RegularExpressions;

namespace Starcounter.Internal {

    /// <summary>
    /// Encapsulates the properties of a location in a file.
    /// </summary>
    public class FileLocation {
        /// <summary>
        /// Represents an unknown file location.
        /// </summary>
        public static readonly FileLocation Unknown = new FileLocation();

        /// <summary>
        /// Gets a <see cref="Regex"/> pattern that can be used to parse
        /// file location information from strings. The groups returned in
        /// a match will be named "file", "line" and "column".
        /// </summary>
        /// <remarks>
        /// The pattern restricts the string to be at the very end of the
        /// input string. It does not validate an eventual file path against
        /// valid path characters. It does support the empty/unknown file
        /// location.
        /// </remarks>
        public static readonly Regex RegexPattern = new Regex(@"<(?<file>.*),(?<line>\d*),(?<column>\d*)>\z");

        /// <summary>
        /// The file.
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// The line in the file.
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// The column of the specified line in the file.
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Initialize a <see cref="FileLocation"/> using
        /// default values.
        /// </summary>
        public FileLocation()
            : this(null, 0, 0) {
        }

        /// <summary>
        /// Initialize a <see cref="FileLocation"/>.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="line">The line in the file.</param>
        /// <param name="column">The column of the specified line in the file.</param>
        public FileLocation(string file, int line, int column)
            : base() {
            this.File = file;
            this.Line = line;
            this.Column = column;
        }

        /// <summary>
        /// Creates a string representation of the given location.
        /// </summary>
        /// <param name="location">
        /// The location the returned string should describe.
        /// </param>
        /// <returns>
        /// Text representation of the given location.
        /// </returns>
        public static string ToString(FileLocation location) {
            string file;
            int line;
            int column;

            if (location == null) {
                return string.Format("<,,>");
            }

            file = location.File ?? string.Empty;
            line = location.Line;
            column = location.Column;

            return string.Format("<{0},{1},{2}>", file, line, column);
        }

        /// <inheritdoc/>
        public override string ToString() {
            return FileLocation.ToString(this);
        }

        /// <summary>
        /// Converts the string representation of a file location to
        /// a <see cref="FileLocation"/> instance with values populated
        /// from the string.
        /// </summary>
        /// <param name="locationString">The string to convert. The
        /// regular expression pattern <see cref="FileLocation.RegexPattern"/>
        /// is used to do the match.</param>
        /// <returns>
        /// A <see cref="FileLocation"/> representing the location
        /// described by <paramref name="locationString"/>.
        /// </returns>
        public static FileLocation Parse(string locationString) {
            Match match;

            if (string.IsNullOrEmpty(locationString))
                return FileLocation.Unknown;

            match = FileLocation.RegexPattern.Match(locationString);
            if (!match.Success)
                return FileLocation.Unknown;

            return FromMatch(match);
        }

        /// <summary>
        /// Creates a new <see cref="FileLocation"/> based on a match
        /// done by using the <see cref="FileLocation.RegexPattern"/>
        /// as the pattern.
        /// </summary>
        /// <param name="match">The match to use.</param>
        /// <returns>A <see cref="FileLocation"/> initialized with the
        /// values in the given match.</returns>
        internal static FileLocation FromMatch(Match match) {
            string file;
            int line;
            int column;
            int temp;

            file = match.Groups["file"].Value.Trim();
            line = int.TryParse(match.Groups["line"].Value, out temp) ? temp : 0;
            column = int.TryParse(match.Groups["column"].Value, out temp) ? temp : 0;

            return new FileLocation(file, line, column);
        }
    }
}