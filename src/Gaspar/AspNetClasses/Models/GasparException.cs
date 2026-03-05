using System;

namespace WCKDRZR.Gaspar.Models
{
    public class GasparException : Exception
    {
        public GasparException(string message) : base(message)
        {
        }
    }
}