using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TinySocketServer.Base
{

    abstract public class SessionBase<TServer, TSession> : ISession<TServer>
          where TServer : ServerBase<TServer, TSession>, new()
          where TSession : SessionBase<TServer, TSession>, new()
    {
        internal int Heartbeat { get; set; } = 0;
        /// <summary>
        /// 连接状态
        /// </summary>
        abstract public bool Closed { get; protected set; }
        /// <summary>
        /// 对端地址
        /// </summary>
        abstract public EndPoint RemoteHost { get; internal set; }

        /// <summary>
        /// 隶属服务器
        /// </summary>
        abstract public TServer Server { get; internal set; }
        /// <summary>
        /// 本连接的Socket对象
        /// </summary>
        abstract public Socket Connection { get; internal set; }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        abstract public int Send(byte[] data, int offset, int count);
        /// <summary>
        /// 关闭连接
        /// </summary>
        abstract public void Close();




        internal virtual Span<byte> Receive(Span<byte> data)
        {
            Heartbeat = 1;
            On_Receive(data);
            return data;
        }

        abstract protected void On_Close();
        abstract protected void On_Receive(Span<byte> data);


        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                    Close();
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。
          
                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~SessionBase() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
