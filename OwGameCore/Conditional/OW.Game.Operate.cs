using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace OW.Game.Conditional
{
    /// <summary>
    /// 
    /// </summary>
    public class GameOperate
    {
        public GameItemsReference Reference { get; set; }

        public decimal Count { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class GameItemsReference
    {
        public static bool TryParse(IDictionary<string, object> dic, [AllowNull] string prefix, out GameItemsReference result)
        {
            var coll = ((IReadOnlyDictionary<string, object>)dic).GetValuesWithoutPrefix(prefix);
            result = new GameItemsReference();
            return true;
        }


    }
}
