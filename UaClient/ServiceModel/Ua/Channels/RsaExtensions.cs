// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.IO;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Workstation.ServiceModel.Ua.Channels
{
    public static class RsaExtensions
    {
        /// <summary>
        /// Encrypts IdentityToken data with the RSA algorithm.
        /// </summary>
        /// <returns>A byte array.</returns>
        public static byte[] EncryptTokenData(this RSA rsa, byte[] dataToEncrypt, string secPolicyUri)
        {
            int cipherTextBlockSize = rsa.KeySize / 8;
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
        public static void EncryptStream(this RSA rsa, Stream source, Stream target, string secPolicyUri)
        {
            int cipherTextBlockSize = rsa.KeySize / 8;
            int plainTextBlockSize;
            RSAEncryptionPadding padding;
            switch (secPolicyUri)
            {
                case SecurityPolicyUris.Basic128Rsa15:
                    plainTextBlockSize = Math.Max(cipherTextBlockSize - 11, 1);
                    padding = RSAEncryptionPadding.Pkcs1;
                    break;

                case SecurityPolicyUris.Basic256:
                    plainTextBlockSize = Math.Max(cipherTextBlockSize - 42, 1);
                    padding = RSAEncryptionPadding.OaepSHA1;
                    break;

                case SecurityPolicyUris.Basic256Sha256:
                    plainTextBlockSize = Math.Max(cipherTextBlockSize - 42, 1);
                    padding = RSAEncryptionPadding.OaepSHA1;
                    break;

                default:
                    plainTextBlockSize = 1;
                    padding = RSAEncryptionPadding.Pkcs1;
                    break;
            }

            if (source.Length % plainTextBlockSize != 0)
            {
                throw new ArgumentOutOfRangeException("source", "Source length is not an integral multiple of the plain text block size.");
            }

            byte[] plainTextBlock = new byte[plainTextBlockSize];

            while (source.Read(plainTextBlock, 0, plainTextBlockSize) > 0)
            {
                byte[] cipherTextBlock = rsa.Encrypt(plainTextBlock, padding);
                target.Write(cipherTextBlock, 0, cipherTextBlockSize);
            }
        }

        /// <summary>
        /// Decrypts IdentityToken data with the RSA algorithm.
        /// </summary>
        /// <returns>A byte array.</returns>
        public static byte[] DecryptTokenData(this RSA rsa, byte[] dataToDecrypt, string secPolicyUri)
        {
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
        public static void DecryptStream(this RSA rsa, Stream source, Stream target, string secPolicyUri)
        {
            int cipherTextBlockSize = (int)rsa.KeySize / 8;
            int plainTextBlockSize;
            RSAEncryptionPadding padding;
            switch (secPolicyUri)
            {
                case SecurityPolicyUris.Basic128Rsa15:
                    plainTextBlockSize = Math.Max(cipherTextBlockSize - 11, 1);
                    padding = RSAEncryptionPadding.Pkcs1;
                    break;

                case SecurityPolicyUris.Basic256:
                    plainTextBlockSize = Math.Max(cipherTextBlockSize - 42, 1);
                    padding = RSAEncryptionPadding.OaepSHA1;
                    break;

                case SecurityPolicyUris.Basic256Sha256:
                    plainTextBlockSize = Math.Max(cipherTextBlockSize - 42, 1);
                    padding = RSAEncryptionPadding.OaepSHA1;
                    break;

                default:
                    plainTextBlockSize = 1;
                    padding = RSAEncryptionPadding.Pkcs1;
                    break;
            }

            if (source.Length % cipherTextBlockSize != 0)
            {
                throw new ArgumentOutOfRangeException("source", "Source length is not an integral multiple of the cipher text block size.");
            }

            byte[] cipherTextBlock = new byte[cipherTextBlockSize];

            while (source.Read(cipherTextBlock, 0, cipherTextBlockSize) > 0)
            {
                byte[] plainTextBlock = rsa.Decrypt(cipherTextBlock, padding);
                target.Write(plainTextBlock, 0, plainTextBlockSize);
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