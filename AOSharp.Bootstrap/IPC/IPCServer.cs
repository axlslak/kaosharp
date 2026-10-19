using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace AOSharp.Bootstrap.IPC
{
    public enum MsgType
    {
        Transaction,
        Event
    }

    [Serializable]
    public struct IPCCallback
    {
        public byte OpCode;
        public Type Type;
        public Action<object, IPCMessage> Callback;
    }

    [Serializable]
    public class IPCServer
    {
        public bool IsConnected { get { return _server.IsConnected; } }

        public Action<IPCServer> OnConnected = null;
        public Action<IPCServer> OnDisconnected = null;
        private Dictionary<byte, IPCCallback> _callbacks = new Dictionary<byte, IPCCallback>();
        private NamedPipeServerStream _server;
        private byte[] _buffer = new byte[ushort.MaxValue];
        private readonly object _sendLock = new object();
        private int _disconnected;

        private void NotifyDisconnected()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) == 0)
                OnDisconnected?.Invoke(this);
        }

        public IPCServer(string pipeName)
        {
            _server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, -1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
        }

        public void Close()
        {
            _server.Close();
        }

        public void RegisterCallback(byte opCode, Type type, Action<object, IPCMessage> callback)
        {
            if(!_callbacks.ContainsKey(opCode))
                _callbacks.Add(opCode, new IPCCallback() { OpCode = opCode, Type = type, Callback = callback});
        }

        public void DeregisterCallback(byte opCode)
        {
            if (_callbacks.ContainsKey(opCode))
                _callbacks.Remove(opCode);
        }

        public void Start()
        {
            //Begin waiting for a connection from a client. 
            //Note: We will only allow one connection at a time making it a 1 on 1 conversation.
            _server.BeginWaitForConnection(WaitForConnectionCallback, null);
        }

        private void BeginRead()
        {
            try
            {
                _server.BeginRead(_buffer, 0, _buffer.Length, ReadCallback, null);
            }
            catch (Exception)
            {
                NotifyDisconnected();
            }
        }

        private void WaitForConnectionCallback(IAsyncResult result)
        {
            try { _server.EndWaitForConnection(result); }
            catch (Exception) { NotifyDisconnected(); return; }

            //Notify subscribers that we have a new client connected
            if (OnConnected != null)
                OnConnected(this);

            //Begin reading messages from the client.
            BeginRead();
        }

        private void ReadCallback(IAsyncResult result)
        {
            try
            {
                int bytesRead = _server.EndRead(result);
                if (bytesRead == 0 || !_server.IsMessageComplete)
                    throw new IOException("Incomplete or oversized IPC message.");
                if (_callbacks.TryGetValue(_buffer[0], out IPCCallback callback))
                {
                    IPCMessage message = (IPCMessage)Activator.CreateInstance(callback.Type);
                    message.Deserialize(_buffer, bytesRead);
                    callback.Callback?.Invoke(this, message);
                }
                BeginRead();
            }
            catch (Exception)
            {
                // A malformed message or closed pipe must not escape an async callback.
                NotifyDisconnected();
            }
        }

        public void Send(IPCMessage message)
        {
            byte[] data = message.Serialize();
            if (data.Length > ushort.MaxValue) throw new InvalidDataException("IPC message is too large.");
            lock (_sendLock)
            {
                using (BinaryWriter writer = new BinaryWriter(_server, Encoding.Default, true))
                {
                    writer.Write(data);
                    writer.Flush();
                }
            }
        }

    }
}
