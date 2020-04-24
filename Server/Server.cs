using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Multiplay;

namespace UnityMultiplayerServer
{
    public delegate void ServerCallBack(Player client, byte[] data);
    public class CallBack
    {
        public Player Player;

        public byte[] Data;

        public ServerCallBack ServerCallBack;

        public CallBack(Player player, byte[] data, ServerCallBack serverCallBack)
        {
            Player = player;
            Data = data;
            ServerCallBack = serverCallBack;
        }

        public void Execute()
        {
            ServerCallBack(Player, Data);
        }
    }

    public class Room
    {
        public enum RoomState
        {
            Await,
            Gaming,
        }
        public string RoomId;
        public RoomState State = RoomState.Await;
        public const int MAX_PLAYER_AMOUNT = 6; // 最大玩家数量
        public List<Player> Players = new List<Player>();
        public Room(string roomId)
        {
            RoomId = roomId;
        }
        /// <summary>
        /// 关闭房间:从房间字典中移除并且所有房间中的玩家清除
        /// </summary>
        public void Close()
        {
            //所有玩家退出房间
            foreach (var each in Players)
            {
                each.ExitRoom();
            }
            Server.Rooms.Remove(RoomId);
        }
    }


    public static class Server
    {
        public static Dictionary<string, Room> Rooms;
        public static List<Player> Players;
        public static ConcurrentQueue<CallBack> _callBackQueue;
        public static Dictionary<MessageType, ServerCallBack> _callBacks = new Dictionary<MessageType, ServerCallBack>();

        private static Socket _serverSocket;
        
        public static void Start()
        {
            _callBackQueue = new ConcurrentQueue<CallBack>();
            Rooms = new Dictionary<string, Room>();
            Players = new List<Player>();
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, 60002));
            _serverSocket.Listen(0);
            Console.WriteLine("Unity Server is Started!");
            Thread tr = new Thread(_Await) { IsBackground = true };
            tr.Start();

            //开启回调方法线程
            Thread handle = new Thread(_Callback) { IsBackground = true };
            handle.Start();
        }
        private static void _Callback()
        {
            while(true)
            {
                if(_callBackQueue.Count > 0)
                {
                    if(_callBackQueue.TryDequeue(out CallBack callback))
                    {
                        callback.Execute();
                    }
                }
                Thread.Sleep(10);
            }
        }


        private static void _Await()
        {
            Socket client;
            while(true)
            {
                try
                {
                    client = _serverSocket.Accept();
                    Player player = new Player(client);
                    Players.Add(player);
                    Console.WriteLine(client.RemoteEndPoint.ToString() + "has connected");
                    Thread tr = new Thread(_Receive) { IsBackground = true};
                    tr.Start(player);

                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void _Receive(object o)
        {
            Player player = o as Player;
            Socket client = player.Socket;
            //持续接受消息
            while (true)
            {
                //解析数据包过程(服务器与客户端需要严格按照一定的协议制定数据包)
                byte[] data = new byte[4];

                int length = 0;                            //消息长度
                MessageType type = MessageType.None;       //类型
                int receive = 0;                           //接收信息

                try
                {
                    receive = client.Receive(data); //同步接受消息
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{client.RemoteEndPoint}已掉线:{ex.Message}");
                    player.Offline();
                    return;
                }

                //包头接收不完整
                if (receive < data.Length)
                {
                    Console.WriteLine($"{client.RemoteEndPoint}已掉线");
                    player.Offline();
                    return;
                }

                //解析消息过程
                using (MemoryStream stream = new MemoryStream(data))
                {
                    BinaryReader binary = new BinaryReader(stream, Encoding.UTF8); //UTF-8格式解析
                    try
                    {
                        length = binary.ReadUInt16();
                        type = (MessageType)binary.ReadUInt16();
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"{client.RemoteEndPoint}已掉线");
                        player.Offline();
                        return;
                    }
                }

                //如果有包体
                if (length - 4 > 0)
                {
                    data = new byte[length - 4];
                    receive = client.Receive(data);
                    if (receive < data.Length)
                    {
                        Console.WriteLine($"{client.RemoteEndPoint}已掉线");
                        player.Offline();
                        return;
                    }
                }
                else
                {
                    data = new byte[0];
                    receive = 0;
                }

                Console.WriteLine($"接受到消息, 房间数:{Rooms.Count}, 玩家数:{Players.Count}");

                //执行回调事件
                if (_callBacks.ContainsKey(type))
                {
                    CallBack callBack = new CallBack(player, data, _callBacks[type]);
                    //放入回调执行线程
                    _callBackQueue.Enqueue(callBack);
                }
            }
        }

        /// <summary>
        /// 服务器接受玩家请求失败时, 玩家掉线
        /// </summary>
        public static void Offline(this Player player)
        {
            //移除该玩家
            Players.Remove(player);

            //如果该玩家此时在线
            if (player.InRoom)
            {
                Rooms[player.RoomId].Close();
            }
        }
        /// <summary>
        /// 注册消息回调事件
        /// </summary>
        public static void Register(MessageType type, ServerCallBack method)
        {
            if (!_callBacks.ContainsKey(type))
                _callBacks.Add(type, method);
            else
                Console.WriteLine("注册了相同的回调事件");
        }
        /// <summary>
        /// 封装并发送信息
        /// </summary>
        public static void Send(this Player player, MessageType type, byte[] data = null)
        {
            //封装消息
            byte[] bytes = _Pack(type, data);

            //发送消息
            try
            {
                player.Socket.Send(bytes);
            }
            catch (Exception ex)
            {
                //客户端掉线
                Console.WriteLine(ex.Message);
                player.Offline();
            }
        }

        /// <summary>
        /// 封装数据
        /// </summary>
        private static byte[] _Pack(MessageType type, byte[] data = null)
        {
            MessagePacker packer = new MessagePacker();
            if (data != null)
            {
                packer.Add((ushort)(4 + data.Length)); //消息长度
                packer.Add((ushort)type);              //消息类型
                packer.Add(data);                      //消息内容
            }
            else
            {
                packer.Add(4);                         //消息长度
                packer.Add((ushort)type);              //消息类型
            }
            return packer.Package;
        }
    }
}


