using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using TinySocketServer.Base;

namespace TinySocketServer.TCP
{
  abstract  public class TcpSession<TServer, TSession> : SessionBase<TServer, TSession>
       where TServer : TcpServer<TServer, TSession>, new()
       where TSession : TcpSession<TServer, TSession>, new()
    {
  

        public override bool Closed { get; protected set; } = false;
        public override EndPoint RemoteHost { get; internal set; } = null;
        public override TServer Server { get; internal set; } = default(TServer);




        public override void Close()
        {

            if (!this.Closed)
            {
                this.Closed = true;
                On_Close();

                if (this.Connection != null)
                {
                    this.Connection.Shutdown(SocketShutdown.Both);
                    this.Connection.Dispose();
                    this.Connection = null;
                }

                this.Server.CloseSession(this as TSession);
                this.Server = null;
            }
        }

        public override int Send(byte[] data, int offset, int count)
        {
            if (!Closed)
            {
                return this.Connection.Send(data, offset, count, SocketFlags.None);
            }
            else
            {
                return 0;
            }
        }

        internal override Span<byte> Receive(Span<byte> data)
        {
            return base.Receive(data);
        }
        public override Socket Connection { get; internal set; }



        abstract protected override void On_Close();

        abstract protected override void On_Receive(Span<byte> data);
        


    }
}
