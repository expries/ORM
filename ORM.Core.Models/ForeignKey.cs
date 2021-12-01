namespace ORM.Core.Models
{
    public class ForeignKey
    {
        public Column ColumnFrom { get; }
        
        public Column ColumnTo { get; }

        public Table TableTo { get; }
        
        public ForeignKey(Column columnFrom, Column columnTo, Table tableTo)
        {
            ColumnFrom = columnFrom;
            ColumnTo = columnTo;
            TableTo = tableTo;
        }
    }
}