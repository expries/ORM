using System;

namespace ORM.Core.Models.Exceptions
{
    public class InvalidEntityException : OrmException
    {
        public InvalidEntityException(string message) : base(message)
        {
        }

        public InvalidEntityException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}