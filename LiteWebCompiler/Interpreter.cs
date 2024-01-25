using StuffProject.ConsoleExt;
using StuffProject.Toolbox;
using StuffProject.Toolbox.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace LiteWebCompiler
{
    public class Interpreter
    {
        private string? _Output;

        public IOHandler IO { get; private set; }
        public StorageHandler Storage { get; private set; }

        public Interpreter(string? output = null)
        {
            ConsoleExt.WriteLine("======================================", ConsoleColor.Blue);
            ConsoleExt.WriteLine("LiteWebCompiler", ConsoleColor.Blue);
            ConsoleExt.WriteLine("XML compiler prject by Jordan Ferrazza", ConsoleColor.Blue);
            ConsoleExt.WriteLine("(C) 2023 Jordan Ferrazza", ConsoleColor.Blue);
            ConsoleExt.WriteLine("======================================", ConsoleColor.Blue);
            ConsoleExt.WriteLine("Welcome", ConsoleColor.Blue);
            IO = new IOHandler(this);
            Storage = new StorageHandler(this);
            IO.OutputFile = output;
        }



        public string StatusPrefix => $"{IO.OutputFile} > {Storage.Focuses.ToStringConcat(".")}:{Storage.FocusMode} > {Storage.Writers.Count}:{Storage.Writer?.Split(Environment.NewLine).Count()} > ";

        bool DoNextLine
        {
            get;
            set;
        } = true;
        bool IfStatementReturned = true;
        bool LineAfterSkipDone = true;

        internal string ThisLine = "";

        public void Run(string line, bool clearWarnings = true)
        {
            try
            {
                ThisLine = line.TrimStart();


                line = line.Split("//")[0].Trim();
                line = Storage.GetVars(line);
                line = Regex.Replace(line, "\\\\[^@]", x => SecurityElement.Escape(x.Value.Substring(1)));

                if (line == "") return;



                var coms = line.Split(' ');
                var coms2 = line.Split(' ').Count() < 2 ? "" : line.Split(' ').ToList().GetRange(1, coms.Count() - 1).ToStringConcat(" ");
                var coms3 = line.Split(' ').Count() < 3 ? "" : line.Split(' ').ToList().GetRange(2, coms.Count() - 2).ToStringConcat(" ");
                var coms4 = line.Split(' ').Count() < 3 ? "" : line.Split(' ').ToList().GetRange(3, coms.Count() - 3).ToStringConcat(" ");

                if (SkipFor > 0)
                {
                    if (clearWarnings)
                    {
                        AddWarning("Skip detected! Skip only works for and may cause issues when reading scripts! Use @clear skip or @skip 0 to delete.");

                    }
                    else
                    {
                        ConsoleExt.WriteLine($"Skip : {SkipFor} > Skipping this line! ^^^^", ConsoleColor.Red);
                        SkipFor--;
                        return;
                    }
                }
                if (!DoNextLine)
                {
                    if (!LineAfterSkipDone)
                    {
                        LineAfterSkipDone = true;
                        DoNextLine = true;
                    }
                    else
                    {
                        DoNextLine = IfStatementReturned;
                        LineAfterSkipDone = false;
                        ConsoleExt.WriteLine("Logic : False > Skipping this line! ^^^^", ConsoleColor.Red);
                        return;
                    }

                }

                if (Storage.Writer != null && !line.StartsWith("@") && Storage.FocusMode == StorageHandler.WritingModes.Literal)
                {
                    Storage.Writer += line + Environment.NewLine;
                }
                else
                    switch (coms[0])
                    {
                        case "@empty": //do nothing
                            break;
                        case "@else": //else
                            Expect(coms, 1, 1);
                            Else();
                            break;
                        case "@clear": //clear ...
                            Expect(coms, 2, 2);
                            Clear(coms[1]);
                            break;
                        case "@to": // set the output file
                            Expect(coms, min: 2);
                            IO.OutputFile = coms2;
                            break;
                        case "@run": // run a macro, such as a script to compile
                            Expect(coms, min: 2);
                            var f = Storage.Focus;
                            foreach (var item in File.ReadAllLines(coms2))
                            {
                                ConsoleExt.WriteLine($"Macro : {coms2} >> " + StatusPrefix + item, ConsoleColor.Yellow);
                                Run(item, false);

                            }
                            if (f != Storage.Focus)
                            {
                                ConsoleExt.WriteLine($"Macro : Warning! : Cursor pointed to different area to macro start! Expected {f}, got {Storage.Focus}.", ConsoleColor.Red);
                                AddWarning("Cursor pointed to different area to macro start! Expected {f}, got {Storage.Focus}.");
                            }
                            break;
                        case "@open": // open a file externally
                            Expect(coms, 1, 1);
                            IO.OpenFile();
                            break;
                        case "@editor": // edit a file externally
                            Expect(coms, 1, 1);
                            IO.OpenEditor();
                            break;
                        case "@format": // beautify the XML
                            Expect(coms, 1, 1);
                            IO.Format();
                            break;
                        case "@let": // declare a variable
                            Expect(coms, min: 3);
                            Storage.Let(coms[1], coms3);
                            break;
                        case "@if": // if
                            Expect(coms, 4, 4);
                            If(coms, false);
                            break;
                        case "@if_": // if, but using variables as plain text
                            Expect(coms, 4, 4);
                            If(coms, true);
                            break;
                        case "@work": // do math
                            Expect(coms, 5, 4);
                            if (coms.Length == 4)
                                Storage.Let(coms[1], Work(Storage.GetVar(coms[1]), coms[2], coms[3]));
                            else
                                Storage.Let(coms[1], Work(coms[2], coms[3], coms[4]));
                            break;

                        case "@create": // declare a tag
                            Expect(coms, 3, 2);
                            if (coms.Length == 3)
                                Storage.Tags.Add(new TagCompiler(coms[1]) { Tag = coms[2] });
                            else
                                Storage.Tags.Add(new TagCompiler(coms[1]));
                            break;
                        case "@create_": // declare a tag, then step into it to edit it
                            Expect(coms, 3, 2);
                            if (coms.Length == 3)
                                Storage.Tags.Add(new TagCompiler(coms[1]) { Tag = coms[2] });
                            else
                                Storage.Tags.Add(new TagCompiler(coms[1]));
                            Run("@edit_ " + coms[1]);
                            break;
                        case "@alias": // declare an alias tag, to use instead of a tag's actual name
                            Expect(coms, min: 3);
                            Storage.AddAlias(coms3, coms[1]);
                            break;
                        case "@default": // declare a default tag, to use when no prefix is specified
                            Expect(coms, 2, 2);
                            Storage.SetDefault(coms[1]);
                            break;
                        case "@inline": // declare an alias tag, to use on text surrounded by the alias
                            Expect(coms, 3, 3);
                            Storage.AddInlineAlias(coms3, coms[1]);
                            break;
                        case "@edit_": // edit a tag
                            Expect(coms, 2, 2);
                            Storage.BeginEdit(coms2);
                            break;


                        case "@line": // with the tag being edited, surround all paragraphs within it with an alias
                            Expect(coms, 2, 2);
                            Storage.ChangeLineWrapOfFocus(coms[1]);
                            break;
                        case "@split": // with the tag being edited, surround all strings split by a symbol within it with an alias
                            Expect(coms, 3, 3);
                            Storage.ChangeLineSplitOfFocus(coms[1], coms[2]);
                            break;
                        case "@tag": // with the tag being edited, set the actual name
                            Expect(coms, 2, 2);
                            Storage.Focus.Tag = coms2;
                            break;

                        case "@there": // with the tag being edited, set an XML attribute
                            Expect(coms, min: 3);
                            Storage.Focus.Properties[coms[1]] = coms3;
                            break;
                        case "@where":  // with the next tag being written, set an XML attribute
                            Expect(coms, min: 3);
                            Storage.PropDump[coms[1]] = coms3;
                            break;
                        case "@where_": // with the next tag being written that has a certain tag name, set an XML attribute
                            Expect(coms, min: 4);
                            Storage.SetTargetedPropDump(coms[1], coms[2], coms4);
                            break;
                        case "@skip": // do not do anything for a certain amount of lines (except manually compile)
                            Expect(coms, 2, 2);
                            SkipFor = int.Parse(coms[1]);
                            break;
                        case "@end": // step out of a block
                            Expect(coms, 1, 1);
                            Storage.JumpOut();


                            break;
                        default:
                            if (coms[0].StartsWith("@"))
                            {
                                throw new ArgumentException("Unknown command. If this was unintended, use \\@ instead of @.");
                            }
                            if (coms[0].EndsWith("__"))
                            {
                                Storage.JumpInDynamic(coms[0].Remove(coms[0].Length - 2));
                            }
                            else if (coms[0].EndsWith("_"))
                            {
                                Storage.JumpInLiteral(coms[0].Remove(coms[0].Length - 1));
                            }
                            else
                            {
                                Storage.Send(line, coms2, coms[0]);
                            }
                            break;
                    }

            }
            catch (Exception ex)
            {
                IO.Write("\n", true);
                IO.Write(@"
X 
X P", true);
                IO.Write("========================================================\n", true);
                IO.Write($"COMPILATION FAILED AT: {line} \n", true);
                if (ex is NullReferenceException || ex is ArgumentNullException)
                    IO.Write($"Hint: This error may be caused by a null current tag. If so, make sure a tag is selected.\n");
                if (ex is FileNotFoundException)
                    IO.Write($"Hint: Make sure the file reference is correct.\n");

                IO.Write("========================================================\n", true);
                IO.Write(ex.ToString() + "\n", true);
                IO.Write("========================================================\n", true);
                throw;
            }
            finally
            {

                if (clearWarnings)
                {
                    foreach (var item in Warnings)
                    {
                        ConsoleExt.WriteLine("Warning! : " + item, ConsoleColor.Red);

                    }
                    Warnings.Clear();
                }
            }
        }

        public List<string> Warnings { get; set; } = new List<string>();
        public int SkipFor { get; private set; }

        public void AddWarning(string warn)
        {
            Warnings.Add($"{ThisLine} > {warn}");
        }

        private void Expect(string[] coms, int max = -1, int min = -1)
        {
            var actual = coms.Length;
            var tooMany = max != -1 && max < actual;
            var tooFew = min != -1 && actual < min;
            if (min == max && (tooMany || tooFew)) throw new ArgumentException($"Wrong argument number, expected {max - 1}, got {actual - 1}");
            if (tooMany) throw new ArgumentException($"Too many arguments, expected {max - 1}, got {actual - 1}");
            if (tooFew) throw new ArgumentException($"Not enough arguments, expected at least {min - 1}, got {actual - 1}");

        }


        private void Clear(string thing)
        {
            switch (thing)
            {
                case "file": //truncate the output file
                    IO.Clear();
                    break;
                case "tags": //clear custom tags
                    Storage.Clear();
                    break;
                case "vars": //clear variables
                    Storage.GlobalVars.Clear();
                    break;
                case "to": //forget the output file
                    IO.OutputFile = null;
                    break;
                case "console": //clear the screen
                    Console.Clear();
                    break;
                case "pointer": //forget what is being written to (X = 0)
                    Storage.Focuses.Clear();
                    break;
                case "layby": //forget what is being written (Y = 0)
                    Storage.Writers.Clear();
                    break;
                case "default": //forget the default tag setting
                    Storage.Default = null;
                    break;
                case "skip": //forget the skip rule
                    SkipFor = 0;
                    break;
                default:
                    throw new ArgumentException("Unknown deletion target");
            }
        }

        private void Else()
        {
            DoNextLine = !IfStatementReturned;
            if (!DoNextLine) ConsoleExt.WriteLine("Logic : True > Skip Else clause!", ConsoleColor.Red);
        }

        private string Work(string l, string c, string r)
        {


            var parseL = double.TryParse(l, out double ll);
            var parseR = double.TryParse(r, out double rr);
            if (!parseL) ll = double.NaN;
            if (!parseR) rr = double.NaN;


            var o = l;
            var oo = ll;

            switch (c)
            {
                case "+":
                    oo += rr;
                    break;
                case "-":
                    oo -= rr;
                    break;
                case "<":
                    o += r;
                    break;
                case ">":
                    o = r + l;
                    break;
                case "rem-start":
                    o = l.Substring((int)rr);
                    break;
                case "rem-end":
                    o = l.Remove((int)rr);
                    break;
                case "rem-end_":
                    o = l.Remove(l.Length - (int)rr);
                    break;
                case "*":
                    oo *= rr;
                    break;
                case "/":
                    oo /= rr;
                    break;
                case "mod":
                    oo %= rr;
                    break;
                default:
                    throw new ArgumentException("Unknown operator");
            }

            if (oo != ll) o = oo.ToString();

            return o;


        }

        private void If(string[] coms, bool refs)
        {
            var l = refs ? Storage.GetVar(coms[1]) : coms[1];
            var c = coms[2];
            var r = refs ? Storage.GetVar(coms[3]) : coms[3];

            var parseL = double.TryParse(l, out double ll);
            var parseR = double.TryParse(r, out double rr);
            if (!parseL) ll = double.NaN;
            if (!parseR) rr = double.NaN;


            var o = false;

            switch (c)
            {
                case "=":
                    o = l == r;
                    break;
                case "<>":
                    o = l != r;
                    break;
                case ">":
                    if (!parseL || !parseR)
                        o = ll > rr;
                    break;
                case "<":
                    o = ll < rr;
                    break;
                case ">=":
                    o = ll >= rr;
                    break;
                case "<=":
                    o = ll <= rr;
                    break;
                case "has":
                    o = l.Contains(r);
                    break;
                case "starts":
                    o = l.StartsWith(r);
                    break;
                case "ends":
                    o = l.EndsWith(r);
                    break;
                case "like":
                    o = Regex.Match(l, r).Success;
                    break;
                default:
                    throw new ArgumentException("Unknown operator");
            }

            ConsoleExt.WriteLine($"Logic : {o} < {coms.ToStringConcat(" ")}", ConsoleColor.Magenta);
            IfStatementReturned = o;
            DoNextLine = o;


        }
    }
}
