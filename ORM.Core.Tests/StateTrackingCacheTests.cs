using FizzWare.NBuilder;
using FluentAssertions;
using ORM.Core.Caching;
using ORM.Core.Tests.Entities;
using Xunit;

namespace ORM.Core.Tests
{
    public class StateTrackingCacheTests
    {
        private readonly StateTrackingCache _cache;
        
        public StateTrackingCacheTests()
        {
            _cache = new StateTrackingCache();
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
        public void HasChanged_ObjectSavedUnmodified_ReturnsFalse()
        {
            var obj = Builder<Author>.CreateNew().Build();
            _cache.Save(obj);
            bool result = _cache.HasChanged(obj);
            result.Should().BeFalse();
        }
        
        [Fact]
        public void HasChanged_ObjectIsNull_ReturnsTrue()
        {
            bool result = _cache.HasChanged(null);
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
    }
}