using System;

namespace ORM.Core.Attributes
{
    public class MaxLengthAttribute : Attribute
    {
        public int Length { get; set; }
        
        public MaxLengthAttribute(int length)
        {
            Length = length;
        }
    }
}