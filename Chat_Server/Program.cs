using Chat_Server.Network;

namespace Chat_Server
{
    internal class Program
    {
        static NetworkServer network = new NetworkServer();

        static void Main(string[] args)
        {
            network.init();
            network.startListening();
        }
    }
}
