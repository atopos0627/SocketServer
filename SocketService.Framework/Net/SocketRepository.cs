﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using SocketService.Framework.Client.Sockets;

namespace SocketService.Framework.Net
{
    public class SocketRepository
    {
        private readonly Dictionary<Guid, ZipSocket> _connectionList = new Dictionary<Guid, ZipSocket>();
        private readonly Mutex _connectionMutex = new Mutex();

        protected SocketRepository()
        {
        }

        private static SocketRepository _instance = null;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static SocketRepository Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SocketRepository();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Finds the by client id.
        /// </summary>
        /// <param name="clientId">The client id.</param>
        /// <returns></returns>
        public ZipSocket FindByClientId(Guid clientId)
        {
            _connectionMutex.WaitOne();
            try
            {
                if (_connectionList.ContainsKey(clientId))
                {
                    return _connectionList[clientId];
                }
                else
                {
                    return null;
                }

            }
            finally
            {
                _connectionMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Removes the specified client id.
        /// </summary>
        /// <param name="clientId">The client id.</param>
        public void Remove(Guid clientId)
        {
            _connectionMutex.WaitOne();
            try
            {
                _connectionList.Remove(clientId);
            }
            finally
            {
                _connectionMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Adds the socket.
        /// </summary>
        /// <param name="clientId">The client id.</param>
        /// <param name="connection">The connection.</param>
        public void AddSocket(Guid clientId, Socket connection)
        {
            _connectionMutex.WaitOne();
            try
            {
                _connectionList.Add(clientId, new ZipSocket(connection, clientId));
            }
            finally
            {
                _connectionMutex.ReleaseMutex();
            }
        }


        /// <summary>
        /// Gets the connection list.
        /// </summary>
        public List<ZipSocket> ConnectionList
        {
            get
            {
                return _connectionList.Values.ToList();
            }
        }


    }
}