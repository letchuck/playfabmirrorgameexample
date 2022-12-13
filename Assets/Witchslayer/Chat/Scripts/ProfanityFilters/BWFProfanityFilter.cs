// CrossTales BadWordFilter support
#if CT_BWF

using Crosstales.BWF;
using System;

namespace Witchslayer.Chat.ProfanityFilters
{

    /// <summary>
    /// Example profanity filter using CrossTales' BadWordFilter.
    /// </summary>
    public class BWFProfanityFilter : ProfanityFilterBase
    {

        public override void Filter(string input, Action<string> callback)
        {
            callback.Invoke(BWFManager.ReplaceAll(input));
        }

    }

}

#endif