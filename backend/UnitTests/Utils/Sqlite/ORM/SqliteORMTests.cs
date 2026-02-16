using AutoFixture;
using Meziantou.Extensions.Logging.Xunit.v3;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Shouldly;
using Utils.Sqlite.ORM;

namespace UnitTests.Utils.Sqlite.ORM
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

    public class SqliteORMTests : IDisposable
    {
        private readonly ILogger<SqliteORM<TestRow>> _logger;
        private readonly string _dbPath;
        private readonly Fixture _fixture;
        private const int ConstantSeed = 42;
        private readonly SqliteORM<TestRow> ORM;

        public SqliteORMTests(ITestOutputHelper testOutputHelper)
        {
            _logger = XUnitLogger.CreateLogger<SqliteORM<TestRow>>(testOutputHelper);

            _dbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.sqlite");
            ORM = new SqliteORM<TestRow>(_dbPath, _logger);

            _fixture = new Fixture();
            SeedFixture(_fixture, ConstantSeed);
        }

        private static void SeedFixture(IFixture fixture, int seed)
        {
            var random = new Random(seed);

            fixture.Customize<int>(
                composer => composer.FromFactory(() => random.Next(1, 10000))
            );
            fixture.Customize<string>(composer =>
                composer.FromFactory(() => $"String_{random.Next(1000, 9999)}")
            );
        }

        [Fact]
        public void Get_InitializesDatabaseAndTable()
        {
            ORM.Get("1=1");
            File.Exists(_dbPath).ShouldBeTrue();
        }

        [Fact]
        public void Get_ReturnsEmpty_WhenNoDataExists()
        {
            var result = ORM.Get("1=1");

            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Fact]
        public void Get_ReturnsAllRecords_OnTrueFilter()
        {
            var rows = _fixture.CreateMany<TestRow>(3).ToList();
            ORM.Upsert(rows);

            var result = ORM.Get("1=1");

            result.Count().ShouldBe(3);
            result.ShouldContain(x => x.TestUniqueString == rows[0].TestUniqueString);
        }

        [Fact]
        public void Get_FiltersCorrectly_WithSimpleClause()
        {
            var target = _fixture.Create<TestRow>();
            var others = _fixture.CreateMany<TestRow>(2).ToList();
            ORM.Upsert(new[] { target }.Concat(others));

            var result = ORM.Get("TestString = @val", new { val = target.TestString });

            result.ShouldHaveSingleItem();
            result.First().TestUniqueString.ShouldBe(target.TestUniqueString);
        }

        [Fact]
        public void Get_HandlesSqlParameters_WithSpecialCharacters()
        {
            var row = _fixture.Build<TestRow>()
                .With(x => x.TestString, "Data'With;Quotes")
                .Create();
            ORM.Upsert([row]);

            var result = ORM.Get("TestString = @val", new { val = row.TestString });

            result.ShouldHaveSingleItem();
            result.First().TestString.ShouldBe("Data'With;Quotes");
        }

        [Fact]
        public void Get_ThrowsSqliteException_OnInvalidSyntax()
        {
            Should.Throw<SqliteException>(() => ORM.Get("NON_EXISTENT_COLUMN = 1"));
        }

        public void Dispose()
        {
            ORM.Dispose();
            if (File.Exists(_dbPath))
            {
                try { File.Delete(_dbPath); } catch { }
            }
        }
    }
}