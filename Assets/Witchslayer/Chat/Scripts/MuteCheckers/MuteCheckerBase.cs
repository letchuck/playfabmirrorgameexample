using FishNet.Connection;
using System;
using UnityEngine;

namespace Witchslayer.Chat.MuteCheckers
{

    public abstract class MuteCheckerBase : ServerChatProcessorComponent
    {

        public abstract void IsMuted(NetworkConnection conn, 
            ChatChannel channel, Action<bool> isMutedCallback);

    }

}
