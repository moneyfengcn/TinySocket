using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.TcpDemo
{
    class TcpSession : TinySocketServer.TCP.TcpSession<TcpServer, TcpSession>
    {
        #region 在这里处理你的业务
        protected override void On_Close()
        {
            Console.WriteLine("TCP会话关闭" + this.RemoteHost);
        }

        protected override void On_Receive(Span<byte> data)
        {
            var tmp = data.ToArray();
            Console.WriteLine("收到TCP会话的数据:{0}\r\n{1}", this.RemoteHost, BitConverter.ToString(tmp));


            //将数据发回去
            tmp[0] = (byte)'A';
            Send(tmp, 0, tmp.Length);
        }
        #endregion
    }

}
