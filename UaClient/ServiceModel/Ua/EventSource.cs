// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Runtime.CompilerServices;

namespace Workstation.ServiceModel.Ua
{
    [EventSource(Name = "ConverterSystems-Workstation-UaClient")]
    public class EventSource : System.Diagnostics.Tracing.EventSource
    {
        public static readonly EventSource Log = new EventSource();

        public class Keywords
        {
            public const EventKeywords Logging = (EventKeywords)1;
        }

        [Event(1, Level = EventLevel.LogAlways, Keywords = Keywords.Logging, Message = "[{2}:{3}][{1}] {0}")]
        public void LogAlways(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            this.WriteEvent(1, message ?? string.Empty, memberName ?? string.Empty, this.FormatPath(filePath) ?? string.Empty, line);
        }

        [Event(2, Level = EventLevel.Critical, Keywords = Keywords.Logging, Message = "[{2}:{3}][{1}] {0}")]
        public void Critical(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            this.WriteEvent(2, message ?? string.Empty, memberName ?? string.Empty, this.FormatPath(filePath) ?? string.Empty, line);
        }

        [Event(3, Level = EventLevel.Error, Keywords = Keywords.Logging, Message = "[{2}:{3}][{1}] {0}")]
        public void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            this.WriteEvent(3, message ?? string.Empty, memberName ?? string.Empty, this.FormatPath(filePath) ?? string.Empty, line);
        }

        [Event(4, Level = EventLevel.Warning, Keywords = Keywords.Logging, Message = "[{2}:{3}][{1}] {0}")]
        public void Warning(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            this.WriteEvent(4, message ?? string.Empty, memberName ?? string.Empty, this.FormatPath(filePath) ?? string.Empty, line);
        }

        [Event(5, Level = EventLevel.Informational, Keywords = Keywords.Logging, Message = "[{2}:{3}][{1}] {0}")]
        public void Informational(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            this.WriteEvent(5, message ?? string.Empty, memberName ?? string.Empty, this.FormatPath(filePath) ?? string.Empty, line);
        }

        [Event(6, Level = EventLevel.Verbose, Keywords = Keywords.Logging, Message = "[{2}:{3}][{1}] {0}")]
        public void Verbose(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            this.WriteEvent(6, message ?? string.Empty, memberName ?? string.Empty, this.FormatPath(filePath) ?? string.Empty, line);
        }

        [Event(7, Level = EventLevel.Error, Keywords = Keywords.Logging, Version = 1)]
        public void Exception(string type, string stackTrace, string message)
        {
            this.WriteEvent(7, type ?? string.Empty, stackTrace ?? string.Empty, message ?? string.Empty);
        }

        [Conditional("DEBUG")]
        [Event(8, Level = EventLevel.Verbose, Keywords = Keywords.Logging, Message = "[{2}:{3}][{1}] {0}")]
        public void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            this.WriteEvent(8, message ?? string.Empty, memberName ?? string.Empty, this.FormatPath(filePath) ?? string.Empty, line);
        }

        [NonEvent]
        public IDisposable MeasureExecution(string label, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0)
        {
            return new StopwatchMonitor(this, label ?? string.Empty, memberName ?? string.Empty, this.FormatPath(filePath) ?? string.Empty, line);
        }

        [Event(9, Level = EventLevel.Verbose, Keywords = Keywords.Logging, Message = "[{2}:{3}][{1}] {0}: {4}ms")]
        private void MeasureExecution(string label, string memberName, string filePath, int line, double duration)
        {
            this.WriteEvent(9, label ?? string.Empty, memberName ?? string.Empty, this.FormatPath(filePath) ?? string.Empty, line, duration);
        }

        private string FormatPath(string filePath)
        {
            if (filePath == null)
            {
                return string.Empty;
            }
            return Path.GetFileName(filePath);
        }

        private class StopwatchMonitor : IDisposable
        {
            private readonly EventSource logger;
            private readonly string label;
            private readonly string memberName;
            private readonly string filePath;
            private readonly int line;
            private Stopwatch stopwatch;

            public StopwatchMonitor(EventSource logger, string label, string memberName, string filePath, int line)
            {
                this.logger = logger;
                this.label = label;
                this.memberName = memberName;
                this.filePath = filePath;
                this.line = line;
                this.stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                if (this.stopwatch != null)
                {
                    this.stopwatch.Stop();
                    this.logger.MeasureExecution(this.label, this.memberName, this.filePath, this.line, this.stopwatch.Elapsed.TotalMilliseconds);
                    this.stopwatch = null;
                }
            }
        }
    }
}
