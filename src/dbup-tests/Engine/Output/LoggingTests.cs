﻿using System;
using System.Collections.Generic;
using DbUp.Builder;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Helpers;
using NSubstitute;
using Serilog.Core;
using Serilog.Events;
using Shouldly;
using Xunit;

namespace DbUp.Tests.Engine.Output
{
    public class LoggingTests
    {
        [Fact]
        public void WhenNoLoggerIsSpecified_LoggingShouldGoToAutodiscoveredLogger()
        {
            var scriptExecutor = Substitute.For<IScriptExecutor>();
            
            var defaultLogger = Serilog.Log.Logger;
            try
            {
                var capturedLogs = new InMemorySink();
                Serilog.Log.Logger = new Serilog.LoggerConfiguration()
                    .WriteTo.Sink(capturedLogs)
                    .CreateLogger();

                var engine = DeployChanges.To
                    .SQLiteDatabase("Data Source=:memory:")
                    .WithScript(new SqlScript("1234", "SELECT 1"))
                    .JournalTo(new NullJournal())
                    .Build();

                var result = engine.PerformUpgrade();
                result.Successful.ShouldBe(true);
                capturedLogs.Events.ShouldContain(e => e.MessageTemplate.Text == "Executing Database Server script '{0}'");
            }
            finally
            {
                Serilog.Log.Logger = defaultLogger;
            }
        }



        class InMemorySink : ILogEventSink
        {
            public List<LogEvent> Events { get; } = new List<LogEvent>();
            public void Emit(LogEvent logEvent) => Events.Add(logEvent);
        }
    }
}