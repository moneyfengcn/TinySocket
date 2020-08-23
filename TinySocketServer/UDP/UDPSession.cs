using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TinySocketServer.Base;

namespace TinySocketServer.UDP
{
    abstract public class UDPSession<TServer, TSession> : SessionBase<TServer, TSession>
       where TServer : UDPServer<TServer, TSession>, new()
       where TSession : UDPSession<TServer, TSession>, new()
    {


        public override bool Closed { get; protected set; } = false;
        public override EndPoint RemoteHost { get; internal set; } = null;
        public override TServer Server { get; internal set; } = default(TServer);
        public override Socket Connection { get; internal set; }

        public override void Close()
        {
            if (!this.Closed)
            {
                this.Closed = true;
                On_Close();


                this.Server.CloseSession(this as TSession);
                this.Server = null;
            }
        }

        public override int Send(byte[] data, int offset, int count)
        {
            return this.Connection.SendTo(data, offset, count, SocketFlags.None, this.RemoteHost);
        }

        abstract protected override void On_Close();


        abstract protected override void On_Receive(Span<byte> data);
   
    }
}
