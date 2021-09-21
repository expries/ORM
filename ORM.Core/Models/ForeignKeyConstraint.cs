namespace ORM.Core.Models
{
    public class ForeignKeyConstraint
    {
        public Column ColumnFrom { get; }
        
        public Column ColumnTo { get; }

        public Table TableTo { get; }

        public ForeignKeyConstraint(Column columnFrom, Column columnTo, Table tableTo)
        {
            ColumnFrom = columnFrom;
            ColumnTo = columnTo;
            TableTo = tableTo;
        }
    }
}