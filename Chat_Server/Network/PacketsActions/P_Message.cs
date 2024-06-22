using System.Collections.Generic;
using System.Net.Sockets;

namespace Chat_Server.Network.PacketsActions
{
    internal class P_Message
    {
        public static void onMessage(Socket clientSocket)
        {
            string prefix = "(" + NetworkServer.getNickNameOnSocket(clientSocket) + ") => ";
            string message = prefix + NetworkServer.getMessageFromClient(clientSocket);

            foreach (KeyValuePair<Socket, User> pair in NetworkServer.getConnections())
            {
                Socket userSocket = pair.Key;

                if (clientSocket.Equals(userSocket))
                    continue;

                NetworkServer.sendMessage(userSocket, PacketType.P_Message);
                NetworkServer.sendMessage(userSocket, message);
            }
        }
    }
}
