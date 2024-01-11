using GY02.Managers;
using GY02.Publisher;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OW.Game.Manager;
using OW.Game.Managers;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Base
{
    public class LoginNameGeneratorOptions : IOptions<LoginNameGeneratorOptions>
    {
        public LoginNameGeneratorOptions Value => this;

        /// <summary>
        /// 前缀。
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// 尾数位数。
        /// </summary>
        [Range(1, 8)]
        public int SuffixLength { get; set; } = 6;

        /// <summary>
        /// 尾数类型码int.ToString使用的类型码。仅支持X或D。
        /// </summary>
        [RegularExpression("[XD]")]
        public string SuffixMask { get; set; } = "X";
    }

    /// <summary>
    /// 2-6码登录名生成器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class LoginNameGenerator
    {
        public LoginNameGenerator(IOptions<LoginNameGeneratorOptions> options, IDbContextFactory<GY02UserContext> dbContextFactory)
        {
            _Options = options.Value;
            _DbContextFactory = dbContextFactory;
            _InitTask = Task.Run(Initialize);
        }

        LoginNameGeneratorOptions _Options;
        IDbContextFactory<GY02UserContext> _DbContextFactory;
        Task _InitTask;

        int _Current;

        /// <summary>
        /// 前缀。
        /// </summary>
        public string Prefix { get; }

        /// <summary>
        /// 初始化，
        /// TODO 要避免已经溢出的问题，如自己起名DVFFFFFF。
        /// </summary>
        private void Initialize()
        {
            using var db = _DbContextFactory.CreateDbContext();
            var totalLength = _Options.SuffixLength + _Options.Prefix.Length;
            var coll = db.VirtualThings.Where(c => c.ExtraGuid == ProjectContent.UserTId && c.ExtraString.Length == totalLength && c.ExtraString.StartsWith(_Options.Prefix))
                .OrderByDescending(c => c.ExtraString).Select(c => c.ExtraString.Substring(_Options.Prefix.Length));
            var style = _Options.SuffixMask == "X" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer;
            for (int i = 0; true; i += 1000)
            {
                var ary = coll.Skip(i).Take(1000).ToArray();
                if (ary.Length == 0) break;
                foreach (var item in ary)
                {
                    if (int.TryParse(item, style, default, out var suffix))
                    {
                        _Current = suffix;
                        return;
                    }
                }
            }
            _Current = 0;
        }

        public string GetNext()
        {
            _InitTask.Wait();
            return $"{_Options.Prefix}{Interlocked.Increment(ref _Current).ToString(_Options.SuffixMask + _Options.SuffixLength)}";
        }

    }
}
