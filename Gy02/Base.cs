
using Gy02.Publisher;
using OW.SyncCommand;
using System;

namespace Gy02
{
    /// <summary>
    /// 
    /// </summary>
	public static class ReturnDtoExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="src"></param>
        public static void FillErrorFrom(this ReturnDtoBase obj, SyncCommandBase src)
        {
            obj.ErrorCode = src.ErrorCode;
            obj.DebugMessage = src.DebugMessage;
            obj.HasError = src.HasError;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        public static void FillErrorFromWorld(this ReturnDtoBase obj)
        {
            obj.ErrorCode = OwHelper.GetLastError();
            obj.DebugMessage = OwHelper.GetLastErrorMessage();
            obj.HasError = 0 != obj.ErrorCode;

        }


    }
}