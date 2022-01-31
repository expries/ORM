using System;
using System.Linq;
using FluentAssertions;
using ORM.Core.Loading;
using ORM.Core.Models.Enums;
using ORM.Core.Models.Exceptions;
using ORM.Core.Models.Tests.Entities;
using Xunit;

namespace ORM.Core.Models.Tests
{
    public class TableTests
    {
        [Fact]
        public void OnConstruction_NoKeyIsDefinedOnEntity_OrmExceptionIsThrown()
        {
            Func<Table> constructTable = () => new EntityTable(typeof(KeylessPerson));
            constructTable.Should().Throw<OrmException>();
        }
        
        [Fact]
        public void OnConstruction_DuplicateKeyIsDefinedOnEntity_OrmExceptionIsThrown()
        {
            Func<Table> constructTable = () => new EntityTable(typeof(DuplicateKeyPerson));
            constructTable.Should().Throw<OrmException>();
        }

        [Fact]
        public void OnConstruction_EntityTypeIsValueType_OrmExceptionIsThrown()
        {
            Func<Table> constructTable = () => new EntityTable(typeof(int));
            constructTable.Should().Throw<OrmException>();
        }

        [Fact]
        public void AfterConstruction_EntityTypeTypeIsProxy_TypeIsStrippedOfProxy()
        {
            var authorProxy = LazyProxyFactory.CreateProxy<Author>();
            var proxyType = authorProxy.GetType();
            var table = new EntityTable(proxyType);
            table.Type.Should().Be(typeof(Author));
        }

        [Fact]
        public void AfterConstruction_EntityWithInternalFields_AllColumnsAreLoaded()
        {
            var table = new EntityTable(typeof(Author));
            table.Columns.Count.Should().Be(5);
        }
        
        [Fact]
        public void AfterConstruction_EntityWithOneToManyRelationship_RelationshipIsLoaded()
        {
            var table = new EntityTable(typeof(Author));
            table.ExternalFields.Count(x => x.Relationship == RelationshipType.OneToMany).Should().Be(1);
            table.ExternalFields.Count.Should().Be(1);
        }
        
        [Fact]
        public void AfterConstruction_EntityWithManyToOneRelationship_RelationshipIsLoaded()
        {
            var table = new EntityTable(typeof(Book));
            table.ExternalFields.Count(x => x.Relationship == RelationshipType.ManyToOne).Should().Be(1);
            table.ExternalFields.Count.Should().Be(1);
        }
        
        [Fact]
        public void AfterConstruction_EntityWithOneToOneRelationship_RelationshipIsLoaded()
        {
            var table = new EntityTable(typeof(Course));
            table.ExternalFields.Count(x => x.Relationship == RelationshipType.OneToOne).Should().Be(1);
            table.ExternalFields.Count.Should().Be(1);
        }
    }
}