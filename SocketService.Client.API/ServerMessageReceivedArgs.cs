﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocketService.Client.API
{
    public class ServerMessageReceivedArgs : EventArgs
    {
        public string Message
        {
            get;
            set;
        }
    
    }
}