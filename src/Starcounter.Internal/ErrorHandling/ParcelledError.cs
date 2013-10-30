
using System.Collections.Generic;

namespace Starcounter.Internal {
    /// <summary>
    /// Expose a set of utility code to work with errors wrapped in
    /// parcels.
    /// </summary>
    public static class ParcelledError {
        /// <summary>
        /// Creates a new parcelled error message.
        /// </summary>
        /// <param name="parcelID">The parcel identity to use.</param>
        /// <param name="errorMessage">The message being parcelled.</param>
        /// <returns>A string representing the error parcel.</returns>
        public static string Format(string parcelID, string errorMessage) {
            return string.Format("{0}{1}{2}", parcelID, errorMessage, parcelID);
        }

        /// <summary>
        /// Extract a set of parcelled errors from the given content.
        /// </summary>
        /// <param name="content">The content to parse.</param>
        /// <param name="parcelID">The parcel ID in use.</param>
        /// <param name="errors">
        /// The list to which extracted messages will be added.</param>
        /// <param name="maxCount">
        /// Optional maximum number of errors to extract.</param>
        public static void ExtractParcelledErrors(string[] content, string parcelID, List<string> errors, int maxCount = -1) {
            string currentParcel = null;

            foreach (var inputString in content) {
                if (inputString == null)
                    continue;

                // Are we currently parsing a multi-line parcel?

                if (currentParcel != null) {
                    // Yes we are.
                    // Check if we have reached the final line.

                    if (inputString.EndsWith(parcelID)) {
                        // End the parcel.

                        currentParcel += " " + inputString.Substring(0, inputString.Length - parcelID.Length);
                        errors.Add(currentParcel);
                        if (errors.Count == maxCount)
                            return;

                        currentParcel = null;
                    } else {
                        // Append the current line to the already
                        // identified parcel content and continue.

                        currentParcel += " " + inputString;
                    }
                } else {
                    // We are currently not in the middle of parsing a
                    // parcel. Check the input.

                    if (inputString.StartsWith(parcelID)) {
                        // Beginning of a new parcel. Create it.

                        currentParcel = inputString.Substring(parcelID.Length);

                        // Check if it's a one-line parcel and if it is,
                        // terminate it.

                        if (inputString.EndsWith(parcelID)) {
                            currentParcel = currentParcel.Substring(0, currentParcel.Length - parcelID.Length);
                            errors.Add(currentParcel);
                            if (errors.Count == maxCount)
                                return;

                            currentParcel = null;
                        }
                    }
                }
            }
        }
    }
}
