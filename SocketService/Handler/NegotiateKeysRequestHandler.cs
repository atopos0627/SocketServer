﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SocketServer.Shared.Request;
using SocketServer.Messaging;
using SocketServer.Command;

namespace SocketServer.Handler
{
    [Serializable]
    //[ServiceHandlerType(typeof(NegotiateKeysRequest))]
    class NegotiateKeysRequestHandler : IRequestHandler<NegotiateKeysRequest>
    {
        public void HandleRequest(NegotiateKeysRequest request, Guid state)
        {
            if (request != null)
            {
                MSMQQueueWrapper.QueueCommand(
                    new NegotiateKeysCommand(state)
                );

                //return true;

            }

            //return false;
        }
    }
}
