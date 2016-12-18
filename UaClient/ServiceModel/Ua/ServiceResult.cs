// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A class that combines the status code and diagnostic info structures.
    /// </summary>
    public class ServiceResult
    {
        public static readonly ServiceResult Good = new ServiceResult(StatusCodes.Good);

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceResult"/> class.
        /// </summary>
        /// <param name="code">A code.</param>
        /// <param name="symbolicId">A symbolicId.</param>
        /// <param name="namespaceUri">A namespaceUri.</param>
        /// <param name="localizedText">A localizedText.</param>
        /// <param name="additionalInfo">AdditionalInfo.</param>
        /// <param name="innerResult">An innerResult</param>
        public ServiceResult(StatusCode code, string symbolicId = null, string namespaceUri = null, LocalizedText localizedText = null, string additionalInfo = null, ServiceResult innerResult = null)
        {
            StatusCode = code;
            SymbolicId = symbolicId;
            NamespaceUri = namespaceUri;
            LocalizedText = localizedText;
            AdditionalInfo = additionalInfo;
            InnerResult = innerResult;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceResult"/> class.
        /// </summary>
        /// <param name="code">A code.</param>
        /// <param name="diagnosticInfo">A diagnostic info.</param>
        /// <param name="stringTable">A string table.</param>
        public ServiceResult(StatusCode code, DiagnosticInfo diagnosticInfo, IList<string> stringTable)
        {
            StatusCode = code;

            if (diagnosticInfo != null)
            {
                SymbolicId = LookupString(stringTable, diagnosticInfo.SymbolicId);
                NamespaceUri = LookupString(stringTable, diagnosticInfo.NamespaceUri);

                string locale = LookupString(stringTable, diagnosticInfo.Locale);
                string localizedText = LookupString(stringTable, diagnosticInfo.LocalizedText);
                LocalizedText = new LocalizedText(locale, localizedText);

                AdditionalInfo = diagnosticInfo.AdditionalInfo;

                if (!StatusCode.IsGood(diagnosticInfo.InnerStatusCode))
                {
                    InnerResult = new ServiceResult(diagnosticInfo.InnerStatusCode, diagnosticInfo.InnerDiagnosticInfo, stringTable);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceResult"/> class.
        /// </summary>
        /// <param name="code">A code.</param>
        /// <param name="index">An index.</param>
        /// <param name="diagnosticInfos">A diagnostic info array.</param>
        /// <param name="stringTable">A string table.</param>
        public ServiceResult(StatusCode code, int index, DiagnosticInfo[] diagnosticInfos, IList<string> stringTable)
        {
            StatusCode = code;

            if (index >= 0 && diagnosticInfos != null && index < diagnosticInfos.Length)
            {
                DiagnosticInfo diagnosticInfo = diagnosticInfos[index];

                if (diagnosticInfo != null)
                {
                    SymbolicId = LookupString(stringTable, diagnosticInfo.SymbolicId);
                    NamespaceUri = LookupString(stringTable, diagnosticInfo.NamespaceUri);

                    string locale = LookupString(stringTable, diagnosticInfo.Locale);
                    string localizedText = LookupString(stringTable, diagnosticInfo.LocalizedText);
                    LocalizedText = new LocalizedText(locale, localizedText);

                    AdditionalInfo = diagnosticInfo.AdditionalInfo;

                    if (!StatusCode.IsGood(diagnosticInfo.InnerStatusCode))
                    {
                        InnerResult = new ServiceResult(diagnosticInfo.InnerStatusCode, diagnosticInfo.InnerDiagnosticInfo, stringTable);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the status code associated with the result.
        /// </summary>
        public StatusCode StatusCode { get; }

        /// <summary>
        /// Gets the namespace that qualifies symbolic identifier.
        /// </summary>
        public string NamespaceUri { get; }

        /// <summary>
        /// Gets the qualified name of the symbolic identifier associated with the status code.
        /// </summary>
        public string SymbolicId { get; }

        /// <summary>
        /// Gets the localized description for the status code.
        /// </summary>
        public LocalizedText LocalizedText { get; }

        /// <summary>
        /// Gets additional diagnostic/debugging information associated with the operation.
        /// </summary>
        public string AdditionalInfo { get; }

        /// <summary>
        /// Gets nested error information.
        /// </summary>
        public ServiceResult InnerResult { get; }

        /// <summary>
        /// Converts a 32-bit code a ServiceResult object.
        /// </summary>
        /// <returns>A ServiceResult.</returns>
        public static implicit operator ServiceResult(uint code)
        {
            return new ServiceResult(code);
        }

        /// <summary>
        /// Converts a StatusCode a ServiceResult object.
        /// </summary>
        /// <returns>A ServiceResult.</returns>
        public static implicit operator ServiceResult(StatusCode code)
        {
            return new ServiceResult(code);
        }

        /// <summary>
        /// Returns true if the status code is good.
        /// </summary>
        /// <returns>A bool.</returns>
        public static bool IsGood(ServiceResult status)
        {
            if (status != null)
            {
                return StatusCode.IsGood(status.StatusCode);
            }

            return true;
        }

        /// <summary>
        /// Returns true if the status code is uncertain.
        /// </summary>
        /// <returns>A bool.</returns>
        public static bool IsUncertain(ServiceResult status)
        {
            if (status != null)
            {
                return StatusCode.IsUncertain(status.StatusCode);
            }

            return false;
        }

        /// <summary>
        /// Returns true if the status code is bad.
        /// </summary>
        /// <returns>A bool.</returns>
        public static bool IsBad(ServiceResult status)
        {
            if (status != null)
            {
                return StatusCode.IsBad(status.StatusCode);
            }

            return false;
        }

        /// <summary>
        /// Converts the value to a human readable string.
        /// </summary>
        /// <returns>A string.</returns>
        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            buffer.Append(StatusCodes.GetDefaultMessage(StatusCode));

            if (!string.IsNullOrEmpty(SymbolicId))
            {
                if (!string.IsNullOrEmpty(NamespaceUri))
                {
                    buffer.AppendFormat(" ({0}:{1})", NamespaceUri, SymbolicId);
                }
                else if (SymbolicId != buffer.ToString())
                {
                    buffer.AppendFormat(" ({0})", SymbolicId);
                }
            }

            if (!string.IsNullOrEmpty(LocalizedText))
            {
                buffer.AppendFormat(" '{0}'", LocalizedText);
            }

            if ((0x0000FFFF & StatusCode) != 0)
            {
                buffer.AppendFormat(" [{0:X4}]", 0x0000FFFF & StatusCode);
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Looks up a string in a string table.
        /// </summary>
        /// <returns>A string.</returns>
        private static string LookupString(IList<string> stringTable, int index)
        {
            if (stringTable == null || index < 0 || index >= stringTable.Count)
            {
                return null;
            }

            return stringTable[index];
        }
    }
}