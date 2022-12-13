using UnityEngine;

namespace Witchslayer.Chat
{

    public class ServerChatProcessorComponent : MonoBehaviour
    {

        protected ServerChatProcessor serverChatProcessor;

        private void Awake()
        {
            serverChatProcessor = GetComponent<ServerChatProcessor>();
        }

    }

}
