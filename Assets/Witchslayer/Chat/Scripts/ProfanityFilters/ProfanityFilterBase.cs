using System;

namespace Witchslayer.Chat.ProfanityFilters
{

    /// <summary>
    /// Base profanity filter class from which all filters must derive from.
    /// For async filters (i.e. using web API), inherit from AsyncProfanityFilterBase instead.
    /// </summary>
    public abstract class ProfanityFilterBase : ServerChatProcessorComponent
    {

        /// <summary>
        /// Filters the input string for profanity.
        /// </summary>
        /// <param name="input"></param>String to filter for profanity.
        /// <param name="callback"></param>Callback invoked with the filtered string.
        public abstract void Filter(string input, Action<string> callback);

    }

}
