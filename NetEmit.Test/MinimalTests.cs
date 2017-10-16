using System;
using System.IO;
using NetEmit.API;
using NUnit.Framework;

using static NUnit.Framework.TestContext;

namespace NetEmit.Test
{
    [TestFixture]
    public class MinimalTests
    {
        private static readonly string ResDir = Path.Combine(CurrentContext.TestDirectory, "Resources");

        #region Models

        private static AssemblyDef BuildTestModel(string key)
        {
            return new AssemblyDef
            {
                Name = "FormsTest",
                Version = "1.0.0.0",
                Manifest = new ManifestDef
                {
                    Framework = "4.6.2",
                    EntryPoint = "FormsTest.Program.Main"
                },
                Namespaces =
                {
                    new NamespaceDef
                    {
                        Name = "FormsTest",
                        Types =
                        {
                            new ClassDef {Name = "Form1"},
                            new ClassDef {Name = "Program"}
                        },
                        Namespaces =
                        {
                            new NamespaceDef
                            {
                                Name = "Properties",
                                Types =
                                {
                                    new ClassDef {Name = "Resources"},
                                    new ClassDef {Name = "Settings"}
                                }
                            }
                        }
                    }
                }
            };
        }

        #endregion

        [Test]
        [TestCase("Forms")]
        [TestCase("Native")]
        [TestCase("Service")]
        [TestCase("Wcf")]
        [TestCase("Wpf")]
        public void ShouldEmitSimilar(string suffix)
        {
            var noOpModel = BuildTestModel(suffix);
            var noOpEmitter = new NoOpEmitter(Path.Combine(ResDir, $"{suffix}Test.exe"));
            Console.WriteLine(noOpEmitter);
        }

        #region No-OP

        private class NoOpEmitter : IAssemblyEmitter
        {
            private readonly string _path;

            public NoOpEmitter(string path)
            {
                _path = path;
            }

            public string Emit(AssemblyDef ass) => _path;

            public void Dispose()
            {
            }
        }

        #endregion
    }
}