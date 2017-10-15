using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using NetEmit.API;
using NetEmit.Cecil;
using NetEmit.CodeDom;
using NetEmit.Netfx;
using NUnit.Framework;

namespace NetEmit.Test
{
    [TestFixture]
    public class EmitTests
    {
        private static readonly ILHelper Helper = HelperFactory.CreateIlHelper();
        
        private static readonly Guid StaticGuid = Guid.NewGuid();

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
                    }
                }
            };

        private static string ShouldEmit(IAssemblyEmitter emitter, string prefix)
        {
            var ass = BuildTestModel();
            var dir = Path.GetFullPath(prefix);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            ass.FileName = Path.Combine(dir, ass.GetFileName());
            var targetFile = Path.Combine(dir, $"{emitter.GetType().Name}_{ass.Name}.dll");
            string file;
            using (var bld = emitter)
                file = bld.Emit(ass);
            var ilFile = Path.Combine(dir, $"{emitter.GetType().Name}_{ass.Name}.txt");
            var dasm = Helper.GetDasmCmd(file, ilFile);
            using (var proc = Process.Start(new ProcessStartInfo(dasm.Item1, dasm.Item2)))
                proc?.WaitForExit();
            Helper.Filter(ilFile);
            if (File.Exists(targetFile))
                File.Delete(targetFile);
            File.Move(file, file = targetFile);
            Console.WriteLine(file);
            Console.WriteLine(ilFile);
            Assert.IsTrue(File.Exists(file));
            Assert.IsTrue(File.Exists(ilFile));
            Assert.IsTrue(new FileInfo(file).Length >= 100);
            Assert.IsTrue(new FileInfo(ilFile).Length >= 100);
            return File.ReadAllText(ilFile, Encoding.UTF8);
        }

        [Test]
        public void ShouldEmitAssembly() => ShouldEmit(new AssemblyEmitter(), "ae");

        [Test]
        public void ShouldEmitCSharp() => ShouldEmit(new CSharpEmitter(), "ce");

        [Test]
        public void ShouldEmitCecil() => ShouldEmit(new CecilEmitter(), "me");

        [Test]
        public void ShouldCompareIlOutput()
        {
            var gens = new IAssemblyEmitter[]
            {
                new AssemblyEmitter(), new CecilEmitter(), new CSharpEmitter()
            };
            var ils = gens.ToDictionary(k => k.GetType().Name, v => ShouldEmit(v, "cmp"));
            Assert.AreEqual(3, ils.Count);
            Directory.CreateDirectory("res");
            foreach (var first in ils)
                foreach (var second in ils)
                {
                    if (first.Key.Equals(second.Key))
                        continue;
                    var diffFile = Path.Combine("res", $"{first.Key}-{second.Key}.diff");
                    var ins = 0;
                    var del = 0;
                    var chg = 0;
                    WriteDiff(first.Value, second.Value, diffFile, ref ins, ref del, ref chg);
                    Console.WriteLine($"{Path.GetFileName(diffFile)} ({ins}+, {del}-, {chg}~)");
                    Assert.IsTrue(File.Exists(diffFile));
                    Assert.AreEqual(0, new FileInfo(diffFile).Length);
                }
        }

        private static void WriteDiff(string oldText, string newText, string file,
            ref int inserts, ref int deletes, ref int changes)
        {
            using (var diffFile = File.CreateText(file))
            {
                var diffBuilder = new InlineDiffBuilder(new Differ());
                var diff = diffBuilder.BuildDiffModel(oldText, newText);
                foreach (var line in diff.Lines)
                {
                    switch (line.Type)
                    {
                        case ChangeType.Inserted:
                            diffFile.Write("+ ");
                            inserts++;
                            break;
                        case ChangeType.Deleted:
                            diffFile.Write("- ");
                            deletes++;
                            break;
                        case ChangeType.Unchanged:
                            continue;
                        default:
                            diffFile.Write("  ");
                            changes++;
                            break;
                    }
                    diffFile.WriteLine(line.Text);
                }
            }
        }
    }
}