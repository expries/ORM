using FizzWare.NBuilder;
using FluentAssertions;
using ORM.Core.Caching;
using ORM.Core.Tests.Entities;
using Xunit;

namespace ORM.Core.Tests
{
    public class EntityCacheTests
    {
        private readonly EntityCache _cache;
        
        public EntityCacheTests()
        {
            _cache = new EntityCache();
        }
        
        [Fact]
        public void Get_ObjectIsSaved_ReturnsStoredObject()
        {
            var storedObj = Builder<Author>.CreateNew().Build();
            _cache.Save(storedObj);
            
            object? obj = _cache.Get(typeof(Author), storedObj.PersonId);
            obj.Should().Be(storedObj);
        }

        [Fact]
        public void Get_ObjectIsNotSaved_ReturnsNull()
        {
            object? obj = _cache.Get(typeof(Author), 1);
            obj.Should().BeNull();
        }
        
        [Fact]
        public void Get_PrimaryKeyIsNull_ReturnsNull()
        {
            object? obj = _cache.Get(typeof(Author), null);
            obj.Should().BeNull();
        }

        [Fact]
        public void Remove_ObjectIsSaved_Returns()
        {
            var obj = Builder<Author>.CreateNew().Build();
            _cache.Save(obj);
            _cache.Remove(obj);
        }

        [Fact]
        public void Remove_ObjectIsNotSaved_Returns()
        {
            var obj = Builder<Author>.CreateNew().Build();
            _cache.Remove(obj);
        }

        [Fact]
        public void GetAll_NoObjectOfTypeIsSaved_ReturnsEmptyList()
        {
            var list = _cache.GetAll(typeof(Author));
            list.Should().BeEmpty();
        }
        
        [Fact]
        public void GetAll_ObjectsOfTypeAreSaved_ReturnsListWithObjects()
        {
            var obj1 = Builder<Author>
                .CreateNew()
                .With(x => x.PersonId = 1)
                .Build();
            
            var obj2 = Builder<Author>
                .CreateNew()
                .With(x => x.PersonId = 2)
                .Build();
            
            _cache.Save(obj1);
            _cache.Save(obj2);
            
            var list = _cache.GetAll(typeof(Author));
            list.Count.Should().Be(2);
        }

        [Fact]
        public void HasChanged_ObjectSavedUnmodified_ReturnsTrue()
        {
            var obj = Builder<Author>.CreateNew().Build();
            _cache.Save(obj);
            bool result = _cache.HasChanged(obj);
            result.Should().BeTrue();
        }
        
        [Fact]
        public void HasChanged_ObjectSavedModified_ReturnsTrue()
        {
            var obj = Builder<Author>.CreateNew().Build();
            _cache.Save(obj);
            obj.FirstName = "new name";
            bool result = _cache.HasChanged(obj);
            result.Should().BeTrue();
        }
        
        [Fact]
        public void HasChanged_ObjectNotSaved_ReturnsTrue()
        {
            var obj = Builder<Author>.CreateNew().Build();
            bool result = _cache.HasChanged(obj);
            result.Should().BeTrue();
        }
        
        [Fact]
        public void HasChanged_ObjectIsNull_ReturnsTrue()
        {
            bool result = _cache.HasChanged(null);
            result.Should().BeTrue();
        }
    }
}