using StuffProject.ConsoleExt;
using StuffProject.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static LiteWebCompiler.Interpreter;

namespace LiteWebCompiler
{
    public class StorageHandler : Handler
    {
        public Interpreter ParentInterpreter { get; private set; }

        public StorageHandler(Interpreter parent)
        {
            ParentInterpreter = parent;
        }

        static public void MergeDictionary(Dictionary<string, string> from, Dictionary<string, string> to)
        {
            foreach (var item in from)
            {
                to[item.Key] = item.Value;
            }
        }
        public Dictionary<string, string> PropDump = new Dictionary<string, string>();
        public Dictionary<string, Dictionary<string, string>> TargetedPropDump = new Dictionary<string, Dictionary<string, string>>();

        private Dictionary<string, string> CollectVars()
        {
            var vars = new Dictionary<string, string>();
            MergeDictionary(GlobalVars, vars);
            MergeDictionary(Cache.ToDictionary(x => "_c_" + x.Key, x => x.Value), vars);
            MergeDictionary(PropDump.ToDictionary(x => "_p_" + x.Key, x => x.Value), vars);
            foreach (var item in TargetedPropDump)
            {
                MergeDictionary(item.Value.ToDictionary(x => $"_p_{item.Key}_{x.Key}", x => x.Value), vars);

            }
            return vars;
        }

        public List<WritingModes> FocusModes = new List<WritingModes>();
        public string GetVars(string text)
        {
            var vars = CollectVars();


            text = Regex.Replace(text, "{{[^{]*}}", x =>
            {
                var key = x.Value.Substring(2, x.Value.Length - 4);
                return Var(vars, key);

            });
            return text;
        }

        public string? GetVar(string name)
        {
            var vars = CollectVars();
            return Var(vars, name);
        }

        private string? Var(Dictionary<string, string> vars, string key)
        {
            if (!vars.ContainsKey(key))
            {
                ConsoleExt.WriteLine($"Variable : (undefined) < {key}", ConsoleColor.Red);
                ParentInterpreter.AddWarning($"Unknown variable used, {key}");
                return null;
            }
            ConsoleExt.WriteLine($"Variable : {vars[key]} < {key}", ConsoleColor.Magenta);
            return vars[key];
        }

        public string GetAliases(string text)
        {
            foreach (var item in InlineAliases)
            {
                text = Regex.Replace(text, $"(?<!\\\\){Regex.Escape(item.Key)}(?:(?!{Regex.Escape(item.Key)}).)*{Regex.Escape(item.Key)}", x =>
                {
                    ConsoleExt.WriteLine($"Alias : {item.Value.Name} ({item.Value.Tag}) < {item.Key}", ConsoleColor.Cyan);
                    return InlineAliases[x.Value.Remove(item.Key.Length)].Run(x.Value.Substring(item.Key.Length, x.Value.Length - (item.Key.Length * 2)), ParentInterpreter);

                });

            }
            return text;
        }
        public string? Writer
        {
            get
            {
                return Writers.LastOrDefault();
            }
            set
            {
                if (Writers.Count == 0 && value != null)
                    Writers.Add(value);
                else
                    Writers[Writers.Count - 1] = value;
            }
        }
        public TagCompiler Tag(string key)
        {
            if (Aliases.ContainsKey(key))
                return Aliases[key];
            if (Tags.ContainsKey(key))
                return Tags[key];
            return null;
        }

        public Listonary<string, TagCompiler> Tags = new Listonary<string, TagCompiler>(x => x.Name);
        public Dictionary<string, TagCompiler> Aliases = new Dictionary<string, TagCompiler>();
        public Dictionary<string, TagCompiler> InlineAliases = new Dictionary<string, TagCompiler>();
        public Dictionary<string, string> GlobalVars = new Dictionary<string, string>();
        public Dictionary<string, string> Cache = new Dictionary<string, string>();

        private TagCompiler _Default;

        public TagCompiler Default
        {
            get { return _Default; }
            set
            {
                GlobalVars["__default"] = value?.ToString();
                _Default = value;
            }
        }


        public void Let(string name, string value)
        {
            GlobalVars[name] = value;
            ConsoleExt.WriteLine($"Variable : {name} > {value}", ConsoleColor.Magenta);
        }

        public void AddAlias(string aliasId, string tagId)
        {
            if (Tags[tagId] == null) throw new ArgumentNullException();
            Aliases.Add(aliasId, Tags[tagId]);
        }

        public void SetDefault(string tagId)
        {
            if (Tags[tagId] == null) throw new ArgumentNullException();
            Default = Tags[tagId];
        }

        public void AddInlineAlias(string alias, string tagId)
        {
            if (Tags[tagId] == null) throw new ArgumentNullException();
            InlineAliases.Add(alias, Tags[tagId]);
        }

        public void BeginEdit(string coms2)
        {
            if (Tags[coms2] == null) throw new ArgumentNullException();
            Focuses.Add(Tags[coms2]);
            FocusModes.Add(WritingModes.None);
            Writers.Add("");
        }

        public enum WritingModes
        {
            None,
            Literal,
            Dynamic
        }



        public void ChangeLineWrapOfFocus(string tagId)
        {
            Focus.LineWrap.Add(Tags[tagId]);
        }

        public void ChangeLineSplitOfFocus(string alias, string tagId)
        {
            Focus.LineSplit.Add(alias, Tags[tagId]);
        }

        public void SetTargetedPropDump(string tagId, string prop, string value)
        {
            if (!TargetedPropDump.ContainsKey(tagId)) TargetedPropDump[tagId] = new Dictionary<string, string>();
            TargetedPropDump[tagId][prop] = value;
        }



        public void JumpOut()
        {
            if (Focuses.Count == 0) throw new InvalidOperationException("Attempt to step out of block with no block to step out of.");

            if (FocusMode == WritingModes.Dynamic)
            {
                Writer += Focus2.EndTag();
            }
            if (Writer != null && FocusMode != WritingModes.None)
            {
                var o = FocusMode == WritingModes.Dynamic ? Writer : Focus2.Run(GetAliases(Writer.TrimEnd()), ParentInterpreter);
                if (Writers.Count == 1)
                {
                    ParentInterpreter.IO.Write(o);
                }
                else
                {
                    Writers[Writers.Count - 2] += o + Environment.NewLine;
                }
            }
            FocusModes.RemoveAt(FocusModes.Count - 1);
            Focuses.Remove(Focuses.Last());
            Writers.RemoveAt(Writers.Count - 1);
        }

        public void JumpInDynamic(string tagId)
        {
            Focuses.Add(Tag(tagId));
            FocusModes.Add(WritingModes.Dynamic);
            Writers.Add(Focus2.StartTag(ParentInterpreter) + Environment.NewLine);
        }

        internal void JumpInLiteral(string tagId)
        {
            Focuses.Add(Tag(tagId));
            FocusModes.Add(WritingModes.Literal);
            Writers.Add("");
        }

        public void Send(string line, string coms2, string v)
        {
            var oo = Regex.Replace(GetAliases(coms2), "((?<!\\\\)|^)\\\\.",x=>x.Value.Substring(1));
            var oo2 = Regex.Replace(GetAliases(line), "((?<!\\\\)|^)\\\\.", x => x.Value.Substring(1));
            if ("<>".Any(x => Tag(v) == null && Focus == null ? line.Contains(x) : coms2.Contains(x)))
                ParentInterpreter.AddWarning("Reserved character detected in text - Please escape these using \\, UNLESS you are manually entering XML to the text.");


            var o = Focus?.Run(oo, ParentInterpreter) ?? Tag(v)?.Run(oo, ParentInterpreter) ?? Default?.Run(oo2, ParentInterpreter) ?? oo2;

            if ((Focus ?? Tag(v) ?? Default) == null)
                ParentInterpreter.AddWarning("Printing plain text, as no default tag specified and alias not found.");

            if (FocusMode == WritingModes.Dynamic)
                Writer += o + Environment.NewLine;
            else
                ParentInterpreter.IO.Write(o);
        }

        public void Clear()
        {
            Tags.Clear();
            Aliases.Clear();
            InlineAliases.Clear();
        }

        public List<TagCompiler> Focuses = new List<TagCompiler>();
        public List<string> Writers = new List<string>();
        public TagCompiler? Focus
        {
            get
            {
                var o = FocusMode == WritingModes.Dynamic ? null : Focuses.LastOrDefault();
                GlobalVars["_g_current"] = o?.ToString();
                return o;
            }
        }
        public TagCompiler? Focus2 => Focuses.LastOrDefault();
        public WritingModes FocusMode => FocusModes.Count == 0 ? WritingModes.Literal : FocusModes.LastOrDefault();


    }
}
