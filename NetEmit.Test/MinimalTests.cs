using System;
using System.IO;
using NUnit.Framework;

using static NUnit.Framework.TestContext;

namespace NetEmit.Test
{
    [TestFixture]
    public class MinimalTests
    {
        private static readonly string ResDir = Path.Combine(CurrentContext.TestDirectory, "Resources");

        [Test]
        [TestCase("FormsTest")]
        [TestCase("NativeTest")]
        [TestCase("ServiceTest")]
        [TestCase("WcfTest")]
        [TestCase("WpfTest")]
        public void ShouldEmitSimilar(string suffix)
        {
            var inputAss = Path.Combine(ResDir, $"{suffix}.exe");
            Console.WriteLine(inputAss);
        }
    }
}