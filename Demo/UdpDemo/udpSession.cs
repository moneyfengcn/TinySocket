using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.UdpDemo
{
    class udpSession : TinySocketServer.UDP.UDPSession<UdpServer, udpSession>
    {
        protected override void On_Receive(Span<byte> data)
        {
            Console.WriteLine("接收到Udp数据 {0}\r\n{1}", this.RemoteHost, BitConverter.ToString(data.ToArray()));
            //发回去
            var buf = data.ToArray();
            Send(buf, 0, buf.Length);
        }
        protected override void On_Close()
        {
            Console.WriteLine("Udp会话己超时结束:" + this.RemoteHost);
        }
    }
}
