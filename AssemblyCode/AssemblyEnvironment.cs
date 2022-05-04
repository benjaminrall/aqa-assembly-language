using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssemblyCode
{
    public class AssemblyEnvironment
    {
        private const int AddressWidth = 8;
        
        private static readonly string[] ValidInstructions = 
        {
            "LDR", // LDR Rd, <memory ref>
            "STR", // STR Rd, <memory ref>
            "ADD", // ADD Rd, Rn, <operand2>
            "SUB", // SUB Rd, Rn, <operand2>
            "MOV", // MOV Rd, <operand2>
            "CMP", // CMP Rn, <operand2>
            "B",   // B <label>
            "BEQ", // BEQ <label>
            "BNE", // BNE <label>
            "BGT", // BGT <label>
            "BLT", // BLT <label>
            "AND", // AND Rd, Rn, <operand2>
            "ORR", // ORR Rd, Rn, <operand2>
            "EOR", // EOR Rd, Rn, <operand2>
            "MVN", // MVN Rd, <operand2>
            "LSL", // LSL Rd, Rn, <operand2>
            "LSR", // LSR Rd, Rn, <operand2>
            "IN",  // IN Rn
            "OUT", // OUT Rd
            "HALT" // HALT
        };

        private bool _programLoaded;
        private int _programCounter;

        private (bool enabled, int a, int b) _lastComparison;
        
        private string[] _program;
        private string[] _instructions;
        private int[] _registers;
        private Dictionary<string, int> _branches;
        
        private static readonly int[] Memory = new int[(int) Math.Pow(2, AddressWidth)];

        private string _loadedFile;
        
        public int GetMemory(int index) => Memory[index];
        public void SetMemory(int index, int value) => Memory[index] = value;
        
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

        public void LoadProgram(string filePath)
        {
            _programLoaded = false;
            
            List<string> program = new();

            try
            {
                using StreamReader sr = new(filePath);

                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    program.Add(line);
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            
            _program = program.ToArray();
            _registers = new int[13];
            _branches = new Dictionary<string, int>();
            _lastComparison = (false, 0, 0);
            
            try
            {
                Initialise();
                _loadedFile = filePath;
                _programLoaded = true;
            }
            catch (AssemblyException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void DisplayCurrentProgram()
        {
            if (_programLoaded)
            {
                Console.WriteLine("Instructions: ");
                for (int i = 0; i < _instructions.Length; i++)
                {
                    Console.WriteLine($"{i} : {_instructions[i]}");
                }

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

        private void Initialise()
        {
            List<string> instructions = new();
            
            int lineNumber = 0;
            
            foreach (string line in _program)
            {
                if (ValidInstructions.Contains(line.Split(' ')[0]))
                {
                    instructions.Add(line.Trim());
                }
                else if (line.EndsWith(":"))
                {
                    string key = line.Remove(line.Length - 1).Trim();
                        
                    if (_branches.ContainsKey(key))
                    {
                        throw new AssemblyException(
                            $"Program cannot contain 2 branches of the same name: {key} on line {lineNumber + 1}");
                    }

                    _branches.Add(key, instructions.Count);
                }
                else if (!line.StartsWith("//") && line != "")
                {
                    throw new AssemblyException(
                        $"Unknown instruction on line {lineNumber + 1} of program: {_program[lineNumber].Split(' ')[0]}");
                }

                lineNumber++;
            }

            _instructions = instructions.ToArray();
        }

        private void Interpret(string line)
        {
            string[] splitLine = line.Split(' ');
            string instruction = splitLine[0];
            string[] args = splitLine.Length > 1
                ? line[line.IndexOf(' ')..].Replace(" ", "").Split(",")
                : Array.Empty<string>();
            switch (instruction)
            {
                case "LDR":
                    Ldr(args);
                    _programCounter++;
                    break;
                case "STR":
                    Str(args);
                    _programCounter++;
                    break;
                case "ADD":
                    Add(args);
                    _programCounter++;
                    break;
                case "SUB":
                    Sub(args);
                    _programCounter++;
                    break;
                case "MOV":
                    Mov(args);
                    _programCounter++;
                    break;
                case "CMP":
                    Cmp(args);
                    _programCounter++;
                    break;
                case "B":
                    Branch(args);
                    break;
                case "BEQ":
                    Beq(args);
                    break;
                case "BNE":
                    Bne(args);
                    break;
                case "BGT":
                    Bgt(args);
                    break;
                case "BLT":
                    Blt(args);
                    break;
                case "AND":
                    And(args);
                    _programCounter++;
                    break;
                case "ORR":
                    Orr(args);
                    _programCounter++;
                    break;
                case "EOR":
                    Eor(args);
                    _programCounter++;
                    break;
                case "MVN":
                    Mvn(args);
                    _programCounter++;
                    break;
                case "LSL":
                    Lsl(args);
                    _programCounter++;
                    break;
                case "LSR":
                    Lsr(args);
                    _programCounter++;
                    break;
                case "IN":
                    In(args);
                    _programCounter++;
                    break;
                case "OUT":
                    Out(args);
                    _programCounter++;
                    break;
                case "HALT":
                    _programCounter = _instructions.Length;
                    break;
            }
        }

        private static int ParseRegister(string arg)
        {
            if (!arg.StartsWith("R") || !int.TryParse(arg[1..], out int register))
            {
                throw new AssemblyException("Argument must be a register, referred to with the form Rn");
            }

            if (register is < 0 or > 12)
            {
                throw new AssemblyException($"{arg} is not a valid register, valid registers are numbered 0-12");
            }

            return register;
        }
        
        private int ParseMemoryLocation(string arg)
        {
            int memLocation;
            if (!arg.StartsWith("R"))
            {
                if (!int.TryParse(arg, out memLocation))
                {
                    throw new AssemblyException("Memory location must be an integer or a register");
                }
            }
            else
            {
                if (!int.TryParse(arg[1..], out int register))
                {
                    throw new AssemblyException(
                        "Memory location must be an integer or a register referred to with the form Rn");
                }
                
                if (register is < 0 or > 12)
                {
                    throw new AssemblyException($"{arg} is not a valid register, valid registers are numbered 0-12");
                }
                
                memLocation = _registers[register];
            }
            
            if (memLocation > Memory.Length - 1 || memLocation < 0)
            {
                throw new AssemblyException(
                    $"{arg} is not a valid memory address, memory addresses must have a width of {AddressWidth} bits");
            }

            return memLocation;
        }

        private int ParseSecondOperand(string arg)
        {
            if (!arg.StartsWith("R") && !arg.StartsWith("#"))
            {
                throw new AssemblyException(
                    "Argument must be either a register of form Rn, or a literal value preceded by a #");
            }

            int operand = 0;

            if (arg.StartsWith("R") && !int.TryParse(arg[1..], out operand))
            {
                throw new AssemblyException("Register argument must be followed by a number");
            }

            if (arg.StartsWith("R") && operand is < 0 or > 12)
            {
                throw new AssemblyException($"{arg} is not a valid register, valid registers are numbered 0-12");
            }

            if (arg.StartsWith("#") && !int.TryParse(arg[1..], out operand))
            {
                throw new AssemblyException("Literal argument must be followed by a number");
            }

            return arg.StartsWith("R") ? _registers[operand] : operand;
        }

        private void Ldr(string[] args)
        {
            if (args.Length != 2)
            {
                throw new AssemblyException($"LDR instruction takes 2 arguments but {args.Length} were given");
            }

            int register = ParseRegister(args[0]);
            int memLocation = ParseMemoryLocation(args[1]);

            _registers[register] = Memory[memLocation];
        }   

        private void Str(string[] args)
        {
            if (args.Length != 2)
            {
                throw new AssemblyException($"STR instruction takes 2 arguments but {args.Length} were given");
            }

            int register = ParseRegister(args[0]);
            int memLocation = ParseMemoryLocation(args[1]);

            Memory[memLocation] = _registers[register];
        }

        private void Add(string[] args)
        {
            if (args.Length != 3)
            {
                throw new AssemblyException($"ADD instruction takes 3 arguments but {args.Length} were given");
            }

            int registerD = ParseRegister(args[0]);
            int registerN = ParseRegister(args[1]);
            int operand2 = ParseSecondOperand(args[2]);

            _registers[registerD] = _registers[registerN] + operand2;
        }

        private void Sub(string[] args)
        {
            if (args.Length != 3)
            {
                throw new AssemblyException($"SUB instruction takes 3 arguments but {args.Length} were given");
            }

            int registerD = ParseRegister(args[0]);
            int registerN = ParseRegister(args[1]);
            int operand2 = ParseSecondOperand(args[2]);

            _registers[registerD] = _registers[registerN] - operand2;
        }

        private void Mov(string[] args)
        {
            if (args.Length != 2)
            {
                throw new AssemblyException($"MOV instruction takes 2 arguments but {args.Length} were given");
            }

            int register = ParseRegister(args[0]);
            int operand2 = ParseSecondOperand(args[1]);

            _registers[register] = operand2;
        }
        
        private void Cmp(string[] args)
        {
            if (args.Length != 2)
            {
                throw new AssemblyException($"CMP instruction takes 2 arguments but {args.Length} were given");
            }

            int register = ParseRegister(args[0]);
            int operand2 = ParseSecondOperand(args[1]);

            _lastComparison = (true, _registers[register], operand2);
        }
        
        private void Branch(string[] args)
        {
            if (args.Length != 1)
            {
                throw new AssemblyException($"B instruction takes 1 argument but {args.Length} were given");
            }

            if (!_branches.ContainsKey(args[0]))
            {
                throw new AssemblyException($"No branch with name '{args[0]}' found");
            }

            _programCounter = _branches[args[0]];
        }
        
        private void Beq(string[] args)
        {
            if (args.Length != 1)
            {
                throw new AssemblyException($"BEQ instruction takes 1 argument but {args.Length} were given");
            }
            
            if (!_branches.ContainsKey(args[0]))
            {
                throw new AssemblyException($"No branch with name '{args[0]}' found");
            }

            if (!_lastComparison.enabled)
            {
                throw new AssemblyException("No comparison has been made");
            }

            _programCounter = _lastComparison.a == _lastComparison.b
                ? _branches[args[0]]
                : _programCounter + 1;
        }
        
        private void Bne(string[] args)
        {
            if (args.Length != 1)
            {
                throw new AssemblyException($"BNE instruction takes 1 argument but {args.Length} were given");
            }
            
            if (!_branches.ContainsKey(args[0]))
            {
                throw new AssemblyException($"No branch with name '{args[0]}' found");
            }
            
            if (!_lastComparison.enabled)
            {
                throw new AssemblyException("No comparison has been made");
            }

            _programCounter = _lastComparison.a != _lastComparison.b
                ? _branches[args[0]]
                : _programCounter + 1;
        }
        
        private void Bgt(string[] args)
        {
            if (args.Length != 1)
            {
                throw new AssemblyException($"BGT instruction takes 1 argument but {args.Length} were given");
            }
            
            if (!_branches.ContainsKey(args[0]))
            {
                throw new AssemblyException($"No branch with name '{args[0]}' found");
            }
            
            if (!_lastComparison.enabled)
            {
                throw new AssemblyException("No comparison has been made");
            }

            _programCounter = _lastComparison.a > _lastComparison.b
                ? _branches[args[0]]
                : _programCounter + 1;
        }
        
        private void Blt(string[] args)
        {
            if (args.Length != 1)
            {
                throw new AssemblyException($"BLT instruction takes 1 argument but {args.Length} were given");
            }
            
            if (!_branches.ContainsKey(args[0]))
            {
                throw new AssemblyException($"No branch with name '{args[0]}' found");
            }
            
            if (!_lastComparison.enabled)
            {
                throw new AssemblyException("No comparison has been made");
            }

            _programCounter = _lastComparison.a < _lastComparison.b
                ? _branches[args[0]]
                : _programCounter + 1;
        }
        
        private void And(string[] args)
        {
            if (args.Length != 3)
            {
                throw new AssemblyException($"AND instruction takes 3 arguments but {args.Length} were given");
            }

            int registerD = ParseRegister(args[0]);
            int registerN = ParseRegister(args[1]);
            int operand2 = ParseRegister(args[2]);

            _registers[registerD] = _registers[registerN] & operand2;
        }
        
        private void Orr(string[] args)
        {
            if (args.Length != 3)
            {
                throw new AssemblyException($"ORR instruction takes 3 arguments but {args.Length} were given");
            }
            
            int registerD = ParseRegister(args[0]);
            int registerN = ParseRegister(args[1]);
            int operand2 = ParseRegister(args[2]);

            _registers[registerD] = _registers[registerN] | operand2;
        }
        
        private void Eor(string[] args)
        {
            if (args.Length != 3)
            {
                throw new AssemblyException($"EOR instruction takes 3 arguments but {args.Length} were given");
            }
            
            int registerD = ParseRegister(args[0]);
            int registerN = ParseRegister(args[1]);
            int operand2 = ParseRegister(args[2]);

            _registers[registerD] = _registers[registerN] ^ operand2;
        }
        
        private void Mvn(string[] args)
        {
            if (args.Length != 2)
            {
                throw new AssemblyException($"MVN instruction takes 2 arguments but {args.Length} were given");
            }

            int register = ParseRegister(args[0]);
            int operand2 = ParseSecondOperand(args[1]);

            _registers[register] = ~operand2;
        }
        
        private void Lsl(string[] args)
        {
            if (args.Length != 3)
            {
                throw new AssemblyException($"LSL instruction takes 3 arguments but {args.Length} were given");
            }

            int registerD = ParseRegister(args[0]);
            int registerN = ParseRegister(args[1]);
            int operand2 = ParseSecondOperand(args[2]);

            _registers[registerD] = _registers[registerN] << operand2;
        }
        
        private void Lsr(string[] args)
        {
            if (args.Length != 3)
            {
                throw new AssemblyException($"LSR instruction takes 3 arguments but {args.Length} were given");
            }
            
            int registerD = ParseRegister(args[0]);
            int registerN = ParseRegister(args[1]);
            int operand2 = ParseSecondOperand(args[2]);

            _registers[registerD] = _registers[registerN] >> operand2;
        }
        
        private void In(string[] args)
        {
            if (args.Length != 1)
            {
                throw new AssemblyException($"IN instruction takes 1 argument but {args.Length} were given");
            }

            int register = ParseRegister(args[0]);
            
            Console.Write("> ");
            string result = Console.ReadLine();

            if (!int.TryParse(result, out int input))
            {
                throw new AssemblyException("Input must be an integer.");
            }

            _registers[register] = input;
        }
        
        private void Out(string[] args)
        {
            if (args.Length != 1)
            {
                throw new AssemblyException($"OUT instruction takes 1 argument but {args.Length} were given");
            }

            int register = ParseRegister(args[0]);
            
            Console.WriteLine(_registers[register]);
        }
        
        public bool Run()
        {
            if (_programLoaded)
            {
                _programCounter = 0;
                try
                {
                    while (_programCounter < _instructions.Length)
                    {
                        Interpret(_instructions[_programCounter]);
                    }

                    return true;
                }
                catch (AssemblyException e)
                {
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