using System.Linq.Expressions;
using NSubstitute;
using ORM.Core;
using ORM.Core.Interfaces;
using ORM.Linq.Interfaces;
using ORM.Linq.Tests.DbContexts;
using ORM.Linq.Tests.Entities;
using Xunit;

namespace ORM.Linq.Tests
{
     public class QueryProviderTests
     {
         private readonly QueryProvider _queryProvider;

         private readonly TestContext _dbContext;
     
         public QueryProviderTests()
         {
             var lazyLoader = Substitute.For<ILazyLoader>();
             var linqCommandBuilder = Substitute.For<ILinqCommandBuilder>();
             _queryProvider = new QueryProvider(linqCommandBuilder, lazyLoader);
             
             var cache = Substitute.For<ICache>();
             var commandBuilder = Substitute.For<ICommandBuilder>();
     
             DbContext.Configure(options =>
             {
                 options.UseCache(cache);
                 options.UseCommandBuilder(commandBuilder);
                 options.UseQueryProvider(_queryProvider);
             });
     
             _dbContext = new TestContext();
         }
         
         [Fact]
         public void CreateQuery_ValidExpression_ReturnsIQueryable()
         {
             var expression = Expression.Constant(_dbContext.Books);
             _queryProvider.CreateQuery(expression);
         }
         
         [Fact]
         public void CreateQueryWithType_ValidExpression_ReturnsIQueryable()
         {
             var expression = Expression.Constant(_dbContext.Books);
             _queryProvider.CreateQuery<Book>(expression);
         }
     
         [Fact]
         public void ExecuteQuery_ValidExpression_ReturnsObjectReader()
         {
             var expression = Expression.Constant(_dbContext.Books);
             _queryProvider.Execute(expression);
         }
         
         [Fact]
         public void ExecuteQueryWithType_ValidExpression_ReturnsObjectReader()
         {
             var expression = Expression.Constant(_dbContext.Books);
             _queryProvider.Execute<Book>(expression);
         }
     }   
}