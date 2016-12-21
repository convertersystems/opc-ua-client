// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Workstation.ServiceModel.Ua
{
    public sealed class DiagnosticInfo
    {
        public DiagnosticInfo(int namespaceUri = -1, int symbolicId = -1, int locale = -1, int localizedText = -1, string additionalInfo = null, StatusCode innerStatusCode = default(StatusCode), DiagnosticInfo innerDiagnosticInfo = null)
        {
            this.NamespaceUri = namespaceUri;
            this.SymbolicId = symbolicId;
            this.Locale = locale;
            this.LocalizedText = localizedText;
            this.AdditionalInfo = additionalInfo;
            this.InnerStatusCode = innerStatusCode;
            this.InnerDiagnosticInfo = innerDiagnosticInfo;
        }

        public int NamespaceUri { get; }

        public int SymbolicId { get; }

        public int Locale { get; }

        public int LocalizedText { get; }

        public string AdditionalInfo { get; }

        public StatusCode InnerStatusCode { get; }

        public DiagnosticInfo InnerDiagnosticInfo { get; }
    }
}