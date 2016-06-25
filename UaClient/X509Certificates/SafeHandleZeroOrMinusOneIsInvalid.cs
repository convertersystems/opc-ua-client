// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Workstation.Security.Cryptography.X509Certificates
{
    /// <summary>Provides a base class for Win32 safe handle implementations in which the value of either 0 or -1 indicates an invalid handle.</summary>
    public abstract class SafeHandleZeroOrMinusOneIsInvalid : SafeHandle
    {
        /// <summary>Initializes a new instance of the <see cref="SafeHandleZeroOrMinusOneIsInvalid" /> class, specifying whether the handle is to be reliably released. </summary>
        /// <param name="ownsHandle">true to reliably release the handle during the finalization phase; false to prevent reliable release (not recommended).</param>
        protected SafeHandleZeroOrMinusOneIsInvalid(bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle)
        {
        }

        /// <summary>Gets a value indicating whether the handle is invalid.</summary>
        /// <returns>true if the handle is not valid; otherwise, false.</returns>
        public override bool IsInvalid
        {
            get
            {
                return this.handle == IntPtr.Zero || this.handle == new IntPtr(-1);
            }
        }
    }
}
