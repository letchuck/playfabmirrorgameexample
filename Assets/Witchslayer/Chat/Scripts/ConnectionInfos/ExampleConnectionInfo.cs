//using FishNet;
//using FishNet.Connection;

//namespace Witchslayer.Chat.ConnectionInfos
//{

//    /// <summary>
//    /// A very simplistic example using the connection's first object to get the username. 
//    /// This may be your player prefab spawned for this conn in a Player script or however you like to do it.
//    /// </summary>
//    public class ExampleConnectionInfo : ConnectionInfoBase
//    {
//        public override void GetConnection(string username, System.Action<NetworkConnection> cbConn)
//        {
//            foreach (var item in InstanceFinder.ServerManager.Clients.Values)
//            {
//                if (item.Authenticated && item.FirstObject.GetComponent<Player>().Username == username)
//                {
//                    cbConn.Invoke(item);
//                    return;
//                }
//            }

//            cbConn.Invoke(null);
//        }

//        public override void GetUsername(NetworkConnection conn, System.Action<string> cbUsername)
//        {
//            cbUsername.Invoke(conn.FirstObject.GetComponent<Player>().Username);
//        }

//    }

//}
