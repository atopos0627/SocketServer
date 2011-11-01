﻿using System;
using System.Linq;
using SocketService.Core.Messaging;
using SocketService.Event;
using SocketService.Actions;
using SocketService.Repository;
using SocketService.Shared;

namespace SocketService.Command
{
    [Serializable]
    internal class CreateRoomVariableCommand : BaseMessageHandler
    {
        private readonly Guid _clientId;
        private readonly string _name;
        private readonly long _roomId;
        private readonly object _so;
        private readonly int _zoneId;

        public CreateRoomVariableCommand(Guid clientId, int zoneId, long roomId, string name, object so)
        {
            _clientId = clientId;
            _zoneId = zoneId;
            _roomId = roomId;
            _name = name;
            _so = so;
        }

        public override void Execute()
        {
            var room = RoomRepository.Instance.Find(_roomId);
            if (room == null) return;

            RoomActionEngine.Instance.CreateRoomVariable(room, _name, ObjectSerialize.Serialize(_so));

            MSMQQueueWrapper.QueueCommand(
                new BroadcastObjectCommand(
                    room.Users.Select( u => u.ClientKey ).ToArray(),
                    new RoomVariableUpdateEvent() { Action = RoomVariableUpdateAction.Add, Name = _name, RoomId = _roomId, Value = _so, ZoneId = _zoneId }
                    )
                );

        }
    }
}