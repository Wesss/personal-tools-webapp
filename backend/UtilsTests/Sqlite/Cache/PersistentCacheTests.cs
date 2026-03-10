using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using Dapper;
using Moq;
using Shouldly;
using Utils.Sqlite.Cache;
using Utils.Sqlite.ORM;

namespace Utils.Sqlite.Cache.Tests
{
    [TestClass]
    public class PersistentCacheTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<ISqliteORM<CacheRow>> _cacheOrmMock;
        private readonly Mock<ISqliteORM<SettingsRow>> _settingsOrmMock;
        private readonly Mock<TimeProvider> _timeProviderMock;
        private readonly DateTimeOffset _currentTime;

        public PersistentCacheTests()
        {
            // TODO WESD let's get rid of auto fixture, I don't think the complexity/unreadability is worth the convenience
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _cacheOrmMock = _fixture.Freeze<Mock<ISqliteORM<CacheRow>>>();
            _settingsOrmMock = _fixture.Freeze<Mock<ISqliteORM<SettingsRow>>>();
            _timeProviderMock = _fixture.Freeze<Mock<TimeProvider>>();

            // Freeze time for consistent testing
            _currentTime = new DateTimeOffset(2026, 3, 9, 12, 0, 0, TimeSpan.Zero);
            _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(_currentTime);
        }

        private PersistentCache CreateTestObj(TimeSpan? maxAge = null)
        {
            return new PersistentCache(
                _cacheOrmMock.Object,
                _settingsOrmMock.Object,
                _timeProviderMock.Object,
                maxAge ?? TimeSpan.FromDays(30)
            );
        }

        [TestMethod]
        public void Set_WhenValueIsNull_ThrowsArgumentNullException()
        {
            var testObj = CreateTestObj();
            var key = _fixture.Create<string>();

            Should.Throw<ArgumentNullException>(() => testObj.Set(key, null!));
        }

        [TestMethod]
        public void Set_WithValidData_UpsertsToCache()
        {
            var testObj = CreateTestObj();
            var key = _fixture.Create<string>();
            var value = _fixture.Create<string>();

            testObj.Set(key, value);

            _cacheOrmMock.Verify(
                x => x.Upsert(
                    It.Is<CacheRow>(r =>
                        r.CacheKey == key &&
                        r.CacheVal == value &&
                        r.DateSetUTC == _currentTime.DateTime
                    )
                ),
                Times.Once
            );
        }

        [TestMethod]
        public void Get_WhenKeyExists_ReturnsValue()
        {
            var testObj = CreateTestObj();
            var key = _fixture.Create<string>();
            var expectedRow = _fixture.Create<CacheRow>();

            _cacheOrmMock.Setup(x => x.Get(It.IsAny<string>(), It.IsAny<DynamicParameters>()))
                .Returns(new List<CacheRow> { expectedRow });

            var result = testObj.Get(key, null);

            result.ShouldBe(expectedRow.CacheVal);
        }

        [TestMethod]
        public void Get_WhenKeyDoesNotExist_ReturnsNull()
        {
            var testObj = CreateTestObj();
            var key = _fixture.Create<string>();

            _cacheOrmMock.Setup(x => x.Get(It.IsAny<string>(), It.IsAny<DynamicParameters>()))
                .Returns(new List<CacheRow>());

            var result = testObj.Get(key, null);

            result.ShouldBeNull();
        }

        [TestMethod]
        public void CheckRunMaintenance_WhenCalledBeforeMaxAge_DoesNotRunMaintenance()
        {
            var maxAge = TimeSpan.FromDays(30);
            var testObj = CreateTestObj(maxAge);

            // First run to set the internal 'lastMaintenance' to the current time
            testObj.CheckRunMaintenance();

            // Advance time, but keep it just under the max age limit
            _timeProviderMock.Setup(t => t.GetUtcNow())
                .Returns(_currentTime.Add(maxAge).AddSeconds(-1));

            // Reset mocks to ignore the first maintenance run
            _cacheOrmMock.Invocations.Clear();
            _settingsOrmMock.Invocations.Clear();

            testObj.CheckRunMaintenance();

            _cacheOrmMock.Verify(x => x.Delete(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
            _cacheOrmMock.Verify(x => x.Vacuum(), Times.Never);
            _settingsOrmMock.Verify(x => x.Upsert(It.IsAny<SettingsRow>()), Times.Never);
        }

        [TestMethod]
        public void CheckRunMaintenance_WhenCalledAfterMaxAge_RunsMaintenanceAndUpdatesSettings()
        {
            var maxAge = TimeSpan.FromDays(30);
            var testObj = CreateTestObj(maxAge);

            // Mock the settings ORM to return an old maintenance date
            var oldMaintenanceDate = _currentTime.DateTime.Subtract(TimeSpan.FromDays(31));
            var settingsRow = new SettingsRow { SettingKey = "LastMaintenance", SettingVal = oldMaintenanceDate.ToString("O") };

            _settingsOrmMock.Setup(x => x.Get(It.IsAny<string>(), It.IsAny<object>()))
                .Returns(new List<SettingsRow> { settingsRow });

            testObj.CheckRunMaintenance();

            _cacheOrmMock.Verify(x => x.Delete(It.Is<string>(s => s.Contains("DateSet < @cutoff")), It.IsAny<object>()), Times.Once);
            _cacheOrmMock.Verify(x => x.Vacuum(), Times.Once);

            _settingsOrmMock.Verify(x => x.Upsert(It.Is<SettingsRow>(r =>
                r.SettingKey == "LastMaintenance" &&
                r.SettingVal == _currentTime.DateTime.ToString("O"))),
            Times.Once);
        }
    }
}