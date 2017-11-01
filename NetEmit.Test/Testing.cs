using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using NetEmit.API;
using NUnit.Framework;

namespace NetEmit.Test
{
    internal static class Testing
    {
        internal static readonly ILHelper Helper = HelperFactory.CreateIlHelper();

        internal static string ShouldEmit(AssemblyDef ass, IAssemblyEmitter emitter, string prefix)
        {
            var root = TestContext.CurrentContext.TestDirectory;
            var dir = Path.Combine(root, prefix);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            ass.FileName = Path.Combine(dir, ass.GetFileName());
            var targetFile = Path.Combine(dir, $"{emitter.GetType().Name}_{ass.Name}.{ass.GetExt()}");
            string file;
            using (var bld = emitter)
                file = bld.Emit(ass);
            var ilFile = Path.Combine(dir, $"{emitter.GetType().Name}_{ass.Name}.txt");
            var dasm = Helper.GetDasmCmd(file, ilFile);
            using (var proc = Process.Start(new ProcessStartInfo(dasm.Item1, dasm.Item2)))
                proc?.WaitForExit();
            Helper.Filter(ilFile);
            CleanupFile(ilFile);
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

        private static void CleanupFile(string file)
        {
            var enc = Encoding.UTF8;
            var raw = File.ReadAllText(file, enc);
            var text = Cleanup(raw);
            File.WriteAllText(file, text, enc);
        }

        internal static void CompareIlOutput(IAssemblyEmitter[] gens, Func<IAssemblyEmitter, string> ilgen)
        {
            var ils = gens.ToDictionary(k => k.GetType().Name, ilgen);
            Assert.AreEqual(3, ils.Count);
            var root = TestContext.CurrentContext.TestDirectory;
            var dir = Path.Combine(root, "res");
            Directory.CreateDirectory(dir);
            foreach (var first in ils)
                foreach (var second in ils)
                {
                    if (first.Key.Equals(second.Key))
                        continue;
                    var diffFile = Path.Combine(dir, $"{first.Key}-{second.Key}.diff");
                    var ins = 0;
                    var del = 0;
                    var chg = 0;
                    WriteDiff(first.Value, second.Value, diffFile, ref ins, ref del, ref chg);
                    Console.WriteLine($"{Path.GetFileName(diffFile)} ({ins}+, {del}-, {chg}~)");
                    Assert.IsTrue(File.Exists(diffFile));
                    Assert.AreEqual(0, new FileInfo(diffFile).Length);
                }
        }

        internal static void WriteDiff(string oldText, string newText, string file,
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

        internal static string Cleanup(string text) => (new CilSortNormalizer()).Normalize(text);
    }
}