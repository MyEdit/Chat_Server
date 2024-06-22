using System;
using System.Net.Sockets;

namespace Chat_Server.Network.PacketsActions
{
    internal class P_Authorization
    {
        public static void auth(Socket clientSocket)
        {
            string nickName = NetworkServer.getMessageFromClient(clientSocket);
            User user = new User(nickName);
            NetworkServer.addConnection(clientSocket, user);
            Console.WriteLine("[INFO]: Client " + nickName + " successfully logged");
        }
    }
}
