using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Witchslayer.Chat.ProfanityFilters
{

    /// <summary>
    /// Example profanity filter using an async API: https://rapidapi.com/neutrinoapi/api/bad-word-filter/
    /// </summary>
    public class NeutrinoAPIProfanityFilter : ProfanityFilterBase
    {

        private const string URL = "https://neutrinoapi-bad-word-filter.p.rapidapi.com/bad-word-filter";

        private const string HEADER_KEY = "X-RapidAPI-Key";
        private const string HEADER_HOST = "X-RapidAPI-Host";

        /// <summary>
        /// You must sign up for a key at the website above. It's quick but very limited unless you buy a plan.
        /// </summary>
        private const string KEY = "YOUR-KEY";
        private const string HOST = "neutrinoapi-bad-word-filter.p.rapidapi.com";

        private const string CONTENT = "content";
        private const string CENSOR_CHAR = "censor-character";

        public override void Filter(string input, Action<string> callback)
        {
            StartCoroutine(FilterAsync(input, callback));
        }

        private IEnumerator FilterAsync(string input, Action<string> callback)
        {
            WWWForm form = new WWWForm();
            form.AddField(CONTENT, input);
            // If you don't provide a censor character, it wont return a censored string.
            form.AddField(CENSOR_CHAR, "*");

            using (UnityWebRequest request = UnityWebRequest.Post(URL, form))
            {
                request.SetRequestHeader(HEADER_KEY, KEY);
                request.SetRequestHeader(HEADER_HOST, HOST);

                var async = request.SendWebRequest();
                yield return async;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // this particular API uses kebab-case, so we need to convert it to be C# serializable
                    string converted = request.downloadHandler.text.Replace('-', '_');
                    RequestContent content = JsonUtility.FromJson<RequestContent>(converted);

                    // Make sure to invoke the censored string so the server can broadcast the message
                    callback?.Invoke(content.censored_content);
                }
            }
        }

        [Serializable]
        private class RequestContent
        {
            public string censored_content;
            public bool is_bad;
            public string[] bad_words_list;
            public int bad_words_total;
        }

    }

}
