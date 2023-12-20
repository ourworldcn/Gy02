using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using OW.Game.Managers;
using OW.Game.Store.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Managers
{
    public class GameRedeemCodeManagerOptions : IOptions<GameRedeemCodeManagerOptions>
    {
        public GameRedeemCodeManagerOptions()
        {
        }

        public GameRedeemCodeManagerOptions Value => this;
    }

    /// <summary>
    /// 兑换码管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class GameRedeemCodeManager : GameManagerBase<GameRedeemCodeManagerOptions, GameRedeemCodeManager>
    {
        public GameRedeemCodeManager(IOptions<GameRedeemCodeManagerOptions> options, ILogger<GameRedeemCodeManager> logger, RedeemCodeGenerator redeemCodeGenerator) : base(options, logger)
        {
            _RedeemCodeGenerator = redeemCodeGenerator;
        }

        RedeemCodeGenerator _RedeemCodeGenerator;

        /// <summary>
        /// 生成码。
        /// </summary>
        /// <param name="count">生成的数量。</param>
        /// <param name="codeType">生成的码的类型，1=通用码，2=一次性码。</param>
        /// <param name="db"></param>
        /// <returns></returns>
        public List<string> Generat(int count, int codeType, DbContext db)
        {
            var result = new List<string>();
            var startColl = from redeemCode in db.Set<GameRedeemCode>()
                            join rdc in db.Set<GameRedeemCodeCatalog>()
                            on redeemCode.CatalogId equals rdc.Id
                            where rdc.CodeType == codeType
                            orderby redeemCode.Code.Substring(0, 3) descending
                            select redeemCode.Code;
            var start = startColl.FirstOrDefault();
            string prefix;
            if (string.IsNullOrEmpty(start))    //若没有生成
            {
                prefix = $"{(codeType == 1 ? 'V' : 'G')}{RedeemCodeGenerator.Chars[0]}{RedeemCodeGenerator.Chars[0]}";
            }
            else
                prefix = _RedeemCodeGenerator.NextPrefix(start);
            result.AddRange(_RedeemCodeGenerator.Generat(count, 8, prefix));
            return result;
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

        public readonly static char[] Chars = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', /*'I',*/ 'J', 'K', 'L', 'M', 'N', /*'O', */'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 
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
                var c = random.Next(0, Chars.Length);
                sb.Append(Chars[c]);
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
                    var c = random.Next(0, Chars.Length);
                    sb.Append(Chars[c]);
                }
                var str = sb.ToString();
                sb.Clear();
                if (!result.Add(str))   //若重复
                    i--;
            }
            return result;
        }

        /// <summary>
        /// 获取下一个前缀。
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public string NextPrefix(string prefix)
        {
            string result;
            var thrid = prefix[2];
            var index = Array.FindIndex(Chars, x => x == thrid);
            if (index == -1)
            {
                result = $"{prefix[0..2]}{Chars[0]}";
            }
            else if (index < Chars.Length - 1)
            {
                result = $"{prefix[0..2]}{Chars[index + 1]}";
            }
            else //若尾号达到末尾
            {
                var sec = prefix[1];
                index = Array.FindIndex(Chars, x => x == sec);
                result = $"{prefix[0]}{Chars[index + 1]}{Chars[0]}";
            }
            return result;
        }
    }
}
