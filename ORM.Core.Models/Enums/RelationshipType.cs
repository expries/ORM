namespace ORM.Core.Models.Enums
{
    /// <summary>
    /// The type of relationship between entities
    /// </summary>
    public enum RelationshipType
    {
        None,
        OneToOne,
        OneToMany,
        ManyToOne,
        ManyToMany
    }
}