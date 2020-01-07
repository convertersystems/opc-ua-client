// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Workstation.ServiceModel.Ua
{
    /// <summary>Defines the states in which an <see cref="T:ConverterSystems.ServiceModel.Ua.ICommunicationObject" /> can exist. </summary>
    public enum CommunicationState
    {
        /// <summary>Indicates that the communication object has been instantiated and is configurable, but not yet open or ready for use.</summary>
        Created,

        /// <summary>Indicates that the communication object is being transitioned to the opened state. </summary>
        Opening,

        /// <summary>Indicates that the communication object is now open and ready to be used. </summary>
        Opened,

        /// <summary>Indicates that the communication object is transitioning to the closed state. </summary>
        Closing,

        /// <summary>Indicates that the communication object has been closed and is no longer usable. </summary>
        Closed,

        /// <summary>Indicates that the communication object has encountered an error or fault from which it cannot recover and from which it is no longer usable. </summary>
        Faulted
    }
}