﻿using System;
using SocketServer.Crypto;

namespace SocketServer.Shared.Request
{
    [Serializable]
    public class GetCentralAuthorityRequest
    {
        /// <summary>
        /// Gets or sets the central authority.
        /// </summary>
        /// <value>
        /// The central authority.
        /// </value>
        public CentralAuthority CentralAuthority { get; set; }
    }
}