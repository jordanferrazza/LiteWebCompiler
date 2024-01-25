using StuffProject.Toolbox;
using StuffProject.Toolbox.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteWebCompiler
{
    public class TagCompiler
    {
        public override string ToString()
        {
            return Name;
        }
        public string Name { get; set; }
        public string Tag { get; set; }
        public List<TagCompiler> LineWrap { get; set; } = new List<TagCompiler>();
        public Dictionary<string, TagCompiler> LineSplit { get; set; } = new Dictionary<string, TagCompiler>();
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        public TagCompiler(string tag)
        {
            Name = tag;
            Tag = tag;
        }

        public string StartTag()
        {
            return $"<{Name}>";
        }
        public string StartTag(Interpreter caller)
        {
            StorageHandler.MergeDictionary(caller.Storage.PropDump, caller.Storage.Cache);
            if (caller.Storage.TargetedPropDump.ContainsKey(Name))
            {
                StorageHandler.MergeDictionary(caller.Storage.TargetedPropDump[Name], caller.Storage.Cache);
                StorageHandler.MergeDictionary(caller.Storage.TargetedPropDump[Name], caller.Storage.PropDump);
                caller.Storage.TargetedPropDump.Remove(Name);
            }
            var p = new Dictionary<string, string>(Properties);
            StorageHandler.MergeDictionary(caller.Storage.PropDump, p);
            var props = p.Select(x => $"{x.Key}=\"{x.Value}\"").ToStringConcat(" ");
            caller.Storage.PropDump.Clear();

            return $"<{Tag}{$" {props}".TrimEnd()}>";
        }
        public string EndTag()
        {
            return $"</{Tag}>";
        }

        public string Run(string contents, Interpreter caller)
        {
            // var o = caller.GetVars(contents.Trim(), caller.PropDump);
            var o = contents.Trim();
            foreach (var item in LineSplit)
            {
                o = o.Split(Environment.NewLine).Select(y => y.Contains(item.Key) ? y.Split(item.Key, StringSplitOptions.RemoveEmptyEntries).Select(x =>
                {
                    if (x.Contains("::"))
                    {
                        var line = x.Split("::");
                        for (int i = 1; i < line.Length; i++)
                        {
                            caller.Run(line[i].Trim());
                        }
                        x = line.First().Trim();
                    }

                    return item.Value.Run(x, caller);
                }


                ).ToStringConcat() : y).ToStringConcat(Environment.NewLine);
            }
            foreach (var item in LineWrap)
            {
                o = o.Split(Environment.NewLine).Select(x => item.Run(x, caller)).ToStringConcat(Environment.NewLine);
            }

            return $"{StartTag(caller)}{o}{EndTag()}";
        }


    }
}
