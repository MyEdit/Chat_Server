using Chat_Server.Network.PacketsActions;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Chat_Server.Network
{
    public enum PacketType //Типы пакетов
    {
        P_Message,
        P_Authorization,
        P_QueryWithoutResponce, //Для примера
        P_Query //Для примера
    }

    internal class NetworkServer
    {
        private static string ip = "127.0.0.1";
        private static int port = 1111;
        private static Socket serverSocket; //Сокет сервера
        private static ConcurrentDictionary<Socket, User> connections = new ConcurrentDictionary<Socket, User>(); //Я использую конкурентную коллекцию которая является потокобезопасной, для студентов лучше использовать обычную мапу и играться с мьютексами чтобы они сами это дело сделали, поспотыкались об гонку данных и дедлоки

        public void init()
        {
            IPEndPoint adress = new IPEndPoint(IPAddress.Parse(ip), port); //Объект адреса сервера
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //Создание сокета сервера и его настрока работы

            serverSocket.Bind(adress); //Привязка адреса сервера к сокету сервера
        }

        public void startListening()
        {
            if (serverSocket == null)
                throw new InvalidOperationException("init method must be called before startListening.");

            serverSocket.Listen(10); //Установка максимальной длины очереди ожидающих подключений
            Console.WriteLine("[INFO]: Listening port started successfully!");

            while (true)
            {
                Socket clientSocket = serverSocket.Accept(); //Подключение клиентского сокета
                Console.WriteLine("[INFO]: Client [" + clientSocket.RemoteEndPoint.ToString() + "] connected");
                Task.Run(() => clientHandler(clientSocket)); //Запускаем для каждого из клиентов отдельный поток обработки
            }
        }

        private void clientHandler(Socket clientSocket)
        {
            while (true)
            {
                try
                {
                    packetHandler(getMessageFromClient<PacketType>(clientSocket), clientSocket); //Переходим в обработчик пакетов
                }
                catch (SocketException) //Вызывается при отключении клиента от сервера (Другого способа не нашел, но и не сильно долго искал, в идеале избавиться от try catch)
                {
                    onClientDisconnected(clientSocket);
                    return;
                }
            }
        }

        private void packetHandler(PacketType packetType, Socket clientSocket) //Тут вроде по кейсу все ясно и понятно
        {
            switch (packetType)
            {
                case PacketType.P_Message:
                {
                    P_Message.onMessage(clientSocket);
                    break;
                }
                case PacketType.P_Authorization:
                {
                    P_Authorization.auth(clientSocket);
                    break;
                }
                case PacketType.P_Query:
                {
                    //Выполняем запрос в БД и возвращаем ответ
                    break;
                }
                case PacketType.P_QueryWithoutResponce:
                {
                    //Выполняем запрос в БД без ответа клиенту
                    break;
                }
                default:
                {
                    Console.WriteLine("[WARN]: Client send unknown packettype");
                    break;
                }
            }
        }

        private void onClientDisconnected(Socket clientSocket)
        {
            Console.WriteLine("[INFO]: Client [" + clientSocket.RemoteEndPoint.ToString() + "] disconnected");
            clientSocket.Close(); //Закрываем клиентский сокет который отключился от сервера
            connections.TryRemove(clientSocket, out _); //Удаляем значение в коллекции всех подключеных клиентов отключившегося клиента
        }

        public static void addConnection(Socket clientSocket, User user)
        {
            connections[clientSocket] = user; //Добавляем в коллекцию всех подключеных клиентов нового клиента
        }

        public static ConcurrentDictionary<Socket, User> getConnections()
        {
            return connections;
        }

        public static string getNickNameOnSocket(Socket clientSocket)
        {
            if (connections.TryGetValue(clientSocket, out User user))
                return user.getNickName();

            return null;
        }

        //Шаблонная функция чтения пакета от клиента
        public static T getMessageFromClient<T>(Socket clientSocket)
        {
            byte[] buffer;

            //Создаем набор байтов по размеру типа данных T
            if (typeof(T).IsEnum)
                buffer = new byte[sizeof(int)];
            else
                buffer = new byte[Marshal.SizeOf<T>()];

            clientSocket.Receive(buffer); //Читаем и записываем данные из пакета в buffer
            return byteArrayToObject<T>(buffer); //Кастуем набор байтов в тип данных T и возвращаем его вызывающему
        }

        //Функция чтения строки в пакете от клиента
        public static string getMessageFromClient(Socket clientSocket)
        {
            int messageSize;
            string message;

            byte[] sizeBuffer = new byte[sizeof(int)];
            clientSocket.Receive(sizeBuffer);
            messageSize = BitConverter.ToInt32(sizeBuffer, 0);

            byte[] stringBuffer = new byte[messageSize];
            clientSocket.Receive(stringBuffer);
            message = Encoding.UTF8.GetString(stringBuffer);

            return message;
        }

        //Отправка пакета клиенту
        public static void sendMessage<T>(Socket clientSocket, T message)
        {
            clientSocket.Send(objectToByteArray(message));
        }

        //Функция отправки строки кленту
        public static void sendMessage(Socket clientSocket, string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);

            clientSocket.Send(lengthBytes);
            clientSocket.Send(messageBytes);
        }

        //Сериализатор. Серелиализует объекты string, int, double, Emun в набор байтов
        private static byte[] objectToByteArray<T>(T obj)
        {
            if (obj is string)
            {
                return Encoding.UTF8.GetBytes(obj as string);
            }

            if (typeof(T) == typeof(int))
            {
                return BitConverter.GetBytes((int)(object)obj);
            }

            if (typeof(T) == typeof(double))
            {
                return BitConverter.GetBytes((double)(object)obj);
            }

            if (typeof(T).IsEnum)
            {
                return BitConverter.GetBytes(Convert.ToInt32(obj));
            }

            throw new InvalidOperationException("Unsupported type");
        }

        //Десериализатор. Десерелиализует набор байтов в объекты string, int, double, Emun
        private static T byteArrayToObject<T>(byte[] byteArray)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)Encoding.UTF8.GetString(byteArray);
            }

            if (typeof(T) == typeof(int))
            {
                return (T)(object)BitConverter.ToInt32(byteArray, 0);
            }

            if (typeof(T) == typeof(double))
            {
                return (T)(object)BitConverter.ToDouble(byteArray, 0);
            }

            if (typeof(T).IsEnum)
            {
                return (T)Enum.ToObject(typeof(T), BitConverter.ToInt32(byteArray, 0));
            }

            throw new InvalidOperationException("Unsupported type");
        }
    }
}
