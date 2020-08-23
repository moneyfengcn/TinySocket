using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.UdpDemo
{
    class UdpServer : TinySocketServer.UDP.UDPServer<UdpServer, udpSession> { }
}
