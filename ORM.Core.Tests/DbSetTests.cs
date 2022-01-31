using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using NSubstitute;
using ORM.Core.Tests.Entities;
using Xunit;

namespace ORM.Core.Tests
{
    public class DbSetTests
    {
        private readonly IQueryProvider _queryProvider;

        private readonly DbSet<Product> _products;

        public DbSetTests()
        {
            _queryProvider = Substitute.For<IQueryProvider>();
            _products = new DbSet<Product>(_queryProvider);
        }

        [Fact]
        public void AfterConstruction_ForEntityType_HasElementTypeAndConstantEnumeratorOfEntity()
        {
            _products.ElementType.Should().Be(typeof(Product));
        }
        
        [Fact]
        public void OnConstruction_ExpressionIsNotIQueryable_ThrowsArgumentOutOfRangeException()
        {
            var expression = Expression.Constant(this);
            Func<DbSet<Product>> createDbSet = () => new DbSet<Product>(_queryProvider, expression);
            createDbSet.Should().Throw<ArgumentOutOfRangeException>();
        }
        
        [Fact]
        public void OnConstruction_ExpressionIsIQueryable_ThrowsNoException()
        {
            var expression = Expression.Constant(_products);
            Func<DbSet<Product>> createDbSet = () => new DbSet<Product>(_queryProvider, expression);
            createDbSet.Should().NotThrow();
        }
        
        [Fact]
        public void GetEnumerator_ReturnsEnumeratorFromQueryProvider()
        {
            var list = new List<Product>();
            var listEnumerator = list.GetEnumerator();
            _queryProvider.Execute(Arg.Any<Expression>()).Returns(list);
            var enumerator = _products.GetEnumerator();
            enumerator.Should().Be(listEnumerator);
        }

    }
}