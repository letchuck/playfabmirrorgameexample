using FishNet.Connection;
using System;

namespace Witchslayer.Chat.MuteCheckers
{

    public abstract class WhisperMuteCheckerBase : ServerChatProcessorComponent
    {

        public abstract void IsMuted(NetworkConnection senderConn, NetworkConnection receiverConn, Action<bool> isMutedCallback);

    }

}
