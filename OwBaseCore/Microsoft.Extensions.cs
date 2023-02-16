using Microsoft.Extensions.Internal;
using System;

namespace Microsoft.Extensions
{
    /// <summary>
    /// Provides access to the normal system clock.
    /// </summary>
    public class OwSystemClock : ISystemClock
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwSystemClock()
        {

        }

        /// <summary>
        /// Retrieves the current system time in UTC.
        /// </summary>
        public DateTimeOffset UtcNow
        {
            get
            {
                return DateTimeOffset.UtcNow;
            }
        }
    }

}