namespace ORM.Core.Models
{
    public class ForeignKeyConstraint
    {
        public Column ColumnFrom { get; set; }
        
        public Column ColumnTo { get; set; }

        public Table TableTo { get; set; }
    }
}