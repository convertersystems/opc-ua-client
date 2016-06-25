// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using static Workstation.Security.Cryptography.X509Certificates.X509Native;

namespace Workstation.Security.Cryptography.X509Certificates
{
    internal class SafeCertContextHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeCertContextHandle()
            : base(true)
        {
        }

        private SafeCertContextHandle(IntPtr handle)
            : base(true)
        {
            this.SetHandle(handle);
        }

        internal static SafeCertContextHandle InvalidHandle
        {
            get
            {
                return new SafeCertContextHandle(IntPtr.Zero);
            }
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CertFreeCertificateContext(this.handle);
        }
    }
}
