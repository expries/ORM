namespace ORM.Core.Models
{
    public class ForeignKey
    {
        public Column ColumnFrom { get; }
        
        public Column ColumnTo { get; }

        public Table TableTo { get; }
        
        public bool IsInheritanceKey { get; }

        public ForeignKey(Column columnFrom, Column columnTo, Table tableTo, bool isInheritanceKey = false)
        {
            ColumnFrom = columnFrom;
            ColumnTo = columnTo;
            TableTo = tableTo;
            IsInheritanceKey = isInheritanceKey;
        }
    }
}