using System;
using System.Collections.Generic;
using System.Text;
using Multiplay;

namespace UnityMultiplayerServer
{
    class Network
    {
        /// <summary>
        /// 启动服务器
        /// </summary>
        /// <param name="ip">IPv4地址</param>
        public Network()
        {
            //注册
            Server.Register(MessageType.HeartBeat, _HeartBeat);
            Server.Register(MessageType.ChangeNickNameRequest, _Enroll);
            Server.Register(MessageType.CreateRoomRequest, _CreateRoom);
            Server.Register(MessageType.EnterRoomRequest, _EnterRoom);
            Server.Register(MessageType.ExitRoomRequest, _ExitRoom);
            Server.Register(MessageType.StartGameRequest, _StartGame);
            Server.Register(MessageType.PlayerTankUpdateRequest, _PlayerTankUpdateRequest);
            //启动服务器
            Server.Start();
        }
        private void _PlayerTankUpdateRequest(Player player, byte[] data)
        {
            PlayerTankRequest result = new PlayerTankRequest();
            PlayerTankRequest receive = NetworkUtils.Deserialize<PlayerTankRequest>(data);

            if(Server.Rooms.ContainsKey(receive.RoomId))
            {
                Room room = Server.Rooms[receive.RoomId];

                foreach(Player _player in room.Players)
                {
                    if(_player.GUID != receive.GUID)
                    {
                        result = receive;
                        result.Suc = true;
                        data = NetworkUtils.Serialize(result);
                        _player.Send(MessageType.PlayerTankUpdateRequest, data);
                    }
                }
            }
            else
            {
                // room not exist
                result.Suc = false;
                data = NetworkUtils.Serialize(result);
                player.Send(MessageType.PlayerTankUpdateRequest, data);
            }
        }
        private void _HeartBeat(Player player, byte[] data)
        {
            //仅做回应
            player.Send(MessageType.HeartBeat);
        }

        private void _Enroll(Player player, byte[] data)
        {
            ChangeNickName result = new ChangeNickName();

            ChangeNickName receive = NetworkUtils.Deserialize<ChangeNickName>(data);

            Console.WriteLine($"玩家{player.NickName}改名为{receive.NickName}");
            //设置玩家名字
            player.NickName = receive.NickName;

            //向玩家发送成功操作结果
            result.Suc = true;
            result.NickName  = receive.NickName;
            data = NetworkUtils.Serialize(result);
            player.Send(MessageType.ChangeNickNameRequest, data);
        }

        private void _CreateRoom(Player player, byte[] data)
        {
            //结果
            CreateRoom result = new CreateRoom();

            CreateRoom receive = NetworkUtils.Deserialize<CreateRoom>(data);

            //逻辑检测(玩家不在任何房间中 并且 不存在该房间)
            if (!player.InRoom && !Server.Rooms.ContainsKey(receive.RoomId))
            {
                //新增房间
                Room room = new Room(receive.RoomId);
                Server.Rooms.Add(room.RoomId, room);
                //添加玩家
                room.Players.Add(player);
                player.EnterRoom(receive.RoomId);

                

                //向客户端发送操作结果
                result.Suc = true;
                result.GUID = 1;
                player.GUID = 1;
                result.RoomId = receive.RoomId;
                Console.WriteLine($"玩家:{player.NickName}创建房间成功, GUID为{player.GUID}");
                data = NetworkUtils.Serialize(result);
                player.Send(MessageType.CreateRoomRequest, data);
            }
            else
            {
                Console.WriteLine($"玩家:{player.NickName}创建房间失败");
                //向客户端发送操作结果
                result.Suc = false;
                data = NetworkUtils.Serialize(result);
                player.Send(MessageType.CreateRoomRequest, data);
            }
        }

        private void _EnterRoom(Player player, byte[] data)
        {
            //结果
            EnterRoom result = new EnterRoom();

            EnterRoom receive = NetworkUtils.Deserialize<EnterRoom>(data);

            //逻辑检测(玩家不在任何房间中 并且 存在该房间)
            if (!player.InRoom && Server.Rooms.ContainsKey(receive.RoomId))
            {
                Room room = Server.Rooms[receive.RoomId];
                //加入玩家
                if (room.Players.Count < Room.MAX_PLAYER_AMOUNT && !room.Players.Contains(player))
                {
                    room.Players.Add(player);
                    player.EnterRoom(receive.RoomId);

                    

                    //向玩家发送成功操作结果
                    result.RoomId = receive.RoomId;
                    result.Suc = true;
                    result.GUID = room.Players.Count;
                    player.GUID = result.GUID;
                    Console.WriteLine($"玩家:{player.NickName}成为了房间:{receive.RoomId}的玩家, GUID为{player.GUID}");
                    data = NetworkUtils.Serialize(result);
                    player.Send(MessageType.EnterRoomRequest, data);
                }
                
                else
                {
                    Console.WriteLine($"玩家:{player.NickName}加入房间失败");

                    result.Suc = false;
                    data = NetworkUtils.Serialize(result);
                    player.Send(MessageType.EnterRoomRequest, data);
                }
            }
            else
            {
                Console.WriteLine($"玩家:{player.NickName}进入房间失败");
                //向玩家发送失败操作结果
                data = NetworkUtils.Serialize(result);
                player.Send(MessageType.EnterRoomRequest, data);
            }
        }

        private void _ExitRoom(Player player, byte[] data)
        {
            //结果
            ExitRoom result = new ExitRoom();

            ExitRoom receive = NetworkUtils.Deserialize<ExitRoom>(data);

            //逻辑检测(有该房间)
            if (Server.Rooms.ContainsKey(receive.RoomId))
            {
                //确保有该房间并且玩家在该房间内
                if (Server.Rooms[receive.RoomId].Players.Contains(player))
                {
                    result.Suc = true;
                    //移除该玩家
                    if (Server.Rooms[receive.RoomId].Players.Contains(player))
                    {
                        Server.Rooms[receive.RoomId].Players.Remove(player);
                    }

                    if (Server.Rooms[receive.RoomId].Players.Count == 0)
                    {
                        Server.Rooms.Remove(receive.RoomId); //如果该房间没有玩家则移除该房间
                    }

                    Console.WriteLine($"玩家:{player.NickName}退出房间成功");

                    player.ExitRoom();
                    //向玩家发送成功操作结果
                    data = NetworkUtils.Serialize(result);
                    player.Send(MessageType.ExitRoomRequest, data);
                }
                else
                {
                    Console.WriteLine($"玩家:{player.NickName}退出房间失败");
                    //向玩家发送失败操作结果
                    result.Suc = false;
                    data = NetworkUtils.Serialize(result);
                    player.Send(MessageType.ExitRoomRequest, data);
                }
            }
            else
            {
                Console.WriteLine($"玩家:{player.NickName}退出房间失败");
                result.Suc = false;
                //向玩家发送失败操作结果
                data = NetworkUtils.Serialize(result);
                player.Send(MessageType.ExitRoomRequest, data);
            }
        }

        private void _StartGame(Player player, byte[] data)
        {
            //结果
            StartGame result = new StartGame();

            StartGame receive = NetworkUtils.Deserialize<StartGame>(data);

            //逻辑检测(有该房间)
            if (Server.Rooms.ContainsKey(receive.RoomId))
            {
                //玩家模式开始游戏
                if (Server.Rooms[receive.RoomId].Players.Contains(player) &&
                    Server.Rooms[receive.RoomId].Players.Count <= Room.MAX_PLAYER_AMOUNT)
                {
                    //游戏开始
                    Server.Rooms[receive.RoomId].State = Room.RoomState.Gaming;

                    Console.WriteLine($"玩家:{player.NickName}开始游戏成功");

                    //遍历该房间玩家
                    foreach (var each in Server.Rooms[receive.RoomId].Players)
                    {

                        if (each == player)
                        {
                            result.Suc = true;

                            data = NetworkUtils.Serialize(result);
                            each.Send(MessageType.StartGameRequest, data);
                        }
                        else
                        {
                            result.Suc = true;

                            data = NetworkUtils.Serialize(result);
                            each.Send(MessageType.StartGameRequest, data);
                        }
                    }

                }
                else
                {
                    Console.WriteLine($"玩家:{player.NickName}开始游戏失败");
                    //向玩家发送失败操作结果
                    result.Suc = false;
                    data = NetworkUtils.Serialize(result);
                    player.Send(MessageType.StartGameRequest, data);
                }
            }
            else
            {
                Console.WriteLine($"玩家:{player.NickName}开始游戏失败");
                //向玩家发送失败操作结果
                result.Suc = false;
                data = NetworkUtils.Serialize(result);
                player.Send(MessageType.StartGameRequest, data);
            }
        }

        
    }
}
