using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class DictionaryStoreTests
    {
        [Fact]
        public void Constructor()
        {
            var store = new DirectoryStore("tmp");

            store.AcceptAllRemoteCertificates
                .Should().BeTrue();

            store.CreateLocalCertificateIfNotExist
                .Should().BeTrue();
        }

        [InlineData(null)]
        [InlineData("")]
        [Theory]
        public void ConstructorNull(string dir)
        {
            dir.Invoking(d => new DirectoryStore(d))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task InvalidApplicationDescription()
        {
            var store = new DirectoryStore("tmp");

            await store.Invoking(s => s.GetLocalCertificateAsync(null))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [InlineData(null)]
        [InlineData("https://hostname/appname")]
        [InlineData("urn://hostname/appname")]
        [InlineData("http://hostname/")]
        [InlineData("")]
        [InlineData("hostname:appname")]
        [Theory]
        public async Task InvalidApplicationUri(string uri)
        {
            var store = new DirectoryStore("tmp");

            var app = new ApplicationDescription
            {
                ApplicationUri = uri,
            };

            await store.Invoking(s => s.GetLocalCertificateAsync(app))
                .Should().ThrowAsync<ArgumentOutOfRangeException>();
        }
        
        [Fact]
        public async Task CreateNoCertificate()
        {
            var store = new DirectoryStore("nocert", createLocalCertificateIfNotExist:false);

            var app = new ApplicationDescription
            {
                ApplicationUri = "http://hostname/appname",
            };

            var (cert, key) = await store.GetLocalCertificateAsync(app);

            cert
                .Should().BeNull();
            key
                .Should().BeNull();
        }
        
        [Fact]
        public async Task CreateCertificate()
        {
            using (var dir = TempDirectory.Create())
            {
                var store = new DirectoryStore(dir.Name, createLocalCertificateIfNotExist: true);

                var app = new ApplicationDescription
                {
                    ApplicationUri = "http://hostname/appname",
                };

                var (cert, key) = await store.GetLocalCertificateAsync(app);

                cert
                    .Should().NotBeNull();
                key
                    .Should().NotBeNull();

                cert.SubjectDN.ToString()
                    .Should().Be("CN=appname,DC=hostname");
            }
        }
        
        [Fact]
        public async Task LoadCertificate()
        {
            using (var dir = TempDirectory.Create())
            {
                var store = new DirectoryStore(dir.Name, createLocalCertificateIfNotExist: true);

                var app = new ApplicationDescription
                {
                    ApplicationUri = "urn:hostname:appname",
                };

                var (cert1, key1) = await store.GetLocalCertificateAsync(app);
                var (cert2, key2) = await store.GetLocalCertificateAsync(app);

                cert1
                    .Should().Be(cert2);

                key1
                    .Should().Be(key2);
            }
        }
        
        [Fact]
        public async Task CertificateDirectoryStructure()
        {
            using (var dir = TempDirectory.Create())
            {
                var store = new DirectoryStore(dir.Name, createLocalCertificateIfNotExist: true);

                var app = new ApplicationDescription
                {
                    ApplicationUri = "http://hostname/appname",
                };

                await store.GetLocalCertificateAsync(app);

                Directory.EnumerateFiles(dir.Name + @"/own/certs")
                    .Should().HaveCount(1);

                Directory.EnumerateFiles(dir.Name + @"/own/private")
                    .Should().HaveCount(1);
            }
        }
        
        [Fact]
        public async Task ValidateCertificateAcceptAll()
        {
            using (var dir = TempDirectory.Create())
            {
                var store = new DirectoryStore(dir.Name, acceptAllRemoteCertificates: true);

                var ret = await store.ValidateRemoteCertificateAsync(null);

                ret
                    .Should().BeTrue();
            }
        }
        
        [Fact]
        public async Task ValidateCertificateNull()
        {
            using (var dir = TempDirectory.Create())
            {
                var store = new DirectoryStore(dir.Name, acceptAllRemoteCertificates: false);

                await store.Invoking(s => s.ValidateRemoteCertificateAsync(null))
                    .Should().ThrowAsync<ArgumentNullException>();
            }
        }

        [Fact]
        public async Task ValidateCertificateNotExisting()
        {
            using (var dirServer = TempDirectory.Create("Server"))
            using (var dirClient = TempDirectory.Create("Client", false))
            {
                var storeServer = new DirectoryStore(dirServer.Name, createLocalCertificateIfNotExist: true);
                var storeClient = new DirectoryStore(dirClient.Name, acceptAllRemoteCertificates: false);

                var server = new ApplicationDescription
                {
                    ApplicationUri = "http://hostname/server",
                };

                // First we create a certificate
                var (cert, _) = await storeServer.GetLocalCertificateAsync(server);

                // The certificate is not in the expected directory
                // hence it should not be accepted
                var ret = await storeClient.ValidateRemoteCertificateAsync(cert);
                ret
                    .Should().BeFalse();
                
                Directory.EnumerateFiles(dirClient.Name + @"/rejected")
                    .Should().HaveCount(1);
            }
        }

        [Fact]
        public async Task ValidateCertificateExisting()
        {
            using (var dirServer = TempDirectory.Create("Server"))
            using (var dirClient = TempDirectory.Create("Client"))
            {
                var storeServer = new DirectoryStore(dirServer.Name, createLocalCertificateIfNotExist: true);
                var storeClient = new DirectoryStore(dirClient.Name, acceptAllRemoteCertificates: false);

                var server = new ApplicationDescription
                {
                    ApplicationUri = "http://hostname/server",
                };

                // First we create a certificate
                var (cert, _) = await storeServer.GetLocalCertificateAsync(server);

                CopyAll(dirServer.Name + @"/own/certs", dirClient.Name + @"/trusted");

                // The certificate is now in the expected directory
                // hence it should be accepted
                var ret = await storeClient.ValidateRemoteCertificateAsync(cert);
                ret
                    .Should().BeTrue();
            }
        }
        
        private static void CopyAll(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (var file in Directory.EnumerateFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
            }
        }

        class TempDirectory : IDisposable
        {
            static public TempDirectory Create(string suffix = "", bool deleteOnDispose = true, [CallerMemberName]string name = null)
            {
                var path = name + suffix;
                DeleteRecursive(path);
                return new TempDirectory(path, deleteOnDispose);
            }

            static private void DeleteRecursive(string name)
            {
                if (Directory.Exists(name))
                {
                    Directory.Delete(name, recursive: true);
                }
            }

            public string Name { get; }
            public bool DeleteOnDispose { get; }

            private TempDirectory(string name, bool deleteOnDispose)
            {
                this.Name = name;
                this.DeleteOnDispose = deleteOnDispose;
            }

            public void Dispose()
            {
                if (this.DeleteOnDispose)
                {
                    DeleteRecursive(this.Name);
                }
            }
        }
    }
}
