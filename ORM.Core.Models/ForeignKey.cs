namespace ORM.Core.Models
{
    /// <summary>
    /// Represents a foreign key of a table
    /// </summary>
    public class ForeignKey
    {
        /// <summary>
        /// Column of the current table that the constraint is applied on
        /// </summary>
        public Column LocalColumn { get; }
        
        /// <summary>
        /// Column of another table that the constraint is pointing towards 
        /// </summary>
        public Column RemoteColumn { get; }

        /// <summary>
        /// Table that contains the remote column
        /// </summary>
        public EntityTable RemoteTable { get; }
        
        public ForeignKey(Column localColumn, Column remoteColumn, EntityTable remoteTable)
        {
            LocalColumn = localColumn;
            RemoteColumn = remoteColumn;
            RemoteTable = remoteTable;
        }
    }
}