using System.Collections.Immutable;
using FizzWare.NBuilder;
using FluentAssertions;
using ORM.Core.Models.Tests.Entities;
using Xunit;

namespace ORM.Core.Models.Tests
{
    public class ColumnTests
    {
        [Fact]
        public void AfterConstruction_PropertyHasKeyAttribute_ColumnIsPrimaryKey()
        {
            var property = typeof(Author).GetProperty(nameof(Author.PersonId));
            var column = new Column(property);
            column.IsPrimaryKey.Should().BeTrue();
            column.Name.Should().Be(nameof(Author.PersonId));
        }
        
        [Fact]
        public void AfterConstruction_PropertyHasColumnAttributes_ColumnTakesOnNameFromAttribute()
        {
            var property = typeof(Author).GetProperty(nameof(Author.Price));
            var column = new Column(property);
            column.Name.Should().Be("PriceColumn");
        }
        
        [Fact]
        public void AfterConstruction_PropertyHasNoAttributes_ColumnTakesOnNameFromProperty()
        {
            var property = typeof(Author).GetProperty(nameof(Author.FirstName));
            var column = new Column(property);
            column.Name.Should().Be(nameof(Author.FirstName));
        }
        
        [Fact]
        public void AfterConstruction_PropertyHasMaxLengthAttribute_ColumnTakesOnMaxLengthFromAttribute()
        {
            var property = typeof(Author).GetProperty(nameof(Author.FirstName));
            var column = new Column(property);
            column.MaxLength.Should().Be(100);
        }
        
        [Fact]
        public void AfterConstruction_PropertyHasNotMappedAttribute_ColumnIsNotMapped()
        {
            var property = typeof(Author).GetProperty(nameof(Author.FullName));
            var column = new Column(property);
            column.IsMapped.Should().BeFalse();
        }
        
        [Fact]
        public void AfterConstruction_PropertyHasUniqueAttribute_ColumnIsUnique()
        {
            var property = typeof(Author).GetProperty(nameof(Author.FirstName));
            var column = new Column(property);
            column.IsUnique.Should().BeTrue();
        }
        
        [Fact]
        public void AfterConstruction_PropertyHasRequiredAttribute_ColumnIsNotNullable()
        {
            var property = typeof(Author).GetProperty(nameof(Author.FirstName));
            var column = new Column(property);
            column.IsNullable.Should().BeFalse();
        }
        
        [Fact]
        public void AfterConstruction_PropertyHasNoRequiredAttribute_ColumnIsNullable()
        {
            var property = typeof(Author).GetProperty(nameof(Author.LastName));
            var column = new Column(property);
            column.IsNullable.Should().BeTrue();
        }
        
        [Fact]
        public void GetValue_PropertyIsNoForeignKey_ReturnsValue()
        {
            var author = Builder<Author>.CreateNew().Build();
            var property = typeof(Author).GetProperty(nameof(Author.PersonId));
            var column = new Column(property);
            object? value = column.GetValue(author);
            value.Should().Be(author.PersonId);
        }
        
        [Fact]
        public void GetValue_PropertyIsForeignKey_ReturnsPrimaryKeyOfReference()
        {
            var author = Builder<Author>
                .CreateNew()
                .Build();
            
            var book = Builder<Book>
                .CreateNew()
                .With(x => x.Author = author)
                .Build();
            
            var pkProperty = typeof(Author).GetProperty(nameof(Author.PersonId));
            var table = new EntityTable(typeof(Author));
            var column = new Column("fk_example", pkProperty.PropertyType, table, isForeignKey: true, isNullable: false);
            object? value = column.GetValue(book);
            value.Should().Be(author.PersonId);
        }
        
        [Fact]
        public void GetValue_PropertyIsForeignKeyAndTableIsNotEntityTable_ReturnsNull()
        {
            var author = Builder<Author>
                .CreateNew()
                .Build();

            var tableA = new EntityTable(typeof(Author));
            var tableB = new EntityTable(typeof(Book));
            var table = new ForeignKeyTable(tableA, tableB);
            
            var column = new Column("fk_example", typeof(int), table, isForeignKey: true, isNullable: false);
            object? value = column.GetValue(author);
            value.Should().BeNull();
        }
    }   
}