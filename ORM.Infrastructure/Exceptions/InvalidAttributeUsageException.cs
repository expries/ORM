using System;

namespace ORM.Infrastructure.Exceptions
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