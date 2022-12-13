using System.Collections.Generic;
using UnityEngine;

namespace Witchslayer.Chat.ScriptableObjects
{

    [CreateAssetMenu(fileName = "DefaultTabChannels", menuName = "Witchslayer/Chat/Default Tab Channels")]
    public class DefaultTabChannels : ScriptableObject
    {

        public List<TabChannels> TabChannels = new List<TabChannels>();

    }

}
