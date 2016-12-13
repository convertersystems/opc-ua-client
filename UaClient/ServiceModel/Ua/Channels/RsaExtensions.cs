// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Workstation.ServiceModel.Ua.Channels
{
    public static class RsaExtensions
    {
        /// <summary>
        /// Encrypts IdentityToken data with the RSA algorithm.
        /// </summary>
        /// <returns>A byte array.</returns>
        public static byte[] EncryptTokenData(this RsaKeyParameters rsa, byte[] dataToEncrypt, string secPolicyUri)
        {
            if (rsa == null)
            {
                throw new ArgumentNullException(nameof(rsa));
            }

            int cipherTextBlockSize = rsa.Modulus.BitLength / 8;
            int plainTextBlockSize;
            switch (secPolicyUri)
            {
                case SecurityPolicyUris.Basic128Rsa15:
                    plainTextBlockSize = Math.Max(cipherTextBlockSize - 11, 1);
                    break;

                case SecurityPolicyUris.Basic256:
                    plainTextBlockSize = Math.Max(cipherTextBlockSize - 42, 1);
                    break;

                case SecurityPolicyUris.Basic256Sha256:
                    plainTextBlockSize = Math.Max(cipherTextBlockSize - 42, 1);
                    break;

                default:
                    plainTextBlockSize = 1;
                    break;
            }

            int blockCount = CeilingDivide(dataToEncrypt.Length + 4, plainTextBlockSize);
            int plainTextSize = blockCount * plainTextBlockSize;
            int cipherTextSize = blockCount * cipherTextBlockSize;

            // setup source
            var source = RecyclableMemoryStreamManager.Default.GetStream();
            var writer = new BinaryWriter(source);
            try
            {
                // encode length.
                writer.Write(dataToEncrypt.Length);

                // encode data.
                writer.Write(dataToEncrypt);

                // round up to multiple of plainTextBlockSize
                source.SetLength(plainTextSize);
                source.Seek(0L, SeekOrigin.Begin);

                // setup target
                byte[] cipherText = new byte[cipherTextSize];
                var target = new MemoryStream(cipherText, true);
                try
                {
                    rsa.EncryptStream(source, target, secPolicyUri);
                    return cipherText;
                }
                finally
                {
                    target.Dispose();
                }
            }
            finally
            {
                writer.Dispose();
            }
        }

        /// <summary>
        /// Encrypts a data stream with the RSA algorithm.
        /// </summary>
        public static void EncryptStream(this RsaKeyParameters rsa, Stream source, Stream target, string secPolicyUri)
        {
            if (rsa == null)
            {
                throw new ArgumentNullException(nameof(rsa));
            }

            int cipherTextBlockSize = rsa.Modulus.BitLength / 8;
            int plainTextBlockSize;
            IBufferedCipher encryptor;
            switch (secPolicyUri)
            {
                case SecurityPolicyUris.Basic128Rsa15:
                    encryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                    encryptor.Init(true, rsa);
                    plainTextBlockSize = Math.Max(cipherTextBlockSize - 11, 1);
                    break;

                case SecurityPolicyUris.Basic256:
                    encryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                    encryptor.Init(true, rsa);
                    plainTextBlockSize = Math.Max(cipherTextBlockSize - 42, 1);
                    break;

                case SecurityPolicyUris.Basic256Sha256:
                    encryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                    encryptor.Init(true, rsa);
                    plainTextBlockSize = Math.Max(cipherTextBlockSize - 42, 1);
                    break;

                default:
                    encryptor = null;
                    plainTextBlockSize = 1;
                    break;
            }

            if (source.Length % plainTextBlockSize != 0)
            {
                throw new ArgumentOutOfRangeException("source", "Source length is not an integral multiple of the plain text block size.");
            }

            byte[] plainTextBlock = new byte[plainTextBlockSize];

            if (encryptor != null)
            {
                while (source.Read(plainTextBlock, 0, plainTextBlockSize) > 0)
                {
                    byte[] cipherTextBlock = encryptor.DoFinal(plainTextBlock);
                    target.Write(cipherTextBlock, 0, cipherTextBlockSize);
                }
            }
            else
            {
                while (source.Read(plainTextBlock, 0, plainTextBlockSize) > 0)
                {
                    target.Write(plainTextBlock, 0, plainTextBlockSize);
                }
            }
        }

        /// <summary>
        /// Decrypts IdentityToken data with the RSA algorithm.
        /// </summary>
        /// <returns>A byte array.</returns>
        public static byte[] DecryptTokenData(this RsaKeyParameters rsa, byte[] dataToDecrypt, string secPolicyUri)
        {
            if (rsa == null)
            {
                throw new ArgumentNullException(nameof(rsa));
            }

            // setup source
            var source = new MemoryStream(dataToDecrypt, false);
            try
            {
                // setup target
                var target = RecyclableMemoryStreamManager.Default.GetStream();
                var reader = new BinaryReader(target);
                try
                {
                    rsa.DecryptStream(source, target, secPolicyUri);

                    // decode length.
                    target.Seek(0L, SeekOrigin.Begin);
                    var length = reader.ReadInt32();

                    // decode data.
                    byte[] plainText = reader.ReadBytes(length);

                    return plainText;
                }
                finally
                {
                    reader.Dispose();
                }
            }
            finally
            {
                source.Dispose();
            }
        }

        /// <summary>
        /// Decrypts a data stream with the RSA algorithm.
        /// </summary>
        public static void DecryptStream(this RsaKeyParameters rsa, Stream source, Stream target, string secPolicyUri)
        {
            if (rsa == null)
            {
                throw new ArgumentNullException(nameof(rsa));
            }

            int cipherTextBlockSize = rsa.Modulus.BitLength / 8;
            int plainTextBlockSize;
            IBufferedCipher decryptor;
            switch (secPolicyUri)
            {
                case SecurityPolicyUris.Basic128Rsa15:
                    decryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                    decryptor.Init(false, rsa);
                    plainTextBlockSize = Math.Max(cipherTextBlockSize - 11, 1);
                    break;

                case SecurityPolicyUris.Basic256:
                    decryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                    decryptor.Init(false, rsa);
                    plainTextBlockSize = Math.Max(cipherTextBlockSize - 42, 1);
                    break;

                case SecurityPolicyUris.Basic256Sha256:
                    decryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                    decryptor.Init(false, rsa);
                    plainTextBlockSize = Math.Max(cipherTextBlockSize - 42, 1);
                    break;

                default:
                    decryptor = null;
                    plainTextBlockSize = 1;
                    break;
            }

            if (source.Length % cipherTextBlockSize != 0)
            {
                throw new ArgumentOutOfRangeException("source", "Source length is not an integral multiple of the cipher text block size.");
            }

            byte[] cipherTextBlock = new byte[cipherTextBlockSize];

            if (decryptor != null)
            {
                while (source.Read(cipherTextBlock, 0, cipherTextBlockSize) > 0)
                {
                    byte[] plainTextBlock = decryptor.DoFinal(cipherTextBlock);
                    target.Write(plainTextBlock, 0, plainTextBlockSize);
                }
            }
            else
            {
                while (source.Read(cipherTextBlock, 0, cipherTextBlockSize) > 0)
                {
                    target.Write(cipherTextBlock, 0, cipherTextBlockSize);
                }
            }
        }

        private static int CeilingDivide(int dividend, int divisor)
        {
            int num = dividend / divisor;
            int rem = dividend % divisor;
            if (rem > 0)
            {
                num++;
            }

            return num;
        }
    }
}