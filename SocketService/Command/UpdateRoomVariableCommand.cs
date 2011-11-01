﻿using System;
using System.Linq;
using SocketService.Core.Data;
using SocketService.Core.Messaging;
using SocketService.Event;
using SocketService.Repository;

namespace SocketService.Command
{
    [Serializable]
    public class UpdateRoomVariableCommand : BaseMessageHandler
    {
        private readonly Guid _clientId;
        private readonly string _name;
        private readonly int _roomId;
        private readonly object _so;
        private readonly int _zoneId;

        public UpdateRoomVariableCommand(Guid clientId, int zoneId, int roomId, string name, object value)
        {
            _zoneId = zoneId;
            _roomId = roomId;
            _name = name;
            _so = value;
            _clientId = clientId;
        }


        public override void Execute()
        {
            Room room = RoomRepository.Instance.Find(_roomId);

            //RoomActionEngine.Instance.UpdateRoomVariable(room.Id, _name, _so);

            MSMQQueueWrapper.QueueCommand(
                new BroadcastObjectCommand(
                    room.Users.Select(u => u == null ? new Guid() : u.ClientKey).ToArray(),
                    new RoomVariableUpdateEvent
                        {
                            ZoneId =  _zoneId,
                            RoomId = room.Id,
                            Name = _name,
                            Value = _so,
                            Action = RoomVariableUpdateAction.Update
                        }
                    )
                );

            //MSMQQueueWrapper.QueueCommand(
            //    new SendObjectCommand(
            //        _clientId,
            //        new UpdateRoomVariableResponse()
            //        {
            //            Room = _roomName,
            //            Name = _name,
            //            Value = _so,
            //        }
            //    )
            //);
        }
    }
}