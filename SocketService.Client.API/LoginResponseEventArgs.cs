﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SocketService.Framework.Client.Response;

namespace SocketService.Client.API
{
    public class LoginResponseEventArgs : EventArgs
    {
        public LoginResponse LoginResponse
        {
            get;
            set;
        }
    }
}