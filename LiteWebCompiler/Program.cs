namespace LiteWebCompiler
{
    internal class Program
    {
        static Interpreter Interpreter;
        static void Main(string[] args)
        {
            Interpreter = new Interpreter();


            while (true)
            {
                Console.Write(Interpreter.StatusPrefix);
                Run(Console.ReadLine());
            }

            static void Run(string line)
            {
                try
                {
                    Interpreter.Run(line);
                }
                catch (Exception ex)
                {
                }
            }
        }
    }
}