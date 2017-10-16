using System.IO;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace NetEmit.Test
{
    internal static class Testing
    {
        internal static readonly ILHelper Helper = HelperFactory.CreateIlHelper();

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
    }
}