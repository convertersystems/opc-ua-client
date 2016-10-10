// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#define DEBUG
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Writes events from System.Diagnostics.Tracing.EventSource to the debugger.
    /// </summary>
    public class DebugEventListener : EventListener
    {
        private readonly Func<EventWrittenEventArgs, string> messageFormatter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugEventListener"/> class.
        /// </summary>
        /// <param name="messageFormatter">A function to format the message.</param>
        public DebugEventListener(Func<EventWrittenEventArgs, string> messageFormatter = null)
        {
            this.messageFormatter = messageFormatter ?? (x => $"[{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")}][{Environment.CurrentManagedThreadId.ToString("00")}][{x.Level}]{string.Format(x.Message ?? string.Empty, x.Payload.ToArray())}");
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            Debug.WriteLine(this.messageFormatter(eventData));
        }
    }

}
