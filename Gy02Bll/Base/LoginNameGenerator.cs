using GY02.Managers;
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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GY02.Base
{
    public class LoginNameGenerator
    {
        public LoginNameGenerator() { }

        private bool _QuicklyRegisterSuffixSeqInit;
        private int _QuicklyRegisterSuffixSeq;

        [MethodImpl(MethodImplOptions.Synchronized)]
        int GetQuicklyRegisterSuffixSeq()
        {
            if (!_QuicklyRegisterSuffixSeqInit)
            {
                using var db = VWorld.CreateNewUserDbContext();
                var maxSeqStr = db.VirtualThings.Where(c => c.ExtraString.StartsWith("gy") && EF.Functions.IsNumeric(c.ExtraString.Substring(2)))
                    .OrderByDescending(c => c.ExtraString.Length).ThenByDescending(c => c.ExtraString).FirstOrDefault()?.ExtraString ?? "0";
                var len = maxSeqStr.Reverse().TakeWhile(c => char.IsDigit(c)).Count();
                _QuicklyRegisterSuffixSeq = int.Parse(maxSeqStr[^len..^0]);
                _QuicklyRegisterSuffixSeqInit = true;
            }
            return Interlocked.Increment(ref _QuicklyRegisterSuffixSeq);
        }

        public string Generate()
        {
            return $"gy{GetQuicklyRegisterSuffixSeq()}";
        }
    }

    public class LoginName26GeneratorOptions : IOptions<LoginName26GeneratorOptions>
    {
        /// <summary>
        /// 前缀。
        /// </summary>
        public string Prefix { get; set; }

        public LoginName26GeneratorOptions Value => this;
    }

    /// <summary>
    /// 2-6码登录名生成器。
    /// </summary>
    public class LoginName26Generator
    {
        public LoginName26Generator(IOptions<LoginName26GeneratorOptions> options)
        {
            _Options = options.Value;
        }

        private bool _QuicklyRegisterSuffixSeqInit;
        private int _QuicklyRegisterSuffixSeq;
        LoginName26GeneratorOptions _Options;
        int _Current = 0;

        /// <summary>
        /// 前缀。
        /// </summary>
        public string Prefix { get; }

        [MethodImpl(MethodImplOptions.Synchronized)]
        int GetQuicklyRegisterSuffixSeq()
        {
            if (!_QuicklyRegisterSuffixSeqInit)
            {
                using var db = VWorld.CreateNewUserDbContext();
                var maxSeqStr = db.VirtualThings.Where(c => c.ExtraString.StartsWith("gy") && EF.Functions.IsNumeric(c.ExtraString.Substring(2)))
                    .OrderByDescending(c => c.ExtraString.Length).ThenByDescending(c => c.ExtraString).FirstOrDefault()?.ExtraString ?? "0";
                var len = maxSeqStr.Reverse().TakeWhile(c => char.IsDigit(c)).Count();
                _QuicklyRegisterSuffixSeq = int.Parse(maxSeqStr[^len..^0]);
                _QuicklyRegisterSuffixSeqInit = true;
            }
            return Interlocked.Increment(ref _QuicklyRegisterSuffixSeq);
        }

        public string GetNext()
        {
            return $"{_Options.Prefix}{Interlocked.Increment(ref _QuicklyRegisterSuffixSeq)}";
        }

        public string Generate()
        {
            return $"gy{GetQuicklyRegisterSuffixSeq()}";
        }
    }

    public static class LoginName26GeneratorExtensions
    {
        public static IServiceCollection AddLoginName26Generator(this IServiceCollection services)
        {
            services.TryAddSingleton<LoginName26Generator, LoginName26Generator>();
            return services;
        }
    }
}
