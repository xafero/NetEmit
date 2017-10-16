using System;
using NetEmit.API;
using NetEmit.Cecil;
using NetEmit.CodeDom;
using NetEmit.Netfx;
using NUnit.Framework;

using static NetEmit.Test.Testing;

namespace NetEmit.Test
{
    [TestFixture]
    public class EmitTests
    {
        private static readonly Guid StaticGuid = Guid.NewGuid();

        #region Model

        private static AssemblyDef BuildTestModel()
            => new AssemblyDef
            {
                Name = "TestMod",
                Manifest = new ManifestDef { Guid = StaticGuid },
                Namespaces =
                {
                    new NamespaceDef
                    {
                        Name = "Auto.Space",
                        Types =
                        {
                            new EnumDef {Name = "MyE"},
                            new StructDef {Name = "MyS"},
                            new DelegateDef {Name = "MyD"},
                            new InterfaceDef {Name = "MyI"},
                            new ClassDef {Name = "MyC"}
                        }
                    }
                }
            };

        #endregion

        [Test]
        public void ShouldEmitAssembly() => ShouldEmit(BuildTestModel(), new AssemblyEmitter(), "ae");

        [Test]
        public void ShouldEmitCSharp() => ShouldEmit(BuildTestModel(), new CSharpEmitter(), "ce");

        [Test]
        public void ShouldEmitCecil() => ShouldEmit(BuildTestModel(), new CecilEmitter(), "me");

        [Test]
        public void ShouldCompareIlOutput()
            => CompareIlOutput(new IAssemblyEmitter[]
            {
                new AssemblyEmitter(), new CecilEmitter(), new CSharpEmitter()
            }, v => ShouldEmit(BuildTestModel(), v, "cmp"));
    }
}