// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Use to build an <see cref="UaApplication"/>.
    /// </summary>
    public class UaApplicationBuilder
    {
        private readonly List<MappedEndpoint> mappedEndpoints = new List<MappedEndpoint>();
        private readonly List<Action<UaApplicationOptions>> configureOptionsActions = new List<Action<UaApplicationOptions>>();
        private readonly List<Action<ILoggerFactory>> configureLoggerFactoryActions = new List<Action<ILoggerFactory>>();
        private ApplicationDescription localDescription;
        private ICertificateStore certificateStore;
        private Func<EndpointDescription, Task<IUserIdentity>> identityProvider;
        private ILoggerFactory loggerFactory;
        private UaApplicationOptions options;

        /// <summary>
        /// Specify the ApplicationUri.
        /// </summary>
        /// <param name="uri">A uri in the form of 'http://{hostname}/{appname}' -or- 'urn:{hostname}:{appname}'.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder SetApplicationUri(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                throw new ArgumentNullException(nameof(uri));
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
        /// Specify the directory-based Certificate store. Directory will be created if it does not exist.
        /// </summary>
        /// <param name="path">The path to the local pki directory.</param>
        /// <param name="acceptAllRemoteCertificates">Set true to accept all remote certificates.</param>
        /// <param name="createLocalCertificateIfNotExist">Set true to create a local certificate and private key, if the files do not exist.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder SetDirectoryStore(string path, bool acceptAllRemoteCertificates = true, bool createLocalCertificateIfNotExist = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            this.certificateStore = new DirectoryStore(path, acceptAllRemoteCertificates, createLocalCertificateIfNotExist);
            return this;
        }

        /// <summary>
        /// Specify the <see cref="IUserIdentity"/>.
        /// </summary>
        /// <param name="identity">The user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder SetIdentity(IUserIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            this.identityProvider = endpoint => Task.FromResult(identity);
            return this;
        }

        /// <summary>
        /// Specify the <see cref="IUserIdentity"/>.
        /// </summary>
        /// <param name="identityProvider">An asynchronous function that provides the user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder SetIdentity(Func<EndpointDescription, Task<IUserIdentity>> identityProvider)
        {
            this.identityProvider = identityProvider ?? throw new ArgumentNullException(nameof(identityProvider));
            return this;
        }

        /// <summary>
        /// Specify the <see cref="ILoggerFactory"/>.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder SetLoggerFactory(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            return this;
        }

        /// <summary>
        /// Configure the <see cref="ILoggerFactory"/>.
        /// </summary>
        /// <param name="configureDelegate">A delegate that configures the LogggerFactory.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder ConfigureLoggerFactory(Action<ILoggerFactory> configureDelegate)
        {
            this.configureLoggerFactoryActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        /// <summary>
        /// Specify the <see cref="UaApplicationOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="UaApplicationOptions"/>.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder SetOptions(UaApplicationOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            return this;
        }

        /// <summary>
        /// Configure the <see cref="UaApplicationOptions"/>.
        /// </summary>
        /// <param name="configureDelegate">A delegate that configures the options.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder ConfigureOptions(Action<UaApplicationOptions> configureDelegate)
        {
            this.configureOptionsActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        /// <summary>
        /// Add a mapping between the requested url and corresponding <see cref="EndpointDescription"/>.
        /// </summary>
        /// <param name="requestedUrl">The url requested.</param>
        /// <param name="endpoint">The endpoint description to use.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder AddMappedEndpoint(string requestedUrl, EndpointDescription endpoint)
        {
            if (string.IsNullOrEmpty(requestedUrl))
            {
                throw new ArgumentNullException(nameof(requestedUrl));
            }

            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            this.mappedEndpoints.Add(new MappedEndpoint { RequestedUrl = requestedUrl, Endpoint = endpoint });
            return this;
        }

        /// <summary>
        /// Add a mapping between the requested url and corresponding <see cref="EndpointDescription"/>.
        /// </summary>
        /// <param name="requestedUrl">The url requested.</param>
        /// <param name="endpointUrl">The endpoint url to use.</param>
        /// <param name="securityPolicyUri">Optionally, the securityPolicyUri to use.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder AddMappedEndpoint(string requestedUrl, string endpointUrl, string securityPolicyUri = null)
        {
            if (string.IsNullOrEmpty(requestedUrl))
            {
                throw new ArgumentNullException(nameof(requestedUrl));
            }

            if (string.IsNullOrEmpty(endpointUrl))
            {
                throw new ArgumentNullException(nameof(endpointUrl));
            }

            this.mappedEndpoints.Add(new MappedEndpoint { RequestedUrl = requestedUrl, Endpoint = new EndpointDescription { EndpointUrl = endpointUrl, SecurityPolicyUri = securityPolicyUri } });
            return this;
        }

        /// <summary>
        /// Adds mappings between requested urls and corresponding <see cref="EndpointDescription"/>s.
        /// </summary>
        /// <remarks>
        /// Provide a appSettings.json file containing:
        /// {
        ///   "MappedEndpoints": [
        ///     {
        ///       "RequestedUrl": "opc.tcp://localhost:26543",
        ///       "Endpoint": {
        ///         "EndpointUrl": "opc.tcp://andrew-think:26543",
        ///         "SecurityPolicyUri": "http://opcfoundation.org/UA/SecurityPolicy#None"
        ///       }
        ///     }
        ///   ]
        /// }
        /// </remarks>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The <see cref="UaApplicationBuilder"/>.</returns>
        public UaApplicationBuilder AddMappedEndpoints(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var maps = configuration.GetSection("MappedEndpoints").Get<MappedEndpoint[]>();
            if (maps != null)
            {
                foreach (var map in maps)
                {
                    this.mappedEndpoints.Add(map);
                }
            }

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

            var loggerFactory = this.loggerFactory ?? new LoggerFactory();
            foreach (var action in this.configureLoggerFactoryActions)
            {
                action(loggerFactory);
            }

            var options = this.options ?? new UaApplicationOptions();
            foreach (var action in this.configureOptionsActions)
            {
                action(options);
            }

            return new UaApplication(
                this.localDescription,
                this.certificateStore,
                this.identityProvider,
                this.mappedEndpoints,
                loggerFactory,
                options);
        }
    }
}
