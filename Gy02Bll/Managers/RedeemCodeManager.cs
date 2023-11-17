using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using OW.Game.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Managers
{
    public class RedeemCodeManagerOptions : IOptions<RedeemCodeManagerOptions>
    {
        public RedeemCodeManagerOptions()
        {
        }

        public RedeemCodeManagerOptions Value => this;
    }

    /// <summary>
    /// 兑换码管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class RedeemCodeManager : GameManagerBase<RedeemCodeManagerOptions, RedeemCodeManager>
    {
        public RedeemCodeManager(IOptions<RedeemCodeManagerOptions> options, ILogger<RedeemCodeManager> logger) : base(options, logger)
        {
        }
    }

    /// <summary>
    /// 兑换码生成器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class RedeemCodeGenerator
    {
        public RedeemCodeGenerator()
        {

        }

        readonly static char[] chars = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', /*'I',*/ 'J', 'K', 'L', 'M', 'N', /*'O', */'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 
        /*'0','1',*/'2','3','4','5','6','7','8','9'};

        /// <summary>
        /// 生产一个兑换码。
        /// </summary>
        /// <param name="length">长度，不包括前缀。</param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public string Generat(int length, string prefix = "")
        {
            Random random = new Random();
            var sb = AutoClearPool<StringBuilder>.Shared.Get();
            using var dw = DisposeHelper.Create(c => AutoClearPool<StringBuilder>.Shared.Return(c), sb);
            sb.Append(prefix);
            for (int i = 0; i < length; i++)
            {
                var c = random.Next(0, chars.Length);
                sb.Append(chars[c]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 生成一组不重复的兑换码
        /// </summary>
        /// <param name="count">生成数量</param>
        /// <param name="length">每个兑换码的长度，不包括前缀。</param>
        /// <param name="prefix">前缀。</param>
        /// <returns></returns>
        public IEnumerable<string> Generat(int count, int length, string prefix = "")
        {
            HashSet<string> result = new HashSet<string>();
            Random random = new Random();
            var sb = AutoClearPool<StringBuilder>.Shared.Get();
            using var dw = DisposeHelper.Create(c => AutoClearPool<StringBuilder>.Shared.Return(c), sb);

            for (int i = 0; i < count; i++)
            {
                sb.Append(prefix);
                for (int j = 0; j < length; j++)
                {
                    var c = random.Next(0, chars.Length);
                    sb.Append(chars[c]);
                }
                var str = sb.ToString();
                sb.Clear();
                if (!result.Add(str))   //若重复
                    i--;
            }
            return result;
        }
    }
}
