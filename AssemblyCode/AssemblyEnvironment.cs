using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssemblyCode
{
    /// <summary>
    /// Represents a virtual environment for executing programs written in the AQA assembly language.
    /// Handles program loading, parsing, and interpretation.
    /// </summary>
    public class AssemblyEnvironment
    {
        // Defines the number of bits for a memory address
        private const int AddressWidth = 8;
        
        // A static array of all valid instruction mnemonics present in the language
        private static readonly string[] ValidInstructions = 
        {
            "LDR", // LDR Rd, <memory ref>      - Loads a value from memory into a register
            "STR", // STR Rd, <memory ref>      - Stores a value from a register into memory
            "ADD", // ADD Rd, Rn, <operand2>    - Performs addition
            "SUB", // SUB Rd, Rn, <operand2>    - Performs subtraction
            "MOV", // MOV Rd, <operand2>        - Copies a value into a register
            "CMP", // CMP Rn, <operand2>        - Compares two values and sets branching flags
            "B",   // B <label>                 - Branches unconditionally
            "BEQ", // BEQ <label>               - Branches if the last comparison was equal
            "BNE", // BNE <label>               - Branches if the last comparison was not equal
            "BGT", // BGT <label>               - Branches if the last comparison was greater than
            "BLT", // BLT <label>               - Branches if the last comparison was less than
            "AND", // AND Rd, Rn, <operand2>    - Performs bitwise logical AND
            "ORR", // ORR Rd, Rn, <operand2>    - Performs bitwise logical OR
            "EOR", // EOR Rd, Rn, <operand2>    - Performs bitwise logical XOR
            "MVN", // MVN Rd, <operand2>        - Performs bitwise logical NOT
            "LSL", // LSL Rd, Rn, <operand2>    - Performs a logical shift left
            "LSR", // LSR Rd, Rn, <operand2>    - Performs a logical shift right
            "IN",  // IN Rn                     - Reads an integer from console input into a register
            "OUT", // OUT Rd                    - Prints the value of a register to the console
            "HALT" // HALT                      - Stops program execution
        };

        private bool _programLoaded; // Flag indicating if a program has been successfully loaded
        private int _programCounter; // Points to the index of the current instruction

        private (bool enabled, int a, int b) _lastComparison; // Stores the details of the last CMP operation
        
        private string[] _program; // Raw lines of code loaded from the file
        private string[] _instructions; // Parsed, executable instruction lines with labels and comments removed
        private int[] _registers; // Values of CPU registers (R0-R12)
        private Dictionary<string, int> _branches;  // Maps label names to instruction indices for branching
        
        // Simulated system memory
        private readonly int[] _memory = new int[(int) Math.Pow(2, AddressWidth)];

        // The file path of the currently loaded program, for error reporting
        private string _loadedFile;
        
        /// <summary>
        /// Gets the value at a specific memory address.
        /// </summary>
        /// <param name="index">The memory address to read from.</param>
        /// <returns>The integer value stored at that address.</returns>
        public int GetMemory(int index) => _memory[index];
        
        /// <summary>
        /// Sets the value at a specific memory address.
        /// </summary>
        /// <param name="index">The memory address to write to.</param>
        /// <param name="value">The integer value to store.</param>
        public void SetMemory(int index, int value) => _memory[index] = value;
        
        /// <summary>
        /// Initialises a new instance of the `AssemblyEnvironment`.
        /// </summary>
        /// <param name="fileName">Optional path to a .assembly file to load upon creation.</param>
        public AssemblyEnvironment(string fileName = null)
        {
            if (fileName != null)
            {
                LoadProgram(fileName);
            }
            else
            {
                _programLoaded = false;
            }
        }

        /// <summary>
        /// Loads and prepares an assembly program from the specified file path.
        /// Resets the environment's state before loading the new program.
        /// </summary>
        /// <param name="filePath">Path to a .assembly file.</param>
        public void LoadProgram(string filePath)
        {
            _programLoaded = false;
            List<string> program = new();

            try
            {
                // Uses a stream reader to read the file line by line
                using StreamReader sr = new(filePath);

                // Reads until the end of file, adding to the program list
                while (sr.ReadLine() is { } line)
                {
                    program.Add(line);
                }
            }
            catch (IOException e)
            {
                // Displays an error message and exits if the file cannot be read
                Console.WriteLine(e.Message);
                return; 
            }
            
            // Resets environment registers and branch and comparison information
            _program = program.ToArray();
            _registers = new int[13];
            _branches = new Dictionary<string, int>();
            _lastComparison = (false, 0, 0);
            
            try
            {
                // Attempts to initialise the environment's instructions from the raw program lines
                Initialise();
                _loadedFile = filePath;
                _programLoaded = true;
            }
            catch (AssemblyException e)
            {
                // Catches any parsing errors from initialisation
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Displays the parsed instructions and identified branch labels for the currently loaded program.
        /// Useful for debugging.
        /// </summary>
        public void DisplayCurrentProgram()
        {
            if (_programLoaded)
            {
                // Displays the list of parsed instructions
                Console.WriteLine("Instructions: ");
                for (int i = 0; i < _instructions.Length; i++)
                {
                    Console.WriteLine($"{i} : {_instructions[i]}");
                }

                // Displays the list of branch names and corresponding instruction indices
                Console.WriteLine("Branches: ");
                foreach ((string key, int value) in _branches)
                {
                    Console.WriteLine($"{key} : {value}");
                }
            }
            else
            {
                Console.WriteLine("No program currently loaded.");
            }
        }

        /// <summary>
        /// Initialises the environment's instructions using the currently loaded raw program lines.
        /// Filters out non-instruction lines, and stores the index of each branch label.
        /// </summary>
        /// <exception cref="AssemblyException"></exception>
        private void Initialise()
        {
            List<string> instructions = new();
            int lineNumber = 0;
            
            foreach (string line in _program)
            {
                // Removes leading and trailing whitespace from the line
                string trimmedLine = line.Trim();
                
                // Ignores empty lines or lines that are just comments
                if (string.IsNullOrWhiteSpace(line) || trimmedLine.StartsWith("//"))
                {
                    lineNumber++;
                    continue;
                }

                // Gets the potential instruction code from the start of the line
                string potentialInstruction = trimmedLine.Split(' ')[0];
                
                if (ValidInstructions.Contains(potentialInstruction))
                {
                    // If the line starts with a valid instruction, add it to the list of executable instructions
                    instructions.Add(trimmedLine);
                }
                else if (line.EndsWith(":"))
                {
                    // Otherwise, if the line ends with a colon it's a branch label
                    string key = trimmedLine.Remove(trimmedLine.Length - 1).Trim();
                        
                    // Ensures that two branches of the same name are not used
                    if (_branches.ContainsKey(key))
                    {
                        throw new AssemblyException(
                            $"Program cannot contain 2 branches of the same name: '{key}' on line {lineNumber + 1}");
                    }

                    // Maps the label to the next instruction's index
                    _branches.Add(key, instructions.Count);
                }
                else
                {
                    // If it's not whitespace, a comment, a label, or a valid instruction, it's an error
                    throw new AssemblyException(
                        $"Unknown instruction or syntax error on line {lineNumber + 1}: '{_program[lineNumber]}'");
                }

                // Increments the line number
                lineNumber++;
            }

            // Stores the loaded instructions in the environment
            _instructions = instructions.ToArray();
        }

        /// <summary>
        /// Core interpretation method for single instruction.
        /// Decodes the instruction and calls the appropriate execution method.
        /// </summary>
        /// <param name="instruction"></param>
        private void Interpret(string instruction)
        {
            // Extracts the instruction mnemonic from the line
            string[] splitInstruction = instruction.Split(' ', 2);
            string mnemonic = splitInstruction[0];
            
            // Extracts the instruction arguments from the line
            string[] args = splitInstruction.Length > 1
                ? splitInstruction[1].Replace(" ", "").Split(",")
                : Array.Empty<string>();
            
            // Main instruction execution switch
            switch (mnemonic)
            {
                case "LDR": Ldr(args); _programCounter++; break;
                case "STR": Str(args); _programCounter++; break;
                case "ADD": Add(args); _programCounter++; break;
                case "SUB": Sub(args); _programCounter++; break;
                case "MOV": Mov(args); _programCounter++; break;
                case "CMP": Cmp(args); _programCounter++; break;
                case "B":   Branch(args); break;
                case "BEQ": Beq(args); break;
                case "BNE": Bne(args); break;
                case "BGT": Bgt(args); break;
                case "BLT": Blt(args); break;
                case "AND": And(args); _programCounter++; break;
                case "ORR": Orr(args); _programCounter++; break;
                case "EOR": Eor(args); _programCounter++; break;
                case "MVN": Mvn(args); _programCounter++; break;
                case "LSL": Lsl(args); _programCounter++; break;
                case "LSR": Lsr(args); _programCounter++; break;
                case "IN":  In(args);  _programCounter++; break;
                case "OUT": Out(args); _programCounter++; break;
                case "HALT":_programCounter = _instructions.Length; break; // Halts by moving PC to the end of the program
            }
        }

        /// <summary>
        /// Parses a register index from an argument.
        /// </summary>
        /// <param name="arg">The argument string, e.g., "R5".</param>
        /// <returns>The integer index of the register (0-12).</returns>
        /// <exception cref="AssemblyException">Thrown if the format is invalid or the register is out of range.</exception>
        private static int ParseRegister(string arg)
        {
            if (!arg.StartsWith("R") || !int.TryParse(arg[1..], out int register))
            {
                throw new AssemblyException($"Invalid register format. Expected 'Rn', but got '{arg}'.");
            }

            if (register is < 0 or > 12)
            {
                throw new AssemblyException($"{arg} is not a valid register. Valid registers are R0-R12.");
            }

            return register;
        }
        
        /// <summary>
        /// Parses a memory address from an argument.
        /// Memory addresses can be given directly as an integer, or indirectly as a register, in which case
        /// the value stored in the register is returned as the memory address.
        /// </summary>
        /// <param name="arg">The argument string, e.g., "17" or "R3"</param>
        /// <returns>The parsed integer memory address.</returns>
        /// <exception cref="AssemblyException">Thrown if the format is invalid or the memory address is out of range.</exception>
        private int ParseMemoryAddress(string arg)
        {
            int memLocation;
            
            // Checks if the argument is a register (indirect addressing)
            if (arg.StartsWith("R"))
            {
                int register = ParseRegister(arg);
                memLocation = _registers[register];
            }
            // Otherwise, treats the argument as a direct address
            else if (!int.TryParse(arg, out memLocation))
            {
                throw new AssemblyException($"Invalid memory location format. Expected an integer or a register, but got '{arg}'.");
            }
            
            // Validates the final address against the environment's memory size
            if (memLocation >= _memory.Length || memLocation < 0)
            {
                throw new AssemblyException(
                    $"Memory address {memLocation} is out of bounds. Valid addresses are 0-{_memory.Length - 1}.");
            }

            return memLocation;
        }

        /// <summary>
        /// Parses the second operand, which can be an immediate integer value or a value from a register.
        /// </summary>
        /// <param name="arg">The argument string, e.g., "#4" or "R8".</param>
        /// <returns>The parsed second operand value.</returns>
        /// <exception cref="AssemblyException">Thrown if the format is invalid.</exception>
        private int ParseSecondOperand(string arg)
        {
            if (arg.StartsWith("#"))
            {
                // Parses immediate values
                if (int.TryParse(arg[1..], out int literal))
                {
                    return literal;
                }
                throw new AssemblyException($"Invalid literal value format: '{arg}'.");
            }

            if (!arg.StartsWith("R"))
                throw new AssemblyException(
                    $"Invalid operand format. Expected a register 'Rn' or a literal '#value', but got '{arg}'.");
            
            // Parses value from a register
            int register = ParseRegister(arg);
            return _registers[register];

        }

        /// <summary>Executes the LDR instruction: Loads a value from memory into a register.</summary>
        private void Ldr(string[] args)
        {
            if (args.Length != 2) throw new AssemblyException($"LDR expects 2 arguments, but got {args.Length}.");
            int register = ParseRegister(args[0]);
            int memLocation = ParseMemoryAddress(args[1]);
            _registers[register] = _memory[memLocation];
        }   

        /// <summary>Executes the STR instruction:Stores a value from a register into memory.</summary>
        private void Str(string[] args)
        {
            if (args.Length != 2) throw new AssemblyException($"STR expects 2 arguments, but got {args.Length}.");
            int register = ParseRegister(args[0]);
            int memLocation = ParseMemoryAddress(args[1]);
            _memory[memLocation] = _registers[register];
        }

        /// <summary>Executes the ADD instruction: Performs addition.</summary>
        private void Add(string[] args)
        {
            if (args.Length != 3) throw new AssemblyException($"ADD expects 3 arguments, but got {args.Length}.");
            int registerD = ParseRegister(args[0]);
            int registerN = ParseRegister(args[1]);
            int operand2 = ParseSecondOperand(args[2]);
            _registers[registerD] = _registers[registerN] + operand2;
        }

        /// <summary>Executes the SUB instruction: Performs subtraction.</summary>
        private void Sub(string[] args)
        {
            if (args.Length != 3) throw new AssemblyException($"SUB expects 3 arguments, but got {args.Length}.");
            int registerD = ParseRegister(args[0]);
            int registerN = ParseRegister(args[1]);
            int operand2 = ParseSecondOperand(args[2]);
            _registers[registerD] = _registers[registerN] - operand2;
        }

        /// <summary>Executes the MOV instruction: Copies a value into a register.</summary>
        private void Mov(string[] args)
        {
            if (args.Length != 2) throw new AssemblyException($"MOV expects 2 arguments, but got {args.Length}.");
            int register = ParseRegister(args[0]);
            int operand2 = ParseSecondOperand(args[1]);
            _registers[register] = operand2;
        }
        
        /// <summary>Executes the CMP instruction: Compares two values and sets branching flags.</summary>
        private void Cmp(string[] args)
        {
            if (args.Length != 2) throw new AssemblyException($"CMP expects 2 arguments, but got {args.Length}.");
            int register = ParseRegister(args[0]);
            int operand2 = ParseSecondOperand(args[1]);
            _lastComparison = (true, _registers[register], operand2);
        }
        
        /// <summary>Executes the B instruction: Branches unconditionally.</summary>
        private void Branch(string[] args)
        {
            if (args.Length != 1) throw new AssemblyException($"B expects 1 argument, but got {args.Length}.");
            if (!_branches.TryGetValue(args[0], out int targetAddress))
            {
                throw new AssemblyException($"Branch label '{args[0]}' not found.");
            }
            _programCounter = targetAddress;
        }
        
        /// <summary>Executes the BEQ instruction: Branches if the last comparison was equal.</summary>
        private void Beq(string[] args)
        {
            if (args.Length != 1) throw new AssemblyException($"BEQ expects 1 argument, but got {args.Length}.");
            if (!_lastComparison.enabled) throw new AssemblyException("Cannot BEQ without a preceding CMP instruction.");
            if (!_branches.TryGetValue(args[0], out int targetAddress)) throw new AssemblyException($"Branch label '{args[0]}' not found.");
            
            // If condition met, jump. Otherwise, continue to the next instruction.
            _programCounter = _lastComparison.a == _lastComparison.b ? targetAddress : _programCounter + 1;
        }
        
        /// <summary>Executes the BNE instruction: Branches if the last comparison was not equal.</summary>
        private void Bne(string[] args)
        {
            if (args.Length != 1) throw new AssemblyException($"BNE expects 1 argument, but got {args.Length}.");
            if (!_lastComparison.enabled) throw new AssemblyException("Cannot BNE without a preceding CMP instruction.");
            if (!_branches.TryGetValue(args[0], out int targetAddress)) throw new AssemblyException($"Branch label '{args[0]}' not found.");

            _programCounter = _lastComparison.a != _lastComparison.b ? targetAddress : _programCounter + 1;
        }
        
        /// <summary>Executes the BGT instruction: Branches if the last comparison was greater than.</summary>
        private void Bgt(string[] args)
        {
            if (args.Length != 1) throw new AssemblyException($"BGT expects 1 argument, but got {args.Length}.");
            if (!_lastComparison.enabled) throw new AssemblyException("Cannot BGT without a preceding CMP instruction.");
            if (!_branches.TryGetValue(args[0], out int targetAddress)) throw new AssemblyException($"Branch label '{args[0]}' not found.");

            _programCounter = _lastComparison.a > _lastComparison.b ? targetAddress : _programCounter + 1;
        }
        
        /// <summary>Executes the BLT instruction: Branches if the last comparison was less than.</summary>
        private void Blt(string[] args)
        {
            if (args.Length != 1) throw new AssemblyException($"BLT expects 1 argument, but got {args.Length}.");
            if (!_lastComparison.enabled) throw new AssemblyException("Cannot BLT without a preceding CMP instruction.");
            if (!_branches.TryGetValue(args[0], out int targetAddress)) throw new AssemblyException($"Branch label '{args[0]}' not found.");

            _programCounter = _lastComparison.a < _lastComparison.b ? targetAddress : _programCounter + 1;
        }
        
        /// <summary>Executes the AND instruction: Performs bitwise logical AND.</summary>
        private void And(string[] args)
        {
            if (args.Length != 3) throw new AssemblyException($"AND expects 3 arguments, but got {args.Length}.");
            int registerD = ParseRegister(args[0]);
            int registerN = ParseRegister(args[1]);
            int operand2 = ParseSecondOperand(args[2]); // Note: Original code parsed this as a register, corrected to be a second operand.
            _registers[registerD] = _registers[registerN] & operand2;
        }
        
        /// <summary>Executes the ORR instruction: Performs bitwise logical OR.</summary>
        private void Orr(string[] args)
        {
            if (args.Length != 3) throw new AssemblyException($"ORR expects 3 arguments, but got {args.Length}.");
            int registerD = ParseRegister(args[0]);
            int registerN = ParseRegister(args[1]);
            int operand2 = ParseSecondOperand(args[2]); // Note: Original code parsed this as a register, corrected to be a second operand.
            _registers[registerD] = _registers[registerN] | operand2;
        }
        
        /// <summary>Executes the EOR instruction: Performs bitwise logical XOR.</summary>
        private void Eor(string[] args)
        {
            if (args.Length != 3) throw new AssemblyException($"EOR expects 3 arguments, but got {args.Length}.");
            int registerD = ParseRegister(args[0]);
            int registerN = ParseRegister(args[1]);
            int operand2 = ParseSecondOperand(args[2]); // Note: Original code parsed this as a register, corrected to be a second operand.
            _registers[registerD] = _registers[registerN] ^ operand2;
        }
        
        /// <summary>Executes the MVN instruction: Performs bitwise logical NOT.</summary>
        private void Mvn(string[] args)
        {
            if (args.Length != 2) throw new AssemblyException($"MVN expects 2 arguments, but got {args.Length}.");
            int register = ParseRegister(args[0]);
            int operand2 = ParseSecondOperand(args[1]);
            _registers[register] = ~operand2;
        }
        
        /// <summary>Executes the LSL instruction: Performs a logical shift left.</summary>
        private void Lsl(string[] args)
        {
            if (args.Length != 3) throw new AssemblyException($"LSL expects 3 arguments, but got {args.Length}.");
            int registerD = ParseRegister(args[0]);
            int registerN = ParseRegister(args[1]);
            int operand2 = ParseSecondOperand(args[2]);
            _registers[registerD] = _registers[registerN] << operand2;
        }
        
        /// <summary>Executes the LSR instruction: Performs a logical shift right.</summary>
        private void Lsr(string[] args)
        {
            if (args.Length != 3) throw new AssemblyException($"LSR expects 3 arguments, but got {args.Length}.");
            int registerD = ParseRegister(args[0]);
            int registerN = ParseRegister(args[1]);
            int operand2 = ParseSecondOperand(args[2]);
            _registers[registerD] = _registers[registerN] >> operand2;
        }
        
        /// <summary>Executes the IN instruction: Reads an integer from console input into a register.</summary>
        private void In(string[] args)
        {
            if (args.Length != 1) throw new AssemblyException($"IN expects 1 argument, but got {args.Length}.");
            int register = ParseRegister(args[0]);
            
            Console.Write("> ");
            string result = Console.ReadLine();

            if (!int.TryParse(result, out int input))
            {
                throw new AssemblyException("Input must be a valid 32-bit integer.");
            }
            _registers[register] = input;
        }
        
        /// <summary>Executes the OUT instruction: Prints the value of a register to the console.</summary>
        private void Out(string[] args)
        {
            if (args.Length != 1) throw new AssemblyException($"OUT expects 1 argument, but got {args.Length}.");
            int register = ParseRegister(args[0]);
            Console.WriteLine(_registers[register]);
        }
        
        /// <summary>
        /// Runs the loaded program from start to finish or until a HALT instruction is reached.
        /// </summary>
        /// <returns>True if the program completes successfully, false if a runtime error occurs or no program is loaded.</returns>
        public bool Run()
        {
            if (_programLoaded)
            {
                // Resets the program counter to start execution
                _programCounter = 0;
                
                try
                {
                    // Main execution loop - continues as long as the PC is within the bounds of the instruction list
                    while (_programCounter < _instructions.Length)
                    {
                        Interpret(_instructions[_programCounter]);
                    }

                    return true;
                }
                catch (AssemblyException e)
                {
                    // If a runtime error occurs, print a formatted traceback.
                    Console.WriteLine("Traceback:");
                    Console.WriteLine($"> File \"{_loadedFile}\", line {_programCounter + 1}");
                    Console.WriteLine($"RuntimeError: {e.Message}");
                    return false;
                }
            }

            Console.WriteLine("No program currently loaded.");
            return false;
        }
    }
}