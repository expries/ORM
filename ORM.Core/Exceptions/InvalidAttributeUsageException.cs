using System;

namespace ORM.Core.Exceptions
{
    public class InvalidAttributeUsageException : Exception
    {
        public InvalidAttributeUsageException(string message) : base(message)
        {
        }

        public InvalidAttributeUsageException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}