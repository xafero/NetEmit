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
                Manifest = new ManifestDef {Guid = StaticGuid},
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
                    },
                    new NamespaceDef
                    {
                        Name = "My.Service.Api",
                        Types =
                        {
                            new InterfaceDef
                            {
                                Name = "IService",
                                Members =
                                {
                                    new MethodDef {Name = "GetTime"},
                                    new PropertyDef {Name = "Name"},
                                    new EventDef {Name = "OnStartUp"},
                                    new IndexerDef()
                                }
                            },
                            new StructDef
                            {
                                Name = "Book",
                                Members =
                                {
                                    new ConstructorDef(),
                                    new ConstantDef {Name = "MaxNumber"},
                                    new FieldDef {Name = "number"},
                                    new MethodDef {Name = "Buy"},
                                    new PropertyDef {Name = "Title"},
                                    new IndexerDef(),
                                    new OperatorDef(),
                                    new EventDef {Name = "Bought"}
                                }
                            },
                            new EnumDef
                            {
                                Name = "Days",
                                Members =
                                {
                                    new ConstantDef {Name = "Sat"},
                                    new ConstantDef {Name = "Wed"},
                                    new ConstantDef {Name = "Fri"}
                                }
                            },
                            new DelegateDef
                            {
                                Name = "Delegat",
                                Members =
                                {
                                    new MethodDef()
                                }
                            },
                            new ClassDef
                            {
                                Name = "Test",
                                Members =
                                {
                                    new ConstructorDef(),
                                    new ConstantDef {Name = "MaxLength"},
                                    new FieldDef {Name = "Id"},
                                    new MethodDef {Name = "Quit"},
                                    new PropertyDef {Name = "Caption"},
                                    new IndexerDef(),
                                    new OperatorDef(),
                                    new EventDef {Name = "Disposed"}
                                }
                            }
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