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

        #region 新接入

        const string AppId = "1000172";

        const string AppKey = "2bc23c15abf368d01f719009e7b8dd333bc0c41e";
        /// <summary>
        /// 游戏支付解密使用。
        /// </summary>
        const string PublicKeyStringV2 = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCaXfg3WrMOZihl/w/gSAf7PtxB/jMwZRYt9j0itCOd54Vk1WBN52YrbRCfrOthWYdrBnUji60+min4vRy+cjhQxG5kyDIlE74Q3ipOjPfaeYSl9KtDfwZ5NZngGaovJJ1wEKZkTIUZPrFSX1RU0vRL1S9k2a8bGNi5YyYBCKc2iQIDAQAB";

        /// <summary>
        /// 游戏登录加密使用.
        /// </summary>
        const string AppPrivateKeyV2 = "MIICdQIBADANBgkqhkiG9w0BAQEFAASCAl8wggJbAgEAAoGBAJR8/L7gWBVCcOE5hYF469A8JJkgeoltPdrNwdDsspSP5r0nPZrIn1rPayt/l3cfXjx8PSinIz3QI0gJUhQpNq0niU9nvgpHW53WCM/RmCvfK8mF92wo+Vu74/VgT/ymQZzj0VpiKr+p5cyD8UHAwUOodJ79s7DoXyz32p3nNbw/AgMBAAECgYB0+XFyPPGm7cxW4SWXNVcvl/GM39UoZfKQZ/8DQzP7bNFsuXkCcoF23GekwMLliMSICad0WxacH1dr7EvIrh7vWlhYzfPE9/YatGXpUX3ZJ8LFGsQ8CqMC8va3REsElSyUafsSZSdFGs0zCJVDWJ/tpEdP+Xkg0svdrk+ADqRWwQJBAOStc6GvgefHWHYGeG0sMv774H17AIQ7C2ONQqKU9kbvdRS8pLriIfdT/hqj5iynNeTFjZfOrpTC8PcRBEsqrpkCQQCmOsoHzeX9+szy/1Fs1enQR85jYSeExn9ctrluH+W0WChKxP57x2y3dQmgnkgXi+41CBUkjiTMfw5Qanu6hsCXAkBkF//7D6Ve3IS99IsVzjjsHzfd9M7/EhEkHBrEq0s5NWscDo5UNtMDPUKGSqNffDk8z7PwdMk52DI9Ere8ZwxRAkBTCPDcfySQ/xQbmiAxbpWSPhxBlkloUMNUK85qzTIwKQ1PdCHu8MpExgjeG9LFOFfwU65ECWEmaZ1b3CUcIq3XAkAVMDlZu8LVQGNqrD3+lwh3I8g0bEwY2iI97TD6pagBEJn/Aoylh9YNvWRaILA2j/F4Wm6h+9Fbgy7vj09ToNV+";

        #endregion 新接入

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
