﻿using System;
using System.Security;
using Engine.Api.Client.Rooms;
using Engine.Api.Server.Messages;
using Engine.Model.Common.Entities;
using Engine.Model.Server;
using ThirtyNineEighty.BinarySerializer;

namespace Engine.Api.Server.Rooms
{
  [SecurityCritical]
  class ServerRefreshRoomCommand :
    ServerCommand<ServerRefreshRoomCommand.MessageContent>
  {
    public const long CommandId = (long)ServerCommandId.RefreshRoom;

    public override long Id
    {
      [SecuritySafeCritical]
      get { return CommandId; }
    }

    [SecuritySafeCritical]
    protected override void OnRun(MessageContent content, CommandArgs args)
    {
      if (string.IsNullOrEmpty(content.RoomName))
        throw new ArgumentException("content.RoomName");

      using (var server = ServerModel.Get())
      {
        Room room;
        if (!TryGetRoom(server.Chat, content.RoomName, args.ConnectionId, out room))
          return;

        if (!room.IsUserExist(args.ConnectionId))
        {
          ServerModel.Api.Perform(new ServerSendSystemMessageAction(args.ConnectionId, SystemMessageId.RoomAccessDenied));
          return;
        }

        var roomRefreshedContent = new ClientRoomRefreshedCommand.MessageContent
        {
          Room = room.ToDto(args.ConnectionId),
          Users = server.Chat.GetRoomUserDtos(room.Name)
        };

        ServerModel.Server.SendMessage(args.ConnectionId, ClientRoomRefreshedCommand.CommandId, roomRefreshedContent);
      }
    }

    [Serializable]
    [BinType("ServerRefreshRoom")]
    public class MessageContent
    {
      [BinField("r")]
      public string RoomName;
    }
  }
}
