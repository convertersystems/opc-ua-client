// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using static Workstation.Security.Cryptography.X509Certificates.X509Native;

namespace Workstation.Security.Cryptography.X509Certificates
{
    internal class SafeCryptKeyHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeCryptKeyHandle()
            : base(true)
        {
        }

        private SafeCryptKeyHandle(IntPtr handle)
            : base(true)
        {
            this.SetHandle(handle);
        }

        internal static SafeCryptKeyHandle InvalidHandle
        {
            get
            {
                return new SafeCryptKeyHandle(IntPtr.Zero);
            }
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CryptDestroyKey(this.handle);
        }
    }
}
