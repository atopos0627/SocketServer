﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using SocketService.Framework.Messaging;
using SocketService.Framework.ServiceHandler;
using SocketService.Framework.Request;
using SocketService.Framework.Command;

namespace SocketService.Handler
{
    [Serializable()]
    [ServiceHandlerType(typeof(CreateRoomVariableRequest))]
    public class CreateRoomVariableRequestHandler : BaseHandler<CreateRoomVariableRequest, Guid>
    {
        public override bool HandleRequest(CreateRoomVariableRequest request, Guid state)
        {
            MSMQQueueWrapper.QueueCommand(
                new CreateRoomVariableCommand(request.Room, request.VariableName, request.Value)
            );

            return true;
        }
    }
}
