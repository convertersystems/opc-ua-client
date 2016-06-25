// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using static Workstation.Security.Cryptography.X509Certificates.X509Native;

namespace Workstation.Security.Cryptography.X509Certificates
{
    internal class SafeX509Extension : IDisposable
    {
        private readonly CERT_EXTENSION value;
        private readonly IntPtr blobPtr;
        private bool isDisposed = false;

        public SafeX509Extension(X509Extension extension)
        {
            this.blobPtr = Marshal.AllocHGlobal(extension.RawData.Length);
            Marshal.Copy(extension.RawData, 0, this.blobPtr, extension.RawData.Length);
            var blob = new CRYPTOAPI_BLOB
            {
                cbData = (uint)extension.RawData.Length,
                pbData = this.blobPtr
            };
            var nativeExtension = new CERT_EXTENSION
            {
                fCritical = extension.Critical,
                pszObjId = extension.Oid.Value,
                Value = blob
            };
            this.value = nativeExtension;
        }

        ~SafeX509Extension()
        {
            this.Dispose();
        }

        public CERT_EXTENSION Value
        {
            get
            {
                if (this.isDisposed)
                {
                    throw new ObjectDisposedException("blob");
                }

                return this.value;
            }
        }

        public void Dispose()
        {
            this.isDisposed = true;
            Marshal.FreeHGlobal(this.blobPtr);
            GC.SuppressFinalize(this);
        }
    }
}
