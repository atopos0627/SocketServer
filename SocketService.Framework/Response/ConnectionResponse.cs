﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocketService.Framework.Response
{
    public class ConnectionResponse : EventArgs
    {
        public bool Success
        {
            get;
            set;
        }
    }
}
