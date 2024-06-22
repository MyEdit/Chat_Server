using System.Threading;

namespace Chat_Server
{
    internal class User
    {
        private static Mutex mutex = new Mutex();
        private string nickName;

        public User(string nickName)        {
            this.nickName = nickName;
        }

        public string getNickName()
        {
            lock (mutex)
            {
                return nickName;
            }
        }
    }
}
