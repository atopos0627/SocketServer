﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SocketService.Framework.Data.Domain;
using SocketService.Framework.Util;

namespace SocketService.Framework.Data
{
    public class RoomRepository : SingletonBase<RoomRepository>
    {
        private object _listLock = new object();
        private List<Room> _roomList = new List<Room>();

        /// <summary>
        /// Adds the room.
        /// </summary>
        /// <param name="room">The room.</param>
        public void AddRoom(Room room)
        {
            lock (_listLock)
            {
                _roomList.Add(room);
            }
        }

        /// <summary>
        /// Finds the name of the by.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public Room FindByName(string name)
        {
            lock (_listLock)
            {
                var query = from room in _roomList
                            where room.Name == name
                            select room;

                return query.FirstOrDefault();
            }
        }
    }
}
