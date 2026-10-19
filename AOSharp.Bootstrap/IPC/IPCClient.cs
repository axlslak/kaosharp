using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Collections.Concurrent;

namespace AOSharp.Bootstrap.IPC
{
    public class IPCClient
    {
        public Action<IPCClient> OnConnected = null;
        public Action<IPCClient> OnDisconnected = null;
        private Dictionary<byte, IPCCallback> _callbacks = new Dictionary<byte, IPCCallback>();
        private IDictionary<int, IPCAsyncResult> _pendingTransactions = new ConcurrentDictionary<int, IPCAsyncResult>();
        private NamedPipeClientStream _client;
        private byte[] _buffer = new byte[ushort.MaxValue];
        private readonly object _sendLock = new object();
        private int _disconnected;

        private void NotifyDisconnected()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) == 0)
            {
                try { _client.Close(); } catch { }
                OnDisconnected?.Invoke(this);
            }
        }
        private int _nextTransactionId = 0;

        public IPCClient(string name)
        {
            _client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
        }

        public void RegisterCallback(byte opCode, Type type, Action<object, IPCMessage> callback = null)
        {
            if (!_callbacks.ContainsKey(opCode))
                _callbacks.Add(opCode, new IPCCallback() { OpCode = opCode, Type = type, Callback = callback });
        }

        public void DeregisterCallback(byte opCode)
        {
            if (_callbacks.ContainsKey(opCode))
                _callbacks.Remove(opCode);
        }

        public void Connect(int timeout = 10000)
        {
            _client.Connect(timeout);
            // Preserve boundaries when several plugin status messages arrive together.
            _client.ReadMode = PipeTransmissionMode.Message;

            if (OnConnected != null)
                OnConnected(this);

            BeginRead();
        }

        public void Disconnect()
        {
            _client.Close();
            _client.Dispose();
        }

        private void BeginRead()
        {
            try
            {
                _client.BeginRead(_buffer, 0, _buffer.Length, ReadCallback, null);
            }
            catch (Exception)
            {
                NotifyDisconnected();
            }
        }

        public void ReadCallback(IAsyncResult result)
        {
            try
            {
                int bytesRead = _client.EndRead(result);
                if (bytesRead == 0 || !_client.IsMessageComplete)
                    throw new IOException("Incomplete or oversized IPC message.");
                if (_callbacks.TryGetValue(_buffer[0], out IPCCallback callback))
                {
                    IPCMessage message = (IPCMessage)Activator.CreateInstance(callback.Type);
                    message.Deserialize(_buffer, bytesRead);
                    if (message is IPCResponseMessage response && _pendingTransactions.TryGetValue(response.TransactionId, out IPCAsyncResult pending))
                    {
                        pending.Response = response;
                        pending.Set();
                        _pendingTransactions.Remove(response.TransactionId);
                    }
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
                using (BinaryWriter writer = new BinaryWriter(_client, Encoding.Default, true))
                {
                    writer.Write(data);
                    writer.Flush();
                }
            }
        }

        public IPCAsyncResult Send(IPCRequestMessage message)
        {
            //Set the transaction id for this request.
            message.TransactionId = _nextTransactionId;

            //Setup a wait event for the response.
            IPCAsyncResult result = new IPCAsyncResult();
            _pendingTransactions.Add(_nextTransactionId, result);

            //Finally send the message to the dll.
            Send((IPCMessage)message);

            _nextTransactionId++;

            return result;
        }
    }
}
