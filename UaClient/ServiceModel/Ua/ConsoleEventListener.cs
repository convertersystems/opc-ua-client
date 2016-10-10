// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using System.Linq;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Writes events from System.Diagnostics.Tracing.EventSource to the standard output stream.
    /// </summary>
    public class ConsoleEventListener : EventListener
    {
        private readonly Func<EventWrittenEventArgs, string> messageFormatter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleEventListener"/> class.
        /// </summary>
        /// <param name="messageFormatter">A function to format the message.</param>
        public ConsoleEventListener(Func<EventWrittenEventArgs, string> messageFormatter = null)
        {
            this.messageFormatter = messageFormatter ?? (x => $"[{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")}][{Environment.CurrentManagedThreadId.ToString("00")}][{x.Level}]{string.Format(x.Message ?? string.Empty, x.Payload.ToArray())}");
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            Console.WriteLine(this.messageFormatter(eventData));
        }
    }

}
