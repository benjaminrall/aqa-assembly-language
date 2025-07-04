# AQA Assembly Language Interpreter

Virtual environment and interpreter made in C# to run programs written using the assembly language specification from AQA A-Level Computer Science.
Specification details can be found [here](https://pmt.physicsandmathstutor.com/download/Computer-Science/A-level/Past-Papers/AQA/AS-Paper-2/Assembly%20Language%20Instruction%20-%20Paper%202%20AQA%20Computer%20Science%20AS-level.pdf). This implementation adds two extra instructions: `IN` and `OUT` for writing and reading values to/from the command line.

### Usage details

The project can be run from the main directory using the following command:
```
> dotnet run --project AssemblyCode
```
This will start an interactive environment, which prompts the user for a path to an assembly script.

Alternatively, a script path can be specified as a command line argument. For example:
```
dotnet run --project AssemblyCode examples/sort.assembly
```

There are three example programs provided [here](./examples)
