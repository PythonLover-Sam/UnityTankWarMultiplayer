using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Multiplay;

/// <summary>
/// 回调委托
/// </summary>
public delegate void CallBack(byte[] data);


public class Network : MonoBehaviour
{
    #region 网络配置
    public static Network network;
    public static bool isConnected;
    private static TcpClient _client;
    public static NetworkStream _stream;
    //消息类型与回调字典
    private static Dictionary<MessageType, CallBack> _callBacks = new Dictionary<MessageType, CallBack>();
    //心跳包机制
    private const float HEARTBEAT_TIME = 3;         //心跳包发送间隔时间
    private static float _timer = HEARTBEAT_TIME;   //距离上次接受心跳包的时间
    public static bool Received = true;             //收到心跳包回信
    //待发送消息队列
    public static Queue<byte[]> _messages = new Queue<byte[]>();

    public void Awake()
    {
        if (network == null)
        {
            network = this;
            isConnected = false;
            DontDestroyOnLoad(this);
            StartCoroutine(ConnectServer());

            //设置异步发送消息
            StartCoroutine(_Send());
            //设置异步接收消息
            StartCoroutine(_Receive());
        }

    }

    public void Start()
    {
        Register(MessageType.HeartBeat, _HeartBeat);
        Register(MessageType.CreateRoomRequest, _CreateRoom);
        Register(MessageType.ChangeNickNameRequest, _ChangeNickName);
        Register(MessageType.EnterRoomRequest, _EnterRoom);
        Register(MessageType.ExitRoomRequest, _ExitRoom);
        Register(MessageType.StartGameRequest, _StartGame);
        Register(MessageType.PlayerTankUpdateRequest, _PlayerTankUpdateRequest);
    }

    public static IEnumerator ConnectServer()
    {
        _client = new TcpClient();
        //异步连接
        IAsyncResult async = _client.BeginConnect("101.200.220.209", 60002, null, null);
        while (!async.IsCompleted)
        {
            Debug.Log("连接服务器中");
            yield return null;
        }

        try
        {
            _client.EndConnect(async);
        }
        catch (Exception ex)
        {
            isConnected = false;
            yield break;
        }
        //获取通信流
        try
        {
            _stream = _client.GetStream();
        }
        catch (Exception ex)
        {
            isConnected = false;
            yield break;
        }
        if (_stream == null)
        {
            isConnected = false;
            yield break;
        }

        // 连接成功
        isConnected = true;
    }

    private static IEnumerator _Send()
    {
        while (true)
        {
            //持续发送消息
            while (isConnected)
            {
                _timer += Time.deltaTime;
                //有待发送消息
                if (_messages.Count > 0)
                {
                    byte[] data = _messages.Dequeue();
                    yield return _Write(data);
                }

                //心跳包机制(每隔一段时间向服务器发送心跳包)
                if (_timer >= HEARTBEAT_TIME)
                {
                    //如果没有收到上一次发心跳包的回复
                    if (!Received)
                    {
                        isConnected = false;  // 掉线
                        Debug.Log("心跳包接受失败,断开连接");
                        yield break;
                    }
                    _timer = 0;
                    //封装消息
                    byte[] data = _Pack(MessageType.HeartBeat);
                    //发送消息
                    yield return _Write(data);

                    Debug.Log("已发送心跳包");
                }
                yield return null; //防止死循环
            }
            while (!isConnected)
                yield return new WaitForSeconds(1);
        }
    }

    private static IEnumerator _Receive()
    {
        while (true)
        {
            //持续接受消息
            while (isConnected)
            {
                //解析数据包过程(服务器与客户端需要严格按照一定的协议制定数据包)
                byte[] data = new byte[4];

                int length;         //消息长度
                MessageType type;   //类型
                int receive = 0;    //接收长度

                //异步读取
                IAsyncResult async = _stream.BeginRead(data, 0, data.Length, null, null);
                while (!async.IsCompleted)
                {
                    yield return null;
                }
                //异常处理
                try
                {
                    receive = _stream.EndRead(async);
                }
                catch (Exception ex)
                {
                    isConnected = false;
                    Debug.Log("消息包头接收失败:");
                    yield break;
                }
                if (receive < data.Length)
                {
                    isConnected = false;
                    Debug.Log("消息包头接收失败:");
                    yield break;
                }

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
                        isConnected = false;
                        Debug.Log("消息包头接收失败:");
                        yield break;
                    }
                }

                //如果有包体
                if (length - 4 > 0)
                {
                    data = new byte[length - 4];
                    //异步读取
                    async = _stream.BeginRead(data, 0, data.Length, null, null);
                    while (!async.IsCompleted)
                    {
                        yield return null;
                    }
                    //异常处理
                    try
                    {
                        receive = _stream.EndRead(async);
                    }
                    catch (Exception ex)
                    {
                        isConnected = false;
                        Debug.Log("消息包头接收失败:");
                        yield break;
                    }
                    if (receive < data.Length)
                    {
                        isConnected = false;
                        Debug.Log("消息包头接收失败:");
                        yield break;
                    }
                }
                //没有包体
                else
                {
                    data = new byte[0];
                    receive = 0;
                }

                if (_callBacks.ContainsKey(type))
                {
                    //执行回调事件
                    CallBack method = _callBacks[type];
                    method(data);
                }
                else
                {
                    Debug.Log("未注册该类型的回调事件");
                }
            }
            while (!isConnected)
                yield return new WaitForSeconds(1);
        }
    }

    private static IEnumerator _Write(byte[] data)
    {
        //如果服务器下线, 客户端依然会继续发消息
        if (!isConnected || _stream == null)
        {
            yield break;
        }

        //异步发送消息
        IAsyncResult async = _stream.BeginWrite(data, 0, data.Length, null, null);
        while (!async.IsCompleted)
        {
            yield return null;
        }
        //异常处理
        try
        {
            _stream.EndWrite(async);
        }
        catch (Exception ex)
        {
            isConnected = false;
            Debug.Log("消息发送失败");
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

    /// <summary>
    /// 注册消息回调事件
    /// </summary>
    public static void Register(MessageType type, CallBack method)
    {
        if (!_callBacks.ContainsKey(type))
            _callBacks.Add(type, method);
        else
            Debug.LogWarning("注册了相同的回调事件");
    }

    /// <summary>
    /// 加入消息队列
    /// </summary>
    public static void Enqueue(MessageType type, byte[] data = null)
    {
        //把数据进行封装
        byte[] bytes = _Pack(type, data);

        if (isConnected)
        {
            //加入队列                                 
            _messages.Enqueue(bytes);
        }
    }
    public static void Disconnected()
    {
        _client.Close();
        isConnected = false;
    }
    #endregion
    #region 请求调用
    public static void UploadTankStatus(PlayerTankRequest ptr)
    {
        byte[] data = NetworkUtils.Serialize(ptr);
        Enqueue(MessageType.PlayerTankUpdateRequest, data);
    }
    public static void ChangeNickNameRequest(string nickname)
    {
        ChangeNickName request = new ChangeNickName();
        request.NickName = nickname;
        byte[] data = NetworkUtils.Serialize(request);
        Enqueue(MessageType.ChangeNickNameRequest, data);
    }

    public static void CreateRoomRequest(string roomId)
    {
        CreateRoom request = new CreateRoom();
        request.RoomId = roomId;
        byte[] data = NetworkUtils.Serialize(request);
        Enqueue(MessageType.CreateRoomRequest, data);
    }

    public static void EnterRoomRequest(string roomId)
    {
        EnterRoom request = new EnterRoom();
        request.RoomId = roomId;
        byte[] data = NetworkUtils.Serialize(request);
        Enqueue(MessageType.EnterRoomRequest, data);
    }

    public static void ExitRoomRequest(string roomId)
    {
        ExitRoom request = new ExitRoom();
        request.RoomId = roomId;
        byte[] data = NetworkUtils.Serialize(request);
        Enqueue(MessageType.ExitRoomRequest, data);
    }

    public static void StartGameRequest(string roomId)
    {
        StartGame request = new StartGame();
        request.RoomId = roomId;
        byte[] data = NetworkUtils.Serialize(request);
        Enqueue(MessageType.StartGameRequest, data);
    }
    #endregion

    #region 回调函数
    private static void _PlayerTankUpdateRequest(byte[] data)
    {
           PlayerTankRequest result = NetworkUtils.Deserialize<PlayerTankRequest>(data);
         if(result.Suc)
          {
             Debug.Log("收到来自服务器发送的tank位置更新包");
             LocalPlayerController.UpdateRemotePlayerStatus(result);
           }
     }
    private static void _HeartBeat(byte[] data)
    {
        Received = true;
        Debug.Log("收到心跳包");
    }

    private static void _ChangeNickName(byte[] data)
    {
        ChangeNickName result = NetworkUtils.Deserialize<ChangeNickName>(data);
        if (result.Suc)
        {
            NetworkPlayer.Instance.OnNickNameChange(result.NickName);
            Debug.Log("修改昵称成功");
        }
        else
        {
            Debug.Log("修改昵称失败");
        }
    }

    private static void _CreateRoom(byte[] data)
    {
        CreateRoom result = NetworkUtils.Deserialize<CreateRoom>(data);
        if (result.Suc)
        {
            NetworkPlayer.Instance.OnRoomIdChange(result.RoomId);
            NetworkPlayer.Instance.OnGUIDChange(result.GUID);
            Debug.Log("创建房间成功");
        }
        else
        {
            NetworkPlayer.Instance.RoomId = "-1";
            Debug.Log("创建房间失败");
        }
    }

    private static void _EnterRoom(byte[] data)
    {
        EnterRoom result = NetworkUtils.Deserialize<EnterRoom>(data);
        if(result.Suc)
        {
            NetworkPlayer.Instance.OnRoomIdChange(result.RoomId);
            NetworkPlayer.Instance.OnGUIDChange(result.GUID);
        }
        else
        {
            NetworkPlayer.Instance.RoomId = "-2";
            Debug.Log("加入房间失败");
        }
    }

    private static void _ExitRoom(byte[] data)
    {
        ExitRoom result = NetworkUtils.Deserialize<ExitRoom>(data);
        if(result.Suc)
        {
            NetworkPlayer.Instance.OnPlayingChange(false);
            NetworkPlayer.Instance.OnRoomIdChange("0");
            NetworkPlayer.Instance.OnGUIDChange(0);
            Debug.Log("退出房间成功");
        }
        else
        {
            Debug.Log("退出房间失败");
        }
    }

    private static void _StartGame(byte[] data)
    {
        StartGame result = NetworkUtils.Deserialize<StartGame>(data);
        if(result.Suc)
        {
            NetworkPlayer.Instance.OnPlayingChange(true);
            Debug.Log("开始游戏成功");
        }
        else
        {
            Debug.Log("开始游戏失败");
        }
    }
    #endregion

}
