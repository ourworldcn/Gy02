using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Game.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GY02.Managers
{
    public class T304ManagerOptions : IOptions<T304ManagerOptions>
    {
        public T304ManagerOptions Value => this;
    }

    /// <summary>
    /// 完美 北美接入管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class T304Manager : GameManagerBase<T304ManagerOptions, T304Manager>
    {
        public T304Manager(IOptions<T304ManagerOptions> options, ILogger<T304Manager> logger) : base(options, logger)
        {
        }

        /// <summary>
        /// 公钥。
        /// </summary>
        const string PublicKeyString = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAmfKkGYVh4fh85D3gInPQgbG6VsIKJqg2mgGQBm+0ETRk+4C3svLR8UZuz+MoFCmtXaYn7DpCVI4XZiWYhjqnYXrSwgBGrGMk7hKx1VAxGYpG9Uv28ZBGxr0i/dikctyeF/uU0p4p2qEFBn9LILXyhj3meOqc5zWRwA2ikxszXmwiS16jbE+L6YddF7hocqgE6ZjRBcDxJep0+bkjhyx4oCPf4rzUrrqiqE2PU+32+fzmzIq46mDjc7qEhIIUhaWViggfLpv74JHGOTGN+7Gm7ZYrU3V/Rvv0W5dowesVOoQ8L5NQD8pD8741J0sxEOBaAxlyYSqxgnZuFIZapPjL2wIDAQAB";

        /// <summary>
        /// 公钥的二进制形式。
        /// </summary>
        static readonly byte[] PublicKey = Convert.FromBase64String(PublicKeyString);

        public byte[] Decrypt(byte[] data)
        {
            using var rsa = RSA.Create("RSA");
            rsa.ImportSubjectPublicKeyInfo(PublicKey, out _);
            rsa.ExportParameters(false);
            return rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA1);
        }

        /// <summary>
        /// 校验签名。
        /// </summary>
        /// <param name="data"></param>
        /// <param name="signature"></param>
        /// <returns></returns>
        public bool Verify(string data, string signature)
        {
            using var rsa = RSA.Create("RSA");
            rsa.ImportSubjectPublicKeyInfo(PublicKey, out _);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signBytes = Convert.FromBase64String(signature);

            var result = rsa.VerifyData(dataBytes, signBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

            return result;
        }
    }
}
