using StuffProject.ConsoleExt;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static LiteWebCompiler.Interpreter;
using static System.Net.Mime.MediaTypeNames;

namespace LiteWebCompiler
{
    public interface Handler
    {
        public Interpreter ParentInterpreter { get; }

    }

    public class IOHandler : Handler
    {
        public Interpreter ParentInterpreter { get; private set; }
        private string? _Output;

        public IOHandler(Interpreter interpreter)
        {
            ParentInterpreter = interpreter;
        }

        public string? OutputFile
        {
            get { return _Output; }
            set
            {
                if (_Output != value) ConsoleExt.WriteLine("Change Output! > " + value, ConsoleColor.Green);
                _Output = value;
                ParentInterpreter.Storage.Cache["_g_to"] = value;
            }
        }

        public void OpenFile()
        {
            ConsoleExt.WriteLine($"Open : {OutputFile}", ConsoleColor.Green);

            var open = new Process();
            open.StartInfo.FileName = OutputFile;
            open.StartInfo.UseShellExecute = true;
            open.Start();
        }

        public void OpenEditor()
        {
            ConsoleExt.WriteLine($"Open (edit) : {OutputFile}", ConsoleColor.Green);

            var open = new Process();
            open.StartInfo.FileName = "NOTEPAD";
            open.StartInfo.Arguments = OutputFile;
            open.StartInfo.UseShellExecute = true;
            open.Start();
        }


        public void Write(string text, bool error = false)
        {
            ConsoleExt.WriteLine("Print! : " + OutputFile + " < " + text, error ? ConsoleColor.Red : ConsoleColor.Green);
            if (OutputFile != null) File.AppendAllText(OutputFile, text);
            else ParentInterpreter.AddWarning("No output file specified.");

        }

        public void Clear()
        {
            File.WriteAllText(OutputFile,"");
        }

        internal void Format()
        {
            ConsoleExt.WriteLine("Format! : " + OutputFile + " (using XmlDocument) (markup must be legal!)", ConsoleColor.Green);
            var o = new XmlDocument();
            o.Load(OutputFile);
            o.Save(OutputFile);
            ConsoleExt.WriteLine("Format! : " + OutputFile + " < " + File.ReadAllText(OutputFile), ConsoleColor.Green);
        }
    }
}
