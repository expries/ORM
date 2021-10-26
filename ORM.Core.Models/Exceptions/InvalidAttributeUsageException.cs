using System;

namespace ORM.Core.Models.Exceptions
{
    public class InvalidAttributeUsageException : OrmException
    {
        public InvalidAttributeUsageException(string message) : base(message)
        {
        }

        public InvalidAttributeUsageException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}