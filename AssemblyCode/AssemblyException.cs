using System;

namespace AssemblyCode
{
    public class AssemblyException : Exception
    {
        public AssemblyException(string message) : base(message) { }
    }
}