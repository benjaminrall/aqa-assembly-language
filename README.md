# AQA Assembly Language Interpreter

A virtual environment and interpreter built in C# to execute programs written using the AQA A-Level Computer Science assembly language specification.

![Example usage](https://github.com/user-attachments/assets/f8c12cd0-5556-4f71-b20b-82a02ab3e2fb)

## Key Features
- Full AQA instruction set, following the specification found [here](https://pmt.physicsandmathstutor.com/download/Computer-Science/A-level/Past-Papers/AQA/AS-Paper-2/Assembly%20Language%20Instruction%20-%20Paper%202%20AQA%20Computer%20Science%20AS-level.pdf)
- Extra I/O commands `IN` and `OUT` for reading from and writing to the console
- Robust handling of syntax and runtime errors

## Usage Instructions

First, clone the repository:
```sh
git clone https://github.com/benjaminrall/aqa-assembly-language.git
cd aqa-assembly-interpreter
```

The project can then be run from the main directory using the following command:
```
dotnet run --project AssemblyCode
```
This will start an interactive environment, which continuously prompts the user for a path to an assembly script until they choose to exit.

Alternatively, a script path can be specified as a command line argument. For example:
```
dotnet run --project AssemblyCode examples/sort.assembly
```

Three example programs are provided in the [`./examples`](./examples) directory to demonstrate the interpreter's functionality.

## License
This project is licensed under the **MIT License**. See the `LICENSE` file for details.
