using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NSubstitute;
using ORM.Core.Interfaces;
using ORM.Core.Models;
using ORM.Core.Models.Exceptions;
using ORM.Core.Tests.DbContexts;
using ORM.Core.Tests.Entities;
using Xunit;

namespace ORM.Core.Tests
{
    public class DbContextTests
    {
        private TestContext _dbContext;

        private ICache _cache;

        private ICommandBuilder _commandBuilder;

        private IQueryProvider _queryProvider;

        public DbContextTests()
        {
            _cache = Substitute.For<ICache>();
            _commandBuilder = Substitute.For<ICommandBuilder>();
            _queryProvider = Substitute.For<IQueryProvider>();

            DbContext.Configure(options =>
            {
                options.UseCache(_cache);
                options.UseCommandBuilder(_commandBuilder);
                options.UseQueryProvider(_queryProvider);
            });

            _dbContext = new TestContext();
        }

        [Fact]
        public void OnConstruction_DbContextWasConfigured_DbSetsAreInitialized()
        {
            _dbContext.Books.Should().NotBeNull();
            _dbContext.Products.Should().NotBeNull();
        }

        [Fact]
        public void OnConstruction_DbContextWasNotConfigured_OrmExceptionIsThrown()
        {
            DbContext.ResetConfiguration();
            Action createDbContext = () => new TestContext();
            createDbContext.Should().Throw<OrmException>();
        }

        [Fact]
        public void Save_CollectionReferenceIsNull_CollectionIsInitialised()
        {
            var entity = Builder<Author>
                .CreateNew()
                .With(x => x.Books = null)
                .Build();

            _dbContext.Save(entity);

            entity.Books.Should().NotBeNull();
            entity.Books.Should().BeEmpty();
        }

        [Fact]
        public void Save_CollectionReference_NavigatedPropertyInCollectionItemsIsSetToEntity()
        {
            var book = Builder<Book>
                .CreateNew()
                .With(x => x.Author = null)
                .Build();

            var author = Builder<Author>
                .CreateNew()
                .With(x => x.Books = new List<Book>
                {
                    book
                })
                .Build();


            _dbContext.Save(author);

            book.Author.Should().Be(author);
        }

        [Fact]
        public void Save_SingleReferenceIsNull_OrmExceptionIsThrown()
        {
            var entity = Builder<Book>
                .CreateNew()
                .With(x => x.Author = null)
                .Build();

            Action action = () => _dbContext.Save(entity);

            action.Should().Throw<OrmException>();
        }

        [Fact]
        public void Save_SingleReference_CollectionOnSingleReferenceContainsEntity()
        {
            var author = Builder<Author>
                .CreateNew()
                .Build();

            var entity = Builder<Book>
                .CreateNew()
                .With(x => x.Author = author)
                .Build();

            var saveCmd = Substitute.For<IDbCommand>();
            saveCmd.ExecuteScalar().Returns(author.PersonId);
            _commandBuilder.BuildSave(author).Returns(saveCmd);

            _dbContext.Save(entity);

            entity.Author.Books.Should().NotBeNullOrEmpty();
            entity.Author.Books.Should().Contain(entity);
        }

        [Fact]
        public void Save_CacheDetectsNoChanges_SaveIsNotPerformed()
        {
            var entity = Builder<Author>
                .CreateNew()
                .Build();

            _cache.HasChanged(entity).Returns(false);

            _dbContext.Save(entity);

            _commandBuilder.DidNotReceive().BuildSave(entity);
        }

        [Fact]
        public void Save_CacheDetectsChanges_SaveIsPerformed()
        {
            var entity = Builder<Author>
                .CreateNew()
                .Build();

            _cache.HasChanged(entity).Returns(true);

            var saveCmd = Substitute.For<IDbCommand>();
            saveCmd.ExecuteScalar().Returns(entity.PersonId);
            _commandBuilder.BuildSave(entity).Returns(saveCmd);

            _dbContext.Save(entity);

            _commandBuilder.Received().BuildSave(entity);
        }

        [Fact]
        public void Save_ManyToManyCollectionReferenceIsNull_ReferenceIsInitialized()
        {
            var entity = Builder<Product>
                .CreateNew()
                .With(x => x.Sellers = null)
                .Build();

            _cache.HasChanged(entity).Returns(true);

            var saveCmd = Substitute.For<IDbCommand>();
            saveCmd.ExecuteScalar().Returns(entity.ProductId);
            _commandBuilder.BuildSave(entity).Returns(saveCmd);

            _dbContext.Save(entity);

            entity.Sellers.Should().NotBeNull();
            entity.Sellers.Should().BeEmpty();
        }

        [Fact]
        public void Save_EmptyManyToManyCollectionReference_ManyToManyReferencesAreDeletedButNotAdded()
        {
            var entity = Builder<Product>
                .CreateNew()
                .With(x => x.Sellers = new List<Seller>())
                .Build();

            _cache.HasChanged(entity).Returns(true);

            var saveCmd = Substitute.For<IDbCommand>();
            saveCmd.ExecuteScalar().Returns(entity.ProductId);
            _commandBuilder.BuildSave(entity).Returns(saveCmd);

            _dbContext.Save(entity);

            _commandBuilder
                .BuildRemoveManyToManyReferences(entity, Arg.Any<Type>())
                .ReceivedCalls();

            _commandBuilder
                .DidNotReceive()
                .BuildSaveManyToManyReferences(entity, Arg.Any<Type>(), Arg.Any<List<object>>());
        }

        [Fact]
        public void Save_ManyToManyCollectionReference_ManyToManyReferencesAreDeletedAndAdded()
        {
            var seller = Builder<Seller>
                .CreateNew()
                .Build();

            var entity = Builder<Product>
                .CreateNew()
                .With(x => x.Sellers = new List<Seller> { seller })
                .Build();

            _cache.HasChanged(entity).Returns(true);

            var saveEntityCmd = Substitute.For<IDbCommand>();
            var saveReferenceCmd = Substitute.For<IDbCommand>();

            saveEntityCmd.ExecuteScalar().Returns(entity.ProductId);
            saveReferenceCmd.ExecuteScalar().Returns(seller.Id);

            _commandBuilder.BuildSave(entity).Returns(saveEntityCmd);
            _commandBuilder.BuildSave(seller).Returns(saveReferenceCmd);

            _dbContext.Save(entity);

            _commandBuilder
                .BuildRemoveManyToManyReferences(entity, Arg.Any<Type>())
                .ReceivedCalls();

            _commandBuilder
                .BuildSaveManyToManyReferences(entity, Arg.Any<Type>(), Arg.Any<List<object>>())
                .ReceivedCalls();
        }

        [Fact]
        public void Save_EntityIsNull_OrmExceptionIsThrown()
        {
            Author entity = null;

            Action save = () => _dbContext.Save(entity);

            save.Should().Throw<OrmException>();
        }

        [Fact]
        public void EnsureCreated_InAssembly_TablesAreFound()
        {
            _commandBuilder
                .BuildEnsureCreated(Arg.Any<List<Table>>())
                .Returns(call =>
                {
                    var tables = call.Arg<List<Table>>();
                    tables.Any(x => x.Name == nameof(Author)).Should().BeTrue();
                    tables.Any(x => x.Name == nameof(Book)).Should().BeTrue();
                    tables.Any(x => x.Name == nameof(Product)).Should().BeTrue();
                    tables.Any(x => x.Name == nameof(Seller)).Should().BeTrue();
                    return Substitute.For<IDbCommand>();
                });
            
            _dbContext.EnsureCreated();
        }

        [Fact]
        public void GetAll_ForEntityType_CommandBuilderIsCalledWithSameType()
        {
            _dbContext.GetAll<Author>();
            _commandBuilder.BuildGetAll<Author>().ReceivedCalls();
        }
        
        [Fact]
        public void GetAll_ForEntityType_CommandBuilderIsNotCalledWithDifferentType()
        {
            _dbContext.GetAll<Author>();
            _commandBuilder.DidNotReceive().BuildGetAll<Seller>();
        }
        
        [Fact]
        public void GetById_ForEntityType_CommandBuilderIsCalledWithSameType()
        {
            _dbContext.GetById<Author>(1);
            _commandBuilder.BuildGetById<Author>(1).ReceivedCalls();
        }
        
        [Fact]
        public void GetById_ForEntityType_CommandBuilderIsNotCalledWithDifferentType()
        {
            _dbContext.GetById<Author>(1);
            _commandBuilder.DidNotReceive().BuildGetById<Seller>(1);
        }
        
        [Fact]
        public void DeleteById_ForEntityType_CommandBuilderIsCalledWithSameType()
        {
            _dbContext.DeleteById<Author>(1);
            _commandBuilder.BuildDeleteById<Author>(1).ReceivedCalls();
        }
        
        [Fact]
        public void DeleteById_ForEntityType_CommandBuilderIsNotCalledWithDifferentType()
        {
            _dbContext.DeleteById<Author>(1);
            _commandBuilder.DidNotReceive().BuildDeleteById<Seller>(1);
        }
    }
}