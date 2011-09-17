﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SocketService.Crypto;
using SocketService.Request;
using SocketService.Serial;
using SocketService.Client;
using SocketService.Handler;
using SocketService.Framework.Messaging;

namespace SocketService.Command
{
    [Serializable()]
    class ParseRequestCommand : BaseMessageHandler
    {
        private readonly byte[] _serialized;
        private readonly Guid _clientId;

        public ParseRequestCommand(Guid clientId, byte[] serialized)
        {
            _serialized = serialized;
            _clientId = clientId;
        }

        public override void Execute()
        {
            IRequestHeader header = (IRequestHeader)ObjectSerialize.Deserialize(_serialized);
            object serverRequest = DecryptHeader(header);
        
            //// lookup this object types handler
            Type requestType = serverRequest.GetType();
            var handlerList = ServiceHandlerRepository.Instance.GetHandlerListForType(requestType);

            // here is where we start using MSMQ and the messaging handler
            // to queue up a new command
            MSMQQueueWrapper.QueueCommand(new HandleClientRequestCommand(_clientId, serverRequest, handlerList));
        }

        private object DecryptHeader(IRequestHeader header)
        {
            // switch on encryption type, and create a decryptor for that type
            // with the remote private key and iv as salt
            AlgorithmType algorithm = AlgorithmType.AES;

            switch (header.Encryption)
            {
                case EncryptionType.DES:
                    algorithm = AlgorithmType.DES;
                    break;

                case EncryptionType.TripleDES:
                    algorithm = AlgorithmType.TripleDES;
                    break;

                case EncryptionType.None:
                    algorithm = AlgorithmType.None;
                    break;
            }


            if (algorithm != AlgorithmType.None)
            {
                Connection connection = ConnectionRepository.Instance.FindConnectionByClientId(_clientId);
                if (connection != null)
                {
                    DiffieHellmanKey privateKey = connection.Provider.CreatePrivateKey(connection.RemotePublicKey);
                    using (Wrapper cryptoWrapper = Wrapper.CreateDecryptor(algorithm,
                                                                        privateKey.ToByteArray(),
                                                                        header.EncryptionPublicKey))
                    {
                        return ObjectSerialize.Deserialize(cryptoWrapper.Decrypt(header.RequestData));
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return ObjectSerialize.Deserialize(header.RequestData);
            }


        }
    }
}
