using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Starcounter.ABCIPC;
using Starcounter.ABCIPC.Internal;
using System.Reflection;

namespace Starcounter.Internal.Tests {
    
    /// <summary>
    /// Expose a set of test methods allowing unit testing of the
    /// ABCIPC in Starcounter.Internal.
    /// </summary>
    [TestFixture]
    public class ABCIPCTest {

        /// <summary>
        /// Executes all methods of this class that are considered
        /// being tests.
        /// </summary>
        /// <remarks>
        /// Allows this assembly to be turned into an executable and
        /// execute tests from the OS shell.
        /// </remarks>
        public static void Main() {
            var test = new ABCIPCTest();
            foreach (var item in test.GetType().GetMethods()) {
                if (item.ReturnType == typeof(void) &&
                    item.GetParameters().Length == 0 &&
                    item.GetCustomAttribute(typeof(TestAttribute)) != null) {
                        item.Invoke(test, null);
                }
            }
        }

        /// <summary>
        /// Tests the predicability of the public constructor of the
        /// <see cref="Client"/> class.
        /// </summary>
        [Test]
        public void TestClientConstructor() {
            Assert.Throws<ArgumentNullException>(new TestDelegate(() => {
                new Client(null, () => { return string.Empty; });
            }));
            Assert.Throws<ArgumentNullException>(new TestDelegate(() => {
                new Client((s) => { }, null);
            }));
        }

        /// <summary>
        /// Tests the predicability of the public constructor of the
        /// <see cref="Server"/> class.
        /// </summary>
        [Test]
        public void TestServerConstructor() {
            Assert.Throws<ArgumentNullException>(new TestDelegate(() => {
                new Server(null, (message, isEndOfRequest) => { });
            }));
            Assert.Throws<ArgumentNullException>(new TestDelegate(() => {
                new Server(() => { return string.Empty; }, null);
            }));
        }

        /// <summary>
        /// Tests <see cref="Client.Send(string)"/> with focus on the
        /// possible return values that doesn't trigger exceptions.
        /// </summary>
        /// <remarks>
        /// These are the known, and tested, return possibilities:
        /// OK
        /// OKWithCarry
        /// Fail
        /// FailWithCarry
        /// </remarks>
        [Test]
        public void TestClientSendReturnValues() {
            Client c;
            
            // Create a client who will only receive "OK"
            // replies, and send a message through it, asserting
            // it was properly interpreted.
            c = new Client((s) => { }, () => {
                return Reply.Protocol.MakeString(Reply.ReplyType.OK);
            });
            Assert.True(c.Send("Foo"));

            // Do the same, but this time assume failure.
            c = new Client((s) => { }, () => {
                return Reply.Protocol.MakeString(Reply.ReplyType.Fail);
            });
            Assert.False(c.Send("Foo"));

            // Now create a client that returns OK, with a carry,
            // one we can properly retreive
            c = new Client((s) => { }, () => {
                return Reply.Protocol.MakeString(Reply.ReplyType.OKWithCarry, "Foo");
            });
            Assert.True(c.Send("Foo", (Reply r) => {
                string response;
                Assert.True(r.TryGetCarry(out response));
                Assert.True(response.Equals("Foo"));
            }));

            // And then the same, but with a failure.
            c = new Client((s) => { }, () => {
                return Reply.Protocol.MakeString(Reply.ReplyType.FailWithCarry, "Foo");
            });
            Assert.False(c.Send("Foo", (Reply r) => {
                string response;
                Assert.True(r.TryGetCarry(out response));
                Assert.True(response.Equals("Foo"));
            }));
        }
    }
}