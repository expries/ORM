using System.Linq;
using ORM.Core.Models.Enums;

namespace ORM.Core.Models
{
    public class ForeignKeyTable : Table
    {
        public EntityTable TableA { get; set; }
        
        public EntityTable TableB { get; set; }
        
        private Column FkColumnA { get; set; }
        
        private Column FkColumnB { get; set; }
        
        private Column KeyTableA { get; set; }
        
        private Column KeyTableB { get; set; }
        
        public ForeignKeyTable(EntityTable tableA, EntityTable tableB) : base($"fk_{tableA.Name}_{tableB.Name}")
        {
            SetTables(tableA, tableB);
            SetPrimaryKeys();
            AddColumns();
            AddForeignKeys();
            AddExternalFields();
            Name = $"fk_{TableA.Name}_{TableB.Name}";
        }

        private void SetTables(EntityTable tableA, EntityTable tableB)
        {
            bool order = tableA.Type.GUID.GetHashCode() > tableB.Type.GUID.GetHashCode();
            TableA = order ? tableA : tableB;
            TableB = order ? tableB : tableA;
        }

        private void SetPrimaryKeys()
        {
            KeyTableA = TableA.Columns.First(_ => _.IsPrimaryKey);
            KeyTableB = TableB.Columns.First(_ => _.IsPrimaryKey);
        }

        private void AddColumns()
        {
            FkColumnA = new Column($"fk_{KeyTableA.Name}", KeyTableA.Type);
            FkColumnB = new Column($"fk_{KeyTableB.Name}", KeyTableB.Type);
            Columns.Add(FkColumnA);
            Columns.Add(FkColumnB);
        }

        private void AddForeignKeys()
        {
            var fkA = new ForeignKey(FkColumnA, KeyTableA, TableA);
            var fkB = new ForeignKey(FkColumnB, KeyTableB, TableB);
            ForeignKeys.Add(fkA);
            ForeignKeys.Add(fkB);
        }

        private void AddExternalFields()
        {
            AddExternalField(TableA, RelationshipType.ManyToOne);
            AddExternalField(TableB, RelationshipType.ManyToOne);
        }
    }
}