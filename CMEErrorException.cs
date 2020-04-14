using System;

namespace BG96Sharp
{
    public class CmeErrorException : Exception
    {
        public CmeErrorException(string message) : base(message) { }
    }
}