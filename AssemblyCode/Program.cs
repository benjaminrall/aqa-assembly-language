using System;

namespace AssemblyCode
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            AssemblyEnvironment env = new ();
            
            while (true)
            {
                Console.Write("Enter script path (.assembly) or EXIT :> ");
                string filePath = Console.ReadLine();

                if (filePath == null)
                {
                    Console.WriteLine("Null file path.");
                    return -1;
                }
                
                if (filePath.ToUpper() == "EXIT")
                {
                    break;
                }
                
                if (!filePath.EndsWith(".assembly"))
                {
                    Console.WriteLine("Invalid file path entered.");
                    continue;
                }

                env.LoadProgram(filePath);

                env.Run();
            }

            return 0;
        }
    }
}