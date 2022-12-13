using System.Collections.Generic;
using UnityEngine;

namespace Witchslayer.Chat.ScriptableObjects
{

    [CreateAssetMenu(fileName = "ChatChannelData", menuName = "Witchslayer/Chat/Chat Channel Data")]
    public class ChatChannelData : ScriptableObject
    {

        public List<Data> ChannelData = new List<Data>();

        public Data GetChannelData(ChatChannel channel)
        {
            return ChannelData.Find(x => x.Channel == channel);
        }

        public Data GetChannelDataWithHint(string hint)
        {
            return ChannelData.Find(x => x.ChannelHint == hint);
        }

        [System.Serializable]
        public class Data
        {

            [Tooltip("Channel this data applies to.")]
            public ChatChannel Channel;

            [Tooltip("Icon used in channel dropdown menu, if any.")]
            public Sprite Icon;

            [Tooltip("Color for messages in this channel.")]
            public Color Color = Color.white;

            [Tooltip("Chat channel hints shown in the chat channel dropdown. " +
                "These can be typed by players to automatically switch to the given channel. " +
                "Ideally these are only one char long. If not, you may want to change the logic in OnInputValueChanged.")]
            public string ChannelHint;

            [Tooltip("True to enable chatting in this channel. Will also appear in channel dropdown menu.")]
            public bool Chattable = true;

        }

    }

}
