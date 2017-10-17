using System;
using System.IO;
using NetEmit.API;
using NetEmit.Cecil;
using NetEmit.Netfx;
using NUnit.Framework;
using static NUnit.Framework.TestContext;
using static NetEmit.Test.Testing;

namespace NetEmit.Test
{
    [TestFixture]
    public class MinimalTests
    {
        private static readonly string ResDir = Path.Combine(CurrentContext.TestDirectory, "Resources");

        #region Models

        private static AssemblyDef BuildTestModel(string key)
        {
            switch (key.ToLowerInvariant())
            {
                case "forms":
                    return BuildFormsModel();
                case "native":
                    return BuildNativeModel();
                case "service":
                    return BuildServiceModel();
                case "wcf":
                    return BuildWcfModel();
                case "wpf":
                    return BuildWpfModel();
                default:
                    throw new InvalidOperationException(key);
            }
        }

        private static AssemblyDef BuildWpfModel()
            => new AssemblyDef
            {
                Name = "WpfTest",
                Manifest = new ManifestDef
                {
                    Guid = new Guid("5A50630E-FF58-4496-B406-CE70FA92DD71"),
                    Framework = "4.6.2",
                    EntryPoint = "WpfTest.Program.Main"
                },
                Namespaces = { new NamespaceDef { Name = "WpfTest", Types = { new ClassDef { Name = "Program" } } } }
            };

        private static AssemblyDef BuildWcfModel()
            => new AssemblyDef
            {
                Name = "WcfTest",
                Manifest = new ManifestDef
                {
                    Guid = new Guid("60390c85-05c6-49bf-9f4f-d02061ba9fbc"),
                    Framework = "4.6.2",
                    EntryPoint = "WcfTest.Program.Main",
                },
                Namespaces = { new NamespaceDef { Name = "WcfTest", Types = { new ClassDef { Name = "Program" } } } }
            };

        private static AssemblyDef BuildServiceModel()
            => new AssemblyDef
            {
                Name = "ServiceTest",
                Manifest = new ManifestDef
                {
                    Guid = new Guid("f98c810c-efd5-4714-9683-1d57539ebb72"),
                    Framework = "4.6.2",
                    EntryPoint = "ServiceTest.Program.Main",
                },
                Namespaces = { new NamespaceDef { Name = "ServiceTest", Types = { new ClassDef { Name = "Program" } } } }
            };

        private static AssemblyDef BuildNativeModel()
            => new AssemblyDef
            {
                Name = "NativeTest",
                Manifest = new ManifestDef
                {
                    Guid = new Guid("cf933a46-8efe-4cf7-b42f-e186afcd4433"),
                    Framework = "4.6.2",
                    EntryPoint = "NativeTest.Program.Main",
                },
                Namespaces = { new NamespaceDef { Name = "NativeTest", Types = { new ClassDef { Name = "Program" } } } }
            };

        private static AssemblyDef BuildFormsModel()
            => new AssemblyDef
            {
                Name = "FormsTest",
                Manifest = new ManifestDef
                {
                    Guid = new Guid("cb1fd752-5b6d-4721-b736-f7dfec2b1ec5"),
                    Framework = "4.6.2",
                    EntryPoint = "FormsTest.Program.Main"
                },
                Resources =
                {
                    new ResourceDef {Name = "FormsTest.Form1.resources", Length = 180},
                    new ResourceDef {Name = "FormsTest.Properties.Resources.resources", Length = 180}
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

        #endregion

        [Test]
        [TestCase("Forms")]
        /*[TestCase("Native")]
        [TestCase("Service")]
        [TestCase("Wcf")]
        [TestCase("Wpf")]*/
        public void ShouldEmitSimilar(string suffix)
        {
            var noOpEmitter = new NoOpEmitter(Path.Combine(ResDir, $"{suffix}Test.exe"));
            var gens = new IAssemblyEmitter[]
            {
                noOpEmitter, new CecilEmitter(), new AssemblyEmitter() /*, new CSharpEmitter()*/
            };
            CompareIlOutput(gens, v => ShouldEmit(BuildTestModel(suffix), v, "mmp"));
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