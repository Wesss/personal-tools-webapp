using AutoFixture;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Utils.Sqlite.ORM;

namespace UtilsTests.Sqlite.ORM
{
    [SqliteTable("TestTable")]
    public class TestRow : SqliteRow
    {
        public long TestTableId { get; set; }

        [SqliteColumn(SqliteColumnType.Text, SqliteNull.NotNull)]
        public string TestString { get; set; } = "";

        [SqliteColumn(SqliteColumnType.Integer, SqliteNull.NotNull)]
        public int TestInt { get; set; }

        [SqliteColumn(SqliteColumnType.Text, SqliteNull.NotNull, SqliteUniqueKey.UniqueKey)]
        public string TestUniqueString { get; set; } = "";
    }

    [TestClass]
    public class SqliteORMTests
    {
        // MSTest automatically injects this property
        public TestContext TestContext { get; set; }

        private readonly ILogger<SqliteORM<TestRow>> _logger;
        private readonly string _dbPath;
        private readonly Fixture _fixture;
        private const int ConstantSeed = 42;
        private readonly SqliteORM<TestRow> ORM;

        public SqliteORMTests()
        {
            _logger = new MSTestLogger<SqliteORM<TestRow>>(msg => TestContext?.WriteLine(msg));
            _fixture = new Fixture();
            SeedFixture(_fixture, ConstantSeed);
            _dbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.sqlite");
            ORM = new SqliteORM<TestRow>(_dbPath, _logger);
        }
        private static void SeedFixture(IFixture fixture, int seed)
        {
            var random = new Random(seed);
            fixture.Customize<int>(c => c.FromFactory(() => random.Next(1, 10000)));
            fixture.Customize<string>(c => c.FromFactory(() => $"String_{random.Next(1000, 9999)}"));
        }

        [TestMethod]
        public void Get_InitializesDatabaseAndTable()
        {
            ORM.Get("1=1");
            File.Exists(_dbPath).ShouldBeTrue();
        }

        [TestMethod]
        public void Get_ReturnsEmpty_WhenNoDataExists()
        {
            var result = ORM.Get("1=1");

            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [TestMethod]
        public void Get_ShouldReturnEmpty_WhenNoMatchesFound()
        {
            var row = _fixture.Create<TestRow>();
            ORM.Upsert(row);

            var result = ORM.Get("TestString = 'NonExistentValue'");

            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [TestMethod]
        public void Upsert_ShouldInsertNewRow_WhenTableIsEmpty()
        {
            var row = _fixture.Build<TestRow>()
                              .Without(x => x.TestTableId)
                              .Create();

            ORM.Upsert(row);

            var result = ORM.Get("1=1").ToList();
            result.ShouldNotBeEmpty();
            result.Count.ShouldBe(1);
            var storedRow = result.First();
            storedRow.TestString.ShouldBe(row.TestString);
            storedRow.TestInt.ShouldBe(row.TestInt);
            storedRow.TestUniqueString.ShouldBe(row.TestUniqueString);
            storedRow.TestTableId.ShouldBeGreaterThan(0);
        }

        [TestMethod]
        public void Upsert_ShouldUpdateExistingRow_WhenUniqueKeyConflicts()
        {
            var uniqueKey = "common-key";
            var originalRow = _fixture.Build<TestRow>()
                .Without(x => x.TestTableId)
                .With(x => x.TestUniqueString, uniqueKey)
                .With(x => x.TestInt, 100)
                .Create();
            var updatedRow = _fixture.Build<TestRow>()
                .Without(x => x.TestTableId)
                .With(x => x.TestUniqueString, uniqueKey) // Same key to trigger conflict
                .With(x => x.TestInt, 999) // Changed value
                .Create();

            ORM.Upsert(originalRow);
            ORM.Upsert(updatedRow);

            var result = ORM.Get($"TestUniqueString = @val", new { val = uniqueKey }).ToList();
            result.Count.ShouldBe(1);
            result.First().TestInt.ShouldBe(999);
            result.First().TestString.ShouldBe(updatedRow.TestString);
        }

        [TestMethod]
        public void Upsert_Bulk_ShouldInsertMultipleRows()
        {
            var rows = _fixture.Build<TestRow>()
                .Without(x => x.TestTableId)
                .CreateMany(5)
                .ToList();

            ORM.Upsert(rows);

            var result = ORM.Get("1=1").ToList();
            result.Count.ShouldBe(5);
        }

        [TestMethod]
        public void Get_ReturnsAllRecords_OnTrueFilter()
        {
            var rows = _fixture.CreateMany<TestRow>(3).ToList();
            ORM.Upsert(rows);

            var result = ORM.Get("1=1");

            result.Count().ShouldBe(3);
            result.ShouldContain(x => x.TestUniqueString == rows[0].TestUniqueString);
        }

        [TestMethod]
        public void Get_FiltersCorrectly_WithSimpleClause()
        {
            var target = _fixture.Create<TestRow>();
            var others = _fixture.CreateMany<TestRow>(2).ToList();
            ORM.Upsert(new[] { target }.Concat(others));

            var result = ORM.Get("TestString = @val", new { val = target.TestString });

            result.ShouldHaveSingleItem();
            result.First().TestUniqueString.ShouldBe(target.TestUniqueString);
        }

        [TestMethod]
        public void Get_HandlesSqlParameters_WithSpecialCharacters()
        {
            var row = _fixture.Build<TestRow>()
                .With(x => x.TestString, "Data'With;Quotes")
                .Create();
            ORM.Upsert(new[] { row });

            var result = ORM.Get("TestString = @val", new { val = row.TestString });

            result.ShouldHaveSingleItem();
            result.First().TestString.ShouldBe("Data'With;Quotes");
        }

        [TestMethod]
        public void Get_ThrowsSqliteException_OnInvalidSyntax()
        {
            Should.Throw<SqliteException>(() => ORM.Get("NON_EXISTENT_COLUMN = 1"));
        }

        [TestCleanup]
        public void Cleanup()
        {
            ORM?.Dispose();
            if (File.Exists(_dbPath))
            {
                try { File.Delete(_dbPath); } catch { }
            }
        }
    }
}