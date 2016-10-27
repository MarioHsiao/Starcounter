using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.IO;

namespace Starcounter.Internal.Tests
{
    [TestFixture]
    public class GuardTest
    {
        [Test]
        public void GuardNotNull()
        {
            var e = Assert.Throws<ArgumentNullException>(() => Guard.NotNull(null, "veryMuchNull"));
            Assert.True(e.Message.Contains("veryMuchNull"));

            Guard.NotNull(new object(), "wontHappen");
        }

        [Test]
        public void GuardNotNullOrEmpty()
        {
            var e = Assert.Throws<ArgumentNullException>(() => Guard.NotNullOrEmpty(null, "veryMuchNull"));
            Assert.True(e.Message.Contains("veryMuchNull"));

            e = Assert.Throws<ArgumentNullException>(() => Guard.NotNullOrEmpty(string.Empty, "veryMuchEmpty"));
            Assert.True(e.Message.Contains("veryMuchEmpty"));

            Guard.NotNullOrEmpty("abc", "wontHappen");
        }

        [Test]
        public void GuardDirectoryExists()
        {
            var e = Assert.Throws<ArgumentNullException>(() => Guard.DirectoryExists(null, "veryMuchNull"));
            Assert.True(e.Message.Contains("veryMuchNull"));

            e = Assert.Throws<ArgumentNullException>(() => Guard.DirectoryExists(string.Empty, "veryMuchEmpty"));
            Assert.True(e.Message.Contains("veryMuchEmpty"));

            var current = Directory.GetCurrentDirectory();
            var nonExistingDir = Path.Combine(current, Guid.NewGuid().ToString() + "asdfjk8asdf8jasdf");
            Assert.False(Directory.Exists(nonExistingDir));

            var e2 = Assert.Throws<DirectoryNotFoundException>(() => Guard.DirectoryExists(nonExistingDir, "myDirectory"));
            Assert.True(e2.Message.Contains("myDirectory"));

            Guard.DirectoryExists(current, "wontHappen");
        }
    }
}
