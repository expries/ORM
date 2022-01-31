using System.Security.Cryptography;
using FluentAssertions;
using NSubstitute;
using ORM.Core.Interfaces;
using ORM.Core.Loading;
using ORM.Core.Tests.Entities;
using Xunit;

namespace ORM.Core.Tests
{
    public class LazyLoaderTests
    {
        private readonly ILazyLoader _lazyLoader;
        
        public LazyLoaderTests()
        {
            var commandBuilder = Substitute.For<ICommandBuilder>();
            _lazyLoader = new LazyLoader(commandBuilder);
        }

        [Fact]
        public void LoadManyToOne_ForEntity_ReturnsNull()
        {
            var entity = new Product();
            var result = _lazyLoader.LoadManyToOne<Product, Seller>(entity);
            result.Should().BeNull();
        }
        
        [Fact]
        public void LoadOneToMany_ForEntity_ReturnsEmptyList()
        {
            var entity = new Product();
            var result = _lazyLoader.LoadOneToMany<Product, Seller>(entity);
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
        
        [Fact]
        public void LoadManyToMany_ForEntity_ReturnsEntityList()
        {
            var entity = new Product();
            var result = _lazyLoader.LoadManyToMany<Product, Seller>(entity);
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }
}