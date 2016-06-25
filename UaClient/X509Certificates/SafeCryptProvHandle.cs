// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using static Workstation.Security.Cryptography.X509Certificates.X509Native;

namespace Workstation.Security.Cryptography.X509Certificates
{
    internal class SafeCryptProvHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeCryptProvHandle()
            : base(true)
        {
        }

        private SafeCryptProvHandle(IntPtr handle)
            : base(true)
        {
            this.SetHandle(handle);
        }

        internal static SafeCryptProvHandle InvalidHandle
        {
            get
            {
                return new SafeCryptProvHandle(IntPtr.Zero);
            }
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CryptReleaseContext(this.handle, 0u);
        }
    }
}
