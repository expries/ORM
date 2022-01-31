using System;
using FluentAssertions;
using ORM.Core.Models.Exceptions;
using ORM.Postgres.DataTypes;
using ORM.Postgres.Interfaces;
using ORM.Postgres.SqlDialect;
using Xunit;

namespace ORM.Postgres.Tests
{
    public class PostgresTypeMapperTests
    {
        private readonly IDbTypeMapper _typeMapper;
        
        public PostgresTypeMapperTests()
        {
            _typeMapper = new PostgresDataTypeMapper();
        }
        
        [Fact]
        public void Map_TypeString_ReturnsPostgresType()
        {
            var postgresType = _typeMapper.Map(typeof(string));
            postgresType.Should().NotBeNull();
        }
        
        [Fact]
        public void Map_TypeInt_ReturnsPostgresType()
        {
            var postgresType = _typeMapper.Map(typeof(int));
            postgresType.Should().NotBeNull();
        }
        
        [Fact]
        public void Map_TypeLong_ReturnsPostgresType()
        {
            var postgresType = _typeMapper.Map(typeof(long));
            postgresType.Should().NotBeNull();
        }
        
        [Fact]
        public void Map_TypeDouble_ReturnsPostgresType()
        {
            var postgresType = _typeMapper.Map(typeof(double));
            postgresType.Should().NotBeNull();
        }
        
        [Fact]
        public void Map_TypeDateTime_ReturnsPostgresType()
        {
            var postgresType = _typeMapper.Map(typeof(DateTime));
            postgresType.Should().NotBeNull();
        }
        
        [Fact]
        public void Map_TypIsNotRegistered_ThrowsUnknownTypeException()
        {
            Func<IDbType> map = () => _typeMapper.Map(typeof(PostgresDataTypeMapper));
            map.Should().Throw<UnknownTypeException>();
        }
    }
}