using System;

namespace ORM.Core.Exceptions
{
    public class InvalidEntityException : Exception
    {
        public InvalidEntityException(string message) : base(message)
        {
        }

        public InvalidEntityException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}