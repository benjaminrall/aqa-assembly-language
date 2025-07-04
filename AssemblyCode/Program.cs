#nullable enable
using System;

namespace AssemblyCode
{
    internal abstract class Program
    {
        public static int Main(string[] args)
        {
            // Creates an AssemblyEnvironment instance
            AssemblyEnvironment env = new();

            // Checks if a command-line argument was provided to decide on the program's execution mode
            if (args.Length > 0)
            {
                RunScriptMode(env, args[0]);
            }
            else
            {
                RunInteractiveMode(env);
            }

            return 0;
        }
        
        /// <summary>
        /// Runs the environment in an interactive mode, prompting the user for script paths.
        /// </summary>
        private static void RunInteractiveMode(AssemblyEnvironment env)
        {
            Console.WriteLine("AQA Assembly Language Environment (Interactive Mode)");
            Console.WriteLine("----------------------------------------------------");

            while (true)
            {
                Console.Write("Enter script path (.assembly) or EXIT :> ");
                string? input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                if (input.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                ExecuteScript(env, input);
            }

            Console.WriteLine("Exiting environment.");
        }

        /// <summary>
        /// Runs the program in script mode using the provided file path.
        /// </summary>
        private static void RunScriptMode(AssemblyEnvironment env, string filePath)
        {
            Console.WriteLine($"Running script from command-line argument: {filePath}");
            ExecuteScript(env, filePath);
        }
        
        /// <summary>
        /// Core logic to load and run a script, handling validation and errors.
        /// </summary>
        private static void ExecuteScript(AssemblyEnvironment env, string filePath)
        {
            if (!filePath.EndsWith(".assembly", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Error: Invalid file type. Please provide a path to a '.assembly' file.");
                return;
            }

            try
            {
                env.LoadProgram(filePath);
                env.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}