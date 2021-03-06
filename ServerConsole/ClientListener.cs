﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using SocketServer;
using System.Threading;
using System.IO;
using System.Net.Sockets;

namespace ServerConsole
{
    class ClientListener
    {
        #region Fields

        private static readonly ILog logger = LogManager.GetLogger("ChannelListener");

        private readonly object clientSync = new object();

        /// <summary>
        /// A List of all clients connected to this server
        /// </summary>
        private readonly List<ClientConnection> clients = new List<ClientConnection>();

        private INetworkListener listener;
        private Thread listenerThread;
        private volatile bool running;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Creates a new ChannelListener on the given port with a default TCPTransport and no message dispatcher
        /// </summary>
        /// <param name="port">The port number to listen for incoming connections</param>
        public ClientListener(int port)
        {
            Initialize(port, new SocketListener(), new PlainEnvelope(), new MessageDispatcher());
        }

        /// <summary>
        /// Creates a new ChannelListener on the given port, using a default TCP listener and MessageDispatcher
        /// </summary>
        /// <param name="port">The port to listen on</param>
        /// <param name="dispatcher">A message dispatcher to handle incoming messages</param>
        public ClientListener(int port, MessageDispatcher dispatcher)
        {
            Initialize(port, new SocketListener(), new PlainEnvelope(), dispatcher);
        }

        /// <summary>
        /// Creates a new ChannelListener on the given port, using the provided TransportListener and MessageDispatcher
        /// </summary>
        /// <param name="port">The port to listen on</param>
        /// <param name="listener">A TransportListener to generate connections</param>
        /// <param name="dispatcher">A message dispatcher to handle incoming messages</param>
        public ClientListener(int port, INetworkListener listener, MessageDispatcher dispatcher)
        {
            Initialize(port, listener, new PlainEnvelope(), dispatcher);
        }

        /// <summary>
        /// Creates a new ChannelListener on the given port, using the provided Envelope and MessageDispatcher
        /// </summary>
        /// <param name="port">The port to listen on</param>
        /// <param name="envelope">A envelope to generate connections</param>
        /// <param name="dispatcher">A message dispatcher to handle incoming messages</param>
        public ClientListener(int port, Envelope envelope, MessageDispatcher dispatcher)
        {
            Initialize(port, new SocketListener(), envelope, dispatcher);
        }


        /// <summary>
        /// Creates a new ChannelListener on the given port, using the provided TransportListener and MessageDispatcher. 
        /// This constructor also allows the user to specify a complete ConnectedMessage to convey arbitrary information
        /// to connecting clients.
        /// </summary>
        /// <param name="port">The port to listen on</param>
        /// <param name="listener">A TransportListener to generate connections</param>
        /// <param name="envelope">An envelope to handle messages</param>
        /// <param name="dispatcher">A message dispatcher to handle incoming messages</param>
        /// <param name="protocol"></param>
        public ClientListener(int port, INetworkListener listener, Envelope envelope, MessageDispatcher dispatcher)
        {
            Initialize(port, listener, envelope, dispatcher);
        }

        #endregion Constructors

        #region Events

        /// <summary>
        /// Raised when a Channel has disconnected remotely
        /// </summary>
        public event EventHandler<ChannelEventArgs> ChannelClosed;

        /// <summary>
        /// Raised when a new incoming Channel has established a connection.
        /// </summary>
        public event EventHandler<ChannelEventArgs> ChannelConnected;

        /// <summary>
        /// Raised when a connected Channel has changed its control mode
        /// </summary>
        public event EventHandler<ChannelEventArgs> ChannelControlModeChanged;

        /// <summary>
        /// Event Raised whenever an incoming message is received
        /// </summary>
        public event EventHandler<MessageEventArgs> MessageReceived
        {
            add
            {
                lock (clientSync)
                {
                    foreach (ClientConnection channel in clients)
                        channel.MessageReceived += value;
                }
            }
            remove
            {
                lock (clientSync)
                {
                    foreach (ClientConnection channel in clients)
                        channel.MessageReceived -= value;
                }
            }
        }

        /// <summary>
        /// Event Raised whenever an outgoing message is sent
        /// </summary>
        public event EventHandler<MessageEventArgs> MessageSent
        {
            add
            {
                lock (clientSync)
                {
                    foreach (ClientConnection channel in clients)
                        channel.MessageSent += value;
                }
            }
            remove
            {
                lock (clientSync)
                {
                    foreach (ClientConnection channel in clients)
                        channel.MessageSent -= value;
                }
            }
        }

        #endregion Events

        #region Properties


        public int ClientCount
        {
            get
            {
                lock (clientSync)
                {
                    return clients.Count;
                }
            }
        }

        public MessageDispatcher Dispatcher { get; private set; }

        public Envelope Envelope { get; private set; }

        public int Port { get; private set; }

        public bool Running
        {
            get { return running; }
        }

        #endregion Properties

        #region Methods

        #region Public Methods

        /// <summary>
        /// Broadcasts a message to all connected clients in Controller mode, ignoring all responses
        /// </summary>
        /// <param name="message">The IMessage to send to all clients</param>
        public void Broadcast(IMessage message)
        {
            if (message == null) throw new ArgumentNullException("message", "message is null.");

            lock (clientSync)
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    ClientConnection serverChannel = clients[i];
                    try
                    {
                        serverChannel.Send(message);
                    }
                    catch (ProtocolException ex)
                    {
                        logger.Error(string.Format("Broadcast message {0} received error: {1}", message.Name, ex.Message));
                    }
                    catch (IOException ioex)
                    {
                        logger.Error(string.Format("Broadcast message {0} received IO error: {1}", message.Name,
                                                   ioex.Message));
                        clients.RemoveAt(i);
                    }
                    catch (InvalidOperationException ioex)
                    {
                        logger.Error(string.Format(
                            "Broadcast message {0} received InvalidOperationException error: {1}", message.Name,
                            ioex.Message));
                        clients.RemoveAt(i);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(string.Format("Broadcast message {0} received Exception: {1}", message.Name,
                                                   ex.Message));
                        clients.RemoveAt(i);
                    }
                }
            }
        }

        public void CloseAll()
        {
            //  WTJ - 6-22-2011:  
            //  Fixed a thread locking issue that causes the app to take too long to shutdown
            //  when there are connections on the channel.
            //
            //  When Channel.Disconnect is called, it'll eventually raise an event which also tries
            //  to lock on the clientSync object. 
            //
            //  The fix is to only lock on this object when absolutely necessary.
            //  
            //  This func processes a copy of the clients list because items are removed from 
            //  the client collection when the ChannelClosed event is triggered.
            //  The ChannelClosed event also has code which locks on the clientSync object, causing a thread lock condition

            IEnumerable<ClientConnection> clientsCopy;
            lock (clientSync)
            {
                clientsCopy = clients.ToArray();
            }
            //disconnect all client channels
            foreach (ClientConnection channel in clientsCopy)
            {
                try
                {
                    channel.Disconnect();
                }
                catch
                {
                    //Channel may already be closed
                    logger.DebugFormat("CloseAll: Attempted to close channel {0}, but it was already closed.",
                                       channel.ConnectionID);
                }
            }

            //clear out the clients collection out just in case..
            lock (clientSync)
            {
                clients.Clear();
            }
        }

        /// <summary>
        /// Sends a message to all connected clients in Controller mode, returning a collection of all responses
        /// </summary>
        /// <param name="message">The IMessage to send to all clients</param>
        public List<T> SendToAll<T>(IMessage message)
            where T : IMessage
        {
            lock (clientSync)
            {
                var responseList = new List<T>();
                for (int i = 0; i < clients.Count; i++)
                {
                    ClientConnection serverChannel = clients[i];
                    try
                    {
                        var response = serverChannel.Send<T>(message);
                        responseList.Add(response);
                    }
                    catch (ProtocolException ex)
                    {
                        // How do we tell the caller that one of the clients returned an error???
                        logger.Error(string.Format("Broadcast message {0} received error: {1}", message.Name, ex.Message));
                    }
                    catch (IOException ioex)
                    {
                        logger.Error(string.Format("Broadcast message {0} received IO error: {1}",
                                                   message.Name, ioex.Message));
                        clients.RemoveAt(i);
                    }
                }
                return responseList;
            }
        }

        public void Start()
        {
            listener.Start();
            logger.Info("ChannelListener accepting connections on port " + Port);

            running = true;
            listenerThread = new Thread(Run)
                                 {IsBackground = false, Name = "ChannelListener", Priority = ThreadPriority.AboveNormal};
            listenerThread.Start();
        }

        public void Stop()
        {
            running = false;

            listener.Stop();
            if (listenerThread == null) return;
            bool joined = listenerThread.Join(10000);
            if (!joined && listenerThread.ThreadState != ThreadState.Stopped)
            {
                logger.Warn("Failed to shut down ChannelListener.");
            }
        }

        #endregion

        private void Initialize(int listenPort, INetworkListener transportListener, Envelope messageEnvelope,
                                MessageDispatcher messageDispatcher)
        {
            Port = listenPort;
            listener = transportListener;
            Envelope = messageEnvelope;
            Dispatcher = messageDispatcher;
            listener.Initialize(Port);
        }

        private void OnChannelClosed(object sender, ChannelEventArgs e)
        {
            lock (clientSync)
            {
                // remove channel from our internal list
                for (int i = 0; i < clients.Count; i++)
                {
                    if (clients[i].ConnectionID == e.ClientConnection.ConnectionID)
                    {
                        clients.RemoveAt(i);
                        break;
                    }
                }

                EventHandler<ChannelEventArgs> handler = ChannelClosed;
                if (handler != null)
                    handler(this, e);
            }
        }

        private void OnChannelConnected(object sender, ChannelEventArgs e)
        {
            EventHandler<ChannelEventArgs> handler = ChannelConnected;
            if (handler != null)
                handler(sender, e);
        }

        private void OnChannelControlModeChanged(object sender, ChannelEventArgs e)
        {
            EventHandler<ChannelEventArgs> handler = ChannelControlModeChanged;
            if (handler != null)
                handler(sender, e);
        }

        private void Run()
        {
            while (running)
            {
                try
                {
                    INetworkTransport transport = listener.AcceptClient();
                    logger.Info("New connection from : " + transport.RemoteEndPoint);

                    ClientConnection channel = ClientConnection.CreateListenerChannel(transport, Envelope, Dispatcher);
                    lock (clientSync)
                    {
                        clients.Add(channel);
                    }
                    OnChannelConnected(this, new ChannelEventArgs(channel));
                    channel.ChannelClosed += OnChannelClosed;
                }
                catch (SocketException)
                {
                    // This is here because AcceptTcpClient throws an exception when we tell it
                    // stop listening.
                    logger.Debug("ChannelListener shutdown.  No longer accepting connections");
                }
                catch (IOException ex)
                {
                    logger.Warn(ex.Message, ex);
                    if (ex.InnerException != null)
                        logger.Warn("Inner Exception: " + ex.InnerException.Message, ex.InnerException);
                }
                catch (Exception ex)
                {
                    // Catch everything to make sure server remains running
                    logger.Fatal("Exception catchall: " + ex.Message, ex);
                    if (ex.InnerException != null)
                        logger.Fatal("Inner Exception: " + ex.InnerException.Message, ex.InnerException);
                    running = false;
                    listener.Stop();
                }
            }
        }

        #endregion Methods
    }
}
