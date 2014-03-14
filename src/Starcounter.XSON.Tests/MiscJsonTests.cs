using System;
using NUnit.Framework;
using Starcounter.Internal.JsonPatch;
using Starcounter.Templates;

namespace Starcounter.Internal.XSON.Tests {
    class MiscJsonTests {
        /// <summary>
        /// Tests the app index path.
        /// </summary>
        [Test]
        public static void TestAppIndexPath() {
            AppAndTemplate aat = Helper.CreateSampleApp();
            TObject appt = (TObject)aat.Template;

            var firstName = (Property<string>)appt.Properties[0];
            Int32[] indexPath = aat.App.IndexPathFor(firstName);
            VerifyIndexPath(new Int32[] { 0 }, indexPath);

            TObject anotherAppt = (TObject)appt.Properties[3];
            Json nearestApp = anotherAppt.Getter(aat.App);

            var desc = (Property<string>)anotherAppt.Properties[1];
            indexPath = nearestApp.IndexPathFor(desc);
            VerifyIndexPath(new Int32[] { 3, 1 }, indexPath);

            TObjArr itemProperty = (TObjArr)appt.Properties[2];
            Json items = itemProperty.Getter(aat.App);

            nearestApp = (Json)items._GetAt(1);
            anotherAppt = (TObject)nearestApp.Template;

            TBool delete = (TBool)anotherAppt.Properties[2];
            indexPath = nearestApp.IndexPathFor(delete);
            VerifyIndexPath(new Int32[] { 2, 1, 2 }, indexPath);
        }

        /// <summary>
        /// Verifies the index path.
        /// </summary>
        /// <param name="expected">The expected.</param>
        /// <param name="received">The received.</param>
        private static void VerifyIndexPath(Int32[] expected, Int32[] received) {
            Assert.AreEqual(expected.Length, received.Length);
            for (Int32 i = 0; i < expected.Length; i++) {
                Assert.AreEqual(expected[i], received[i]);
            }
        }
    }
}
