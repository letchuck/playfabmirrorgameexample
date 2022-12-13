using System.Collections.Generic;
using UnityEngine;

namespace Witchslayer.Chat.ProfanityFilters
{

    /// <summary>
    /// Example profanity filter that reads profanity from a text file, caches it, and uses it for filtering.
    /// </summary>
    public class TextFileProfanityFilter : ProfanityFilterBase
    {

        private const char CENSOR_CHAR = '*';

        [SerializeField] private TextAsset _file = null;
        private List<string> _profanityList;

        private void Awake()
        {
            // This assumes it is a CSV file
            _profanityList = new List<string>(_file.text.Split(','));
            Debug.Log($"Profanity list populated with {_profanityList.Count} words.");
        }

        // Always invoke the callback if you want the chat to proceed to the next processing step.
        public override void Filter(string input, System.Action<string> callback)
        {
            if (_profanityList == null || _profanityList.Count== 0)
            {
                callback.Invoke(input);
                return;
            }

            string[] words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                // is this word profane?
                if (_profanityList.Contains(words[i]))
                {
                    // replace it with censor chars
                    words[i] = new string(CENSOR_CHAR, words[i].Length);
                }
            }

            callback.Invoke(string.Join(' ', words));
        }

    }

}
