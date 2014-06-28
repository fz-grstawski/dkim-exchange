﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Security.Cryptography;
using System.IO;

using DkimSigner.RSA;
using Configuration.DkimSigner.Properties;

namespace ConfigurationSettings
{
    public class DomainElement
    {
        public string Domain { get; set; }
        public string Selector { get; set; }
        public string PrivateKeyFile { get; set; }

        public DomainElement()
        {
            
        }

        /// <summary>
        /// Domain element constructor
        /// </summary>
        public DomainElement(string domain, string selector, string privateKeyFile, string recipientRule = null, string senderRule = null)
        {
            Domain = domain;
            Selector = selector;
            PrivateKeyFile = privateKeyFile;
        }

        public override string ToString()
        {
            return Domain;
        }

        /// <summary>
        /// RSACryptoServiceProvider to manipulate to encrypt the information
        /// </summary>
        public RSACryptoServiceProvider CryptoProvider
        {
            get { return cryptoProvider; }
        }
        private RSACryptoServiceProvider cryptoProvider;

        /// <summary>
        /// Create the RSACryptoServiceProvider for the domain
        /// </summary>
        /// <param name="basePath">Path of the private key to open</param>
        /// <returns></returns>
        public bool initElement(string basePath)
        {
            string path;
            if (Path.IsPathRooted(PrivateKeyFile))
                path = @"keys\" + PrivateKeyFile;
            else
                path = Path.Combine(basePath, @"keys\" + PrivateKeyFile);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("PrivateKey for domain " + Domain + " not found: " + path);
            }

            byte[] key = File.ReadAllBytes(path);
            switch (RSACryptoHelper.GetFormatFromEncodedRsaPrivateKey(key))
            {
                case RSACryptoFormat.PEM:
                    string pemkey = File.ReadAllText(path, Encoding.ASCII);
                    cryptoProvider = RSACryptoHelper.GetProviderFromPemEncodedRsaPrivateKey(pemkey);
                    break;
                case RSACryptoFormat.DER:
                    cryptoProvider = RSACryptoHelper.GetProviderFromDerEncodedRsaPrivateKey(key);
                    break;
                case RSACryptoFormat.XML:
                    string xmlkey = File.ReadAllText(path, Encoding.ASCII);
                    cryptoProvider = RSACryptoHelper.GetProviderFromXmlEncodedRsaPrivateKey(xmlkey);
                    break;
                default:
                    throw new ArgumentException(
                            Resources.RSACryptHelper_UnknownFormat,
                            "encodedKey");
            }

            return true;
        }

        internal string Key
        {
            get { return string.Format("{0}|{1}", Selector, Domain); }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DkimSigner"/> class.
        /// </summary>
        ~DomainElement()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing,
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.cryptoProvider != null)
                {
                    this.cryptoProvider.Clear();
                    this.cryptoProvider = null;
                }
            }
        }
    }
}