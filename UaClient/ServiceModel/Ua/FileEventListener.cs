// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Writes events from System.Diagnostics.Tracing.EventSource to a file.
    /// </summary>
    public class FileEventListener : EventListener
    {
        private readonly object newFileLock = new object();
        private readonly Func<DateTime, string> fileNameFormatter;
        private readonly Encoding encoding;
        private readonly BlockingCollection<string> q = new BlockingCollection<string>();
        private readonly Task processingTask;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly Func<EventWrittenEventArgs, string> messageFormatter;
        private readonly int daysToKeep;
        private readonly string searchPattern;
        private string fileName;
        private StreamWriter streamWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileEventListener"/> class.
        /// </summary>
        /// <param name="messageFormatter">A function that formats the message.</param>
        /// <param name="encoding">The encoding of the message.</param>
        /// <param name="fileNameFormatter">A function that formats the file name.</param>
        /// <param name="daysToKeep">The number of days to keep the file. If Int.MaxValue, then all files are retained.</param>
        public FileEventListener(
            Func<EventWrittenEventArgs, string> messageFormatter = null,
            Encoding encoding = null,
            Func<DateTime, string> fileNameFormatter = null,
            int daysToKeep = 7)
        {
            this.messageFormatter = messageFormatter ?? (x => $"[{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")}][{Environment.CurrentManagedThreadId.ToString("00")}][{x.Level}]{string.Format(x.Message ?? string.Empty, x.Payload.ToArray())}");
            this.fileNameFormatter = fileNameFormatter ?? (x => "UaClient-" + x.ToString("yyyy-MM-ddTHH") + ".log");
            this.encoding = encoding ?? Encoding.UTF8;
            this.daysToKeep = daysToKeep;
            this.searchPattern = "*" + Path.GetExtension(this.fileNameFormatter(DateTime.Now));
            this.processingTask = Task.Factory.StartNew(this.ConsumeQueue, TaskCreationOptions.LongRunning);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            this.q.Add(this.messageFormatter(eventData));
        }

        private void ConsumeQueue()
        {
            while (!this.cancellationTokenSource.IsCancellationRequested)
            {
                string nextString;
                try
                {
                    if (this.q.TryTake(out nextString, Timeout.Infinite, this.cancellationTokenSource.Token))
                    {
                        try
                        {
                            this.CheckFileRolling();
                            this.streamWriter.WriteLine(nextString);
                        }
                        catch (Exception ex)
                        {
                            EventSource.Log.Exception(ex.ToString(), ex.StackTrace, "FileStream Write/Flush failed");
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        protected void CheckFileRolling()
        {
            var now = DateTime.Now;
            var nextName = this.fileNameFormatter(now);

            if (this.streamWriter == null || nextName != this.fileName)
            {
                lock (this.newFileLock)
                {
                    this.streamWriter?.Dispose();

                    this.fileName = nextName;

                    var fi2 = new FileInfo(this.fileName);
                    if (!fi2.Directory.Exists)
                    {
                        fi2.Directory.Create();
                    }

                    this.streamWriter = new StreamWriter(
                        new FileStream(this.fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 4096, useAsync: false),
                        this.encoding);

                    if (this.daysToKeep > 0)
                    {
                        var dropDate = now.AddDays(-this.daysToKeep);
                        var dropFiles = fi2.Directory.EnumerateFiles(this.searchPattern, SearchOption.TopDirectoryOnly)
                            .Where(fi3 => fi3 != fi2 && fi3.CreationTime < dropDate);

                        foreach (var fi4 in dropFiles)
                        {
                            fi4.Delete();
                        }
                    }
                }
            }
        }

        public override void Dispose()
        {
            this.cancellationTokenSource.Cancel();
            this.streamWriter?.Dispose();
        }
    }
}
