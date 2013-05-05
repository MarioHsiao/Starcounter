
namespace Starcounter.Administrator.API.Utilities {
    /// <summary>
    /// String extension methods.
    /// </summary>
    /// <remarks>
    /// Should be moved to some lower level assembly, like Starcounter.Internal.
    /// </remarks>
    public static class StringExtensions {
        /// <summary>
        /// Counts the number of occurances of a sequence part of a string.
        /// </summary>
        /// <param name="s">The string to count occurances in.</param>
        /// <param name="sequence">The sequence to match.</param>
        /// <param name="startIndex">The index in the source string to start from.
        /// </param>
        /// <returns>Number of occurances of <paramref name="sequence"/> found in
        /// <paramref name="s"/>.</returns>
        public static int CountOccurrences(this string s, string sequence, int startIndex = 0) {
            return InternalCountOccurrances(s, sequence, startIndex, 0);
        }

        static int InternalCountOccurrances(string s, string sequence, int startIndex, int count = 0) {
            int index = s.IndexOf(sequence, startIndex);
            if (index == -1) {
                return count;
            }

            count++;
            startIndex = index + sequence.Length;
            return InternalCountOccurrances(s, sequence, startIndex, count);
        }
    }
}