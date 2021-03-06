﻿#if !NETCORE
using System.Data;
using System.Data.SQLite;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;
using DbUp.Tests.TestInfrastructure;
using NSubstitute;
using DbUp.SQLite;
using Xunit;
using Shouldly;
using DbUp.Helpers;
using System.Linq;

namespace DbUp.Tests.Support.SQLite
{
    public class SQLiteTableJournalTests
    {
        [Fact]
        public void dbversion_is_zero_when_journal_table_not_exist()
        {
            // Given
            var dbConnection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            dbConnection.CreateCommand().Returns(command);
            var connectionManager = Substitute.For<IConnectionManager>();
            command.ExecuteScalar().Returns(x => { throw new SQLiteException("table not found"); });
            var consoleUpgradeLog = new ConsoleUpgradeLog();
            var hasher = new Hasher();
            var journal = new SQLiteTableJournal(() => connectionManager, () => consoleUpgradeLog, () => hasher, "SchemaVersions");

            // When
            var scripts = journal.GetExecutedScripts();

            // Expect
            command.DidNotReceive().ExecuteReader();
            Assert.Empty(scripts);
        }

        [Fact]
        public void creates_a_new_journal_table_when_not_exist()
        {
            // Given
            var dbConnection = Substitute.For<IDbConnection>();
            var connectionManager = new TestConnectionManager(dbConnection, true);
            var command = Substitute.For<IDbCommand>();
            var param1 = Substitute.For<IDbDataParameter>();
            var param2 = Substitute.For<IDbDataParameter>();
            dbConnection.CreateCommand().Returns(command);
            command.CreateParameter().Returns(param1, param2);
            command.ExecuteScalar().Returns(x => 0);
            var consoleUpgradeLog = new ConsoleUpgradeLog();
            var hasher = new Hasher();
            var journal = new SQLiteTableJournal(() => connectionManager, () => consoleUpgradeLog, () => hasher, "SchemaVersions");

            // When
            journal.StoreExecutedScript(new SqlScript("test", "select 1"), () => command);

            // Expect
            command.Received(2).CreateParameter();
            param1.ParameterName.ShouldBe("scriptName");
            param2.ParameterName.ShouldBe("applied");
            command.Received().ExecuteNonQuery();
        }
    }
}
#endif