// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using static Workstation.Security.Cryptography.X509Certificates.X509Native;

namespace Workstation.Security.Cryptography.X509Certificates
{
    internal class SafeCertStoreHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeCertStoreHandle()
            : base(true)
        {
        }

        private SafeCertStoreHandle(IntPtr handle)
            : base(true)
        {
            this.SetHandle(handle);
        }

        internal static SafeCertStoreHandle InvalidHandle
        {
            get
            {
                return new SafeCertStoreHandle(IntPtr.Zero);
            }
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CertCloseStore(this.handle, 0u);
        }
    }
}
