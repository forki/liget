using System;
using System.Text;
using LiGet.Cache.Catalog;
using LiGet.Cache.DBreeze;
using Moq;
using Xunit;
using static DBreeze.DBreezeConfiguration;

namespace LiGet.Tests.Cache.DBreeze
{
    public class DBreezeCacheProviderTest : IDisposable
    {
        private Mock<ICatalogScanner> catalogScanner;
        private DBreezeCacheProvider db;
        private byte[] metaContent = Encoding.UTF8.GetBytes("meta");
        private DBreezeEngine engine;

        public DBreezeCacheProviderTest() {
            Mock<IDBreezeConfig> config = new Mock<IDBreezeConfig>(MockBehavior.Loose);
            config.SetupGet(c => c.StorageBackend).Returns(eStorage.MEMORY);
            config.SetupGet(c => c.RootCacheDirectory).Returns("dummy");
            catalogScanner = new Mock<ICatalogScanner>(MockBehavior.Loose);
            db = new DBreezeCacheProvider(engine = new DBreezeEngine(config.Object), catalogScanner.Object);
        }
        public void Dispose()
        {
            if(engine != null)
                engine.Dispose();
        }

        [Fact]
        public void InsertAndGetContent() {
            db.Insert("a",DateTimeOffset.FromUnixTimeSeconds(10), metaContent);
            var result = db.TryGet("a");
            Assert.NotNull(result);
            var content = Encoding.UTF8.GetString(result);
            Assert.Equal("meta", content);
        }

        [Fact]
        public void GetContentReturnsNullWhenEmpty() {
            var result = db.TryGet("a");
            Assert.Null(result);
        }

        [Fact]
        public void InvalidateIfOlderShouldRemoveKeyWhenNewTimestampIsHigher() {
            db.Insert("a",DateTimeOffset.FromUnixTimeSeconds(10), metaContent);
            db.InvalidateIfOlder("a", DateTimeOffset.FromUnixTimeSeconds(11));
            Assert.Null(db.TryGet("a"));
        }

        [Fact]
        public void InvalidateIfOlderShouldNotRemoveKeyWhenNewTimestampIsLower() {
            db.Insert("a",DateTimeOffset.FromUnixTimeSeconds(10), metaContent);
            db.InvalidateIfOlder("a", DateTimeOffset.FromUnixTimeSeconds(9));
            Assert.NotNull(db.TryGet("a"));
        }

        [Fact]
        public void InvalidateIfOlderShouldNotRemoveKeyWhenPackageNameDoesNotMatch() {
            db.Insert("a",DateTimeOffset.FromUnixTimeSeconds(10), metaContent);
            db.InvalidateIfOlder("b", DateTimeOffset.FromUnixTimeSeconds(11));
            Assert.NotNull(db.TryGet("a"));
        }

    }
}