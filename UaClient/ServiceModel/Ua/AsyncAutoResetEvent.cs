// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    public class AsyncAutoResetEvent
    {
        private readonly Queue<TaskCompletionSource<bool>> waits = new Queue<TaskCompletionSource<bool>>();
        private static readonly Task completedTask = Task.FromResult(true);
        private bool signaled;

        public Task WaitAsync()
        {
            lock (this.waits)
            {
                if (this.signaled)
                {
                    this.signaled = false;
                    return completedTask;
                }
                else
                {
                    var tcs = new TaskCompletionSource<bool>();
                    this.waits.Enqueue(tcs);
                    return tcs.Task;
                }
            }
        }

        public void Set()
        {
            TaskCompletionSource<bool> toRelease = null;
            lock (this.waits)
            {
                if (this.waits.Count > 0)
                {
                    toRelease = this.waits.Dequeue();
                }
                else if (!this.signaled)
                {
                    this.signaled = true;
                }
            }

            if (toRelease != null)
            {
                toRelease.SetResult(true);
            }
        }
    }
}
