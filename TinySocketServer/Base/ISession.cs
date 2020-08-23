using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TinySocketServer.Base
{
    public interface ISession<TServer> : IDisposable
    {
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        int Send(byte[] data, int offset, int count);

        /// <summary>
        /// 关闭连接
        /// </summary>
        void Close();
    }
}
