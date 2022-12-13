using FishNet.Connection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Witchslayer.Chat.Commands
{

    public abstract class ChatCommandBase
    {

        /// <summary>
        /// Command string to use when typing in chat to trigger this command.
        /// </summary>
        public abstract string[] CommandKeys { get; }

        /// <summary>
        /// Parses a command with the proper syntax. 
        /// Invokes cbResult with info about the parsing success or failure.
        /// If on server, pass the conn that called the command. Leave conn null if on client.
        /// </summary>
        /// <param name="commands"></param>
        /// <param name="cbResult"></param>
        /// <param name="conn"></param>
        public abstract void Parse(string[] commands, System.Action<string> cbResult=null, NetworkConnection conn = null);

    }

}
