// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using static Workstation.Security.Cryptography.X509Certificates.X509Native;

namespace Workstation.Security.Cryptography.X509Certificates
{
    internal sealed class SafeLocalAllocHandle : SafeBuffer
    {
        private SafeLocalAllocHandle()
            : base(true)
        {
        }

        internal SafeLocalAllocHandle(IntPtr handle)
            : base(true)
        {
            this.SetHandle(handle);
        }

        internal static SafeLocalAllocHandle InvalidHandle
        {
            get
            {
                return new SafeLocalAllocHandle(IntPtr.Zero);
            }
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.LocalFree(this.handle) == IntPtr.Zero;
        }
    }
}
