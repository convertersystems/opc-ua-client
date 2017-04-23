// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Use to build an <see cref="UaApplication"/>.
    /// </summary>
    public class UaApplicationBuilder
    {
        private ApplicationDescription localDescription;
        private ICertificateStore certificateStore;
        private Func<EndpointDescription, Task<IUserIdentity>> identityProvider;
        private ILoggerFactory loggerFactory;
        private UaApplicationOptions options;
        private Dictionary<string, EndpointDescription> mappedEndpoints = new Dictionary<string, EndpointDescription>();

        /// <summary>
        /// Specify the ApplicationUri.
        /// </summary>
        /// <param name="uri">A uri in the form of 'http://{hostname}/{appname}' -or- 'urn:{hostname}:{appname}'.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder UseApplicationUri(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (this.localDescription != null)
            {
                throw new InvalidOperationException("The localDescription has already been specified.");
            }

            string appName = null;

            UriBuilder appUri = new UriBuilder(uri);
            if (appUri.Scheme == "http" && !string.IsNullOrEmpty(appUri.Host))
            {
                var path = appUri.Path.Trim('/');
                if (!string.IsNullOrEmpty(path))
                {
                    appName = path;
                }
            }

            if (appUri.Scheme == "urn")
            {
                var parts = appUri.Path.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    appName = parts[1];
                }
            }

            if (appName == null)
            {
                throw new ArgumentOutOfRangeException(nameof(uri), "Expecting ApplicationUri in the form of 'http://{hostname}/{appname}' -or- 'urn:{hostname}:{appname}'.");
            }

            this.localDescription = new ApplicationDescription
            {
                ApplicationUri = appUri.ToString(),
                ApplicationName = appName,
                ApplicationType = ApplicationType.Client
            };

            return this;
        }

        /// <summary>
        /// Specify the path to the directory-based Certificate store. Directory will be created if it does not exist.
        /// </summary>
        /// <param name="path">The path to the local pki directory.</param>
        /// <param name="acceptAllRemoteCertificates">Set true to accept all remote certificates.</param>
        /// <param name="createLocalCertificateIfNotExist">Set true to create a local certificate and private key, if the files do not exist.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder UseDirectoryStore(string path, bool acceptAllRemoteCertificates = true, bool createLocalCertificateIfNotExist = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (this.certificateStore != null)
            {
                throw new InvalidOperationException("The certificateStore has already been specified.");
            }

            this.certificateStore = new DirectoryStore(path, acceptAllRemoteCertificates, createLocalCertificateIfNotExist);
            return this;
        }

        /// <summary>
        /// Use the <see cref="IUserIdentity"/>.
        /// </summary>
        /// <param name="identity">The user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder UseIdentity(IUserIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            if (this.identityProvider != null)
            {
                throw new InvalidOperationException("The identityProvider has already been specified.");
            }

            this.identityProvider = async endpoint => identity;
            return this;
        }

        /// <summary>
        /// Specify the function that provides the <see cref="IUserIdentity"/>.
        /// </summary>
        /// <param name="identityProvider">An asynchronous function that provides the user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder UseIdentity(Func<EndpointDescription, Task<IUserIdentity>> identityProvider)
        {
            if (identityProvider == null)
            {
                throw new ArgumentNullException(nameof(identityProvider));
            }

            if (this.identityProvider != null)
            {
                throw new InvalidOperationException("The IdentityProvider has already been specified.");
            }

            this.identityProvider = identityProvider;
            return this;
        }

        /// <summary>
        /// Specify the <see cref="ILoggerFactory"/>.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder UseLoggerFactory(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (this.loggerFactory != null)
            {
                throw new InvalidOperationException("The loggerFactory has already been specified.");
            }

            this.loggerFactory = loggerFactory;
            return this;
        }

        /// <summary>
        /// Specify the <see cref="UaApplicationOptions"/>.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder UseOptions(UaApplicationOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (this.options != null)
            {
                throw new InvalidOperationException("The options has already been specified.");
            }

            this.options = options;
            return this;
        }

        /// <summary>
        /// Substitute the <see cref="EndpointDescription"/> for the requested url.
        /// </summary>
        /// <param name="requestedUrl">The url requested.</param>
        /// <param name="endpoint">The endpoint decription to use.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder Map(string requestedUrl, EndpointDescription endpoint)
        {
            if (string.IsNullOrEmpty(requestedUrl))
            {
                throw new ArgumentNullException(nameof(requestedUrl));
            }

            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            this.mappedEndpoints.Add(requestedUrl, endpoint);
            return this;
        }

        /// <summary>
        /// Substitute the endpoint url for the requested url. The most secure <see cref="EndpointDescription"/> with matching SecurityPolicyUri will be selected.
        /// </summary>
        /// <param name="requestedUrl">The url requested.</param>
        /// <param name="endpointUrl">The endpoint url to use.</param>
        /// <param name="securityPolicyUri">Optionally, filter by SecurityPolicyUri.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder Map(string requestedUrl, string endpointUrl, string securityPolicyUri = null)
        {
            if (string.IsNullOrEmpty(requestedUrl))
            {
                throw new ArgumentNullException(nameof(requestedUrl));
            }

            if (string.IsNullOrEmpty(endpointUrl))
            {
                throw new ArgumentNullException(nameof(endpointUrl));
            }

            this.mappedEndpoints.Add(requestedUrl, new EndpointDescription { EndpointUrl = endpointUrl, SecurityPolicyUri = securityPolicyUri });
            return this;
        }

        /// <summary>
        /// Build the <see cref="UaApplication"/>
        /// </summary>
        /// <returns>The  <see cref="UaApplication"/></returns>
        public UaApplication Build()
        {
            if (this.localDescription == null)
            {
                throw new InvalidOperationException("An ApplicationUri or ApplicationDescription must be specified.");
            }

            return new UaApplication(
                this.localDescription,
                this.certificateStore,
                this.identityProvider,
                this.mappedEndpoints,
                this.loggerFactory,
                this.options);
        }
    }
}
