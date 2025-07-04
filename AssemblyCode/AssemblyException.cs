using System;

namespace AssemblyCode
{
    /// <summary>
    /// Represents errors that occur during the parsing or execution of an assembly language program.
    /// This exception is thrown for issues like invalid syntax, unknown instructions, or runtime errors.
    /// </summary>
    public class AssemblyException : Exception
    {
        public AssemblyException(string message) : base(message) { }
    }
}