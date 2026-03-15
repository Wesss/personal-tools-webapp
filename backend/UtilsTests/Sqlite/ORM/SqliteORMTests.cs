using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Shouldly;
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
        private readonly SqliteORM<TestRow> ORM;

        public SqliteORMTests()
        {
            _logger = new MSTestLogger<SqliteORM<TestRow>>(msg => TestContext?.WriteLine(msg));
            _dbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.sqlite");
            ORM = new SqliteORM<TestRow>(_dbPath, _logger);
        }

        private static TestRow CreateRow(int seed = 1) => new TestRow
        {
            TestString = $"String_{seed}",
            TestInt = 1000 + seed,
            TestUniqueString = $"Unique_{seed}_{Guid.NewGuid()}"
        };

        private static List<TestRow> CreateRows(int count) =>
            Enumerable.Range(1, count).Select(CreateRow).ToList();

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
            var row = CreateRow();
            ORM.Upsert(row);

            var result = ORM.Get("TestString = 'NonExistentValue'");

            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [TestMethod]
        public void Upsert_ShouldInsertNewRow_WhenTableIsEmpty()
        {
            var row = CreateRow();

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
            var originalRow = new TestRow
            {
                TestUniqueString = uniqueKey,
                TestInt = 100,
                TestString = "Original String"
            };

            var updatedRow = new TestRow
            {
                TestUniqueString = uniqueKey, // Same key to trigger conflict
                TestInt = 999, // Changed value
                TestString = "Updated String"
            };

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
            var rows = CreateRows(5);

            ORM.Upsert(rows);

            var result = ORM.Get("1=1").ToList();
            result.Count.ShouldBe(5);
        }

        [TestMethod]
        public void Get_ReturnsAllRecords_OnTrueFilter()
        {
            var rows = CreateRows(3);
            ORM.Upsert(rows);

            var result = ORM.Get("1=1");

            result.Count().ShouldBe(3);
            result.ShouldContain(x => x.TestUniqueString == rows[0].TestUniqueString);
        }

        [TestMethod]
        public void Get_FiltersCorrectly_WithSimpleClause()
        {
            var target = CreateRow(99);
            var others = CreateRows(2);
            ORM.Upsert(new[] { target }.Concat(others));

            var result = ORM.Get("TestString = @val", new { val = target.TestString });

            result.ShouldHaveSingleItem();
            result.First().TestUniqueString.ShouldBe(target.TestUniqueString);
        }

        [TestMethod]
        public void Get_HandlesSqlParameters_WithSpecialCharacters()
        {
            var row = CreateRow();
            row.TestString = "Data'With;Quotes";

            ORM.Upsert(row);

            var result = ORM.Get("TestString = @val", new { val = row.TestString });

            result.ShouldHaveSingleItem();
            result.First().TestString.ShouldBe("Data'With;Quotes");
        }

        [TestMethod]
        public void Get_ThrowsSqliteException_OnInvalidSyntax()
        {
            Should.Throw<SqliteException>(() => ORM.Get("NON_EXISTENT_COLUMN = 1"));
        }

        [TestMethod]
        public void Delete_ByValues_ShouldRemoveSpecifiedRow()
        {
            var rows = CreateRows(3);
            ORM.Upsert(rows);
            var targetRow = rows[0];

            ORM.Delete(targetRow);

            var result = ORM.Get("1=1").ToList();
            result.Count.ShouldBe(2);
            result.ShouldNotContain(x => x.TestUniqueString == targetRow.TestUniqueString);
        }

        [TestMethod]
        public void Delete_ByValues_ShouldRemoveMultipleRows()
        {
            var rows = CreateRows(5);
            ORM.Upsert(rows);
            var targetsToRemove = rows.Take(3).ToList();

            ORM.Delete(targetsToRemove);

            var result = ORM.Get("1=1").ToList();
            result.Count.ShouldBe(2);
            foreach (var target in targetsToRemove)
            {
                result.ShouldNotContain(x => x.TestUniqueString == target.TestUniqueString);
            }
        }

        [TestMethod]
        public void Delete_ByValues_DoesNothing_WhenListIsEmpty()
        {
            var rows = CreateRows(2);
            ORM.Upsert(rows);

            ORM.Delete(Enumerable.Empty<TestRow>());

            var result = ORM.Get("1=1").ToList();
            result.Count.ShouldBe(2);
        }

        [TestMethod]
        public void Delete_ByValues_DoesNothing_WhenKeysDoNotExist()
        {
            var existingRows = CreateRows(2);
            ORM.Upsert(existingRows);

            // Create a completely new row that was never inserted
            var nonExistentRow = CreateRow(99);

            ORM.Delete(nonExistentRow);

            var result = ORM.Get("1=1").ToList();
            result.Count.ShouldBe(2);
        }

        [TestMethod]
        public void Delete_ByFilter_ShouldRemoveMatchingRow()
        {
            var rows = CreateRows(3);
            ORM.Upsert(rows);
            var targetRow = rows.First();

            ORM.Delete("TestUniqueString = @val", new { val = targetRow.TestUniqueString });

            var result = ORM.Get("1=1").ToList();
            result.Count.ShouldBe(2);
            result.ShouldNotContain(x => x.TestUniqueString == targetRow.TestUniqueString);
        }

        [TestMethod]
        public void Delete_ByFilter_ShouldRemoveMultipleMatchingRows()
        {
            var rows = CreateRows(5);
            var targetInt = 9999;
            rows[0].TestInt = targetInt;
            rows[1].TestInt = targetInt;
            ORM.Upsert(rows);

            ORM.Delete("TestInt = @val", new { val = targetInt });

            var result = ORM.Get("1=1").ToList();
            result.Count.ShouldBe(3);
            result.ShouldNotContain(x => x.TestInt == targetInt);
        }

        [TestMethod]
        public void Delete_ByFilter_ThrowsArgumentException_OnEmptyFilter()
        {
            Should.Throw<ArgumentException>(() => ORM.Delete(""));
            Should.Throw<ArgumentException>(() => ORM.Delete("   "));
        }

        [TestMethod]
        public void Delete_ByFilter_ThrowsSqliteException_OnInvalidSyntax()
        {
            Should.Throw<SqliteException>(() => ORM.Delete("NON_EXISTENT_COLUMN = 1"));
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