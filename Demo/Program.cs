using Demo.TcpDemo;
using Demo.UdpDemo;
using System;
using System.Net;

namespace Demo
{




    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello TinySocketServer!");

            #region 继承UDP服务器的实现

            var udp_server = new UdpServer();
            //UDP本地侦听端口
            var localPoint = new IPEndPoint(IPAddress.Any, 9090);
            //启动UDP服务器
            udp_server.Start(localPoint);

            #endregion

            #region 继承Tcp服务器的实现

            var tcp_server = new TcpServer();
            //启用TCP服务器，侦听地下和端口值 和上面的UDP一样
            tcp_server.Start(localPoint);
            #endregion
            while (true)
            {
                if (Console.ReadLine().ToUpper() == "EXIT") break;
            }
        }


    }
}
