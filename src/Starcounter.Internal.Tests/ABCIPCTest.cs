using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Starcounter.ABCIPC;
using Starcounter.ABCIPC.Internal;

namespace Starcounter.Internal.Tests {
    
    /// <summary>
    /// Expose a set of test methods allowing unit testing of the
    /// ABCIPC in Starcounter.Internal.
    /// </summary>
    [TestFixture]
    public class ABCIPCTest {
        
        /// <summary>
        /// Tests the predicability of the public constructor of the
        /// <see cref="Client"/> class.
        /// </summary>
        [Test]
        public void TestClientConstructor() {
            try {
                new Client(null, () => { return string.Empty; });
            } catch (ArgumentNullException) {
                try {
                    new Client((s) => { }, null);
                } catch (ArgumentNullException) {
                    return;
                }
            }
            Assert.Fail();
        }
    }
}