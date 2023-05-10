using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OW.Game.Manager;
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
}
