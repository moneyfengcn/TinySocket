# TinySocket
a socket server library

我的本意是简单封装一个基本的TCP、UDP通信服务器框架。
虽然已经有无数类似的SOCKET服务器封装项目，但是我还是想按我的想法写一个。
那么它应该有这样的特点：
* 轻量级。尽量的轻，你只需要将几个类文件复制到你的代码中去就可以用，不需要再涉及一些第三方的库。
* 最小限度的封装。它不涉及到通信协议及其它的任何业务，只是一个单纯的IO框架。
* 容易理解和修改。
* 半成品，它不能在你的代码里直接new出来用。需要你至少做一次最简单的继承，以便让你熟悉代码。

一个庞大的开源代码洋洋洒洒数万行，许多代码不是用户想要的，如果裁剪那将是一个非常辛苦的过程。并且有时候很多开源代码会面临无人维护的结果，出现BUG将会是灾难性的。所以我将这个库定义的特别的简单。

***

# 使用例程：
```
#region 这里实现一个最简单的TCP通信服务器
    //继承TcpServer
    class TcpServer : TinySocketServer.TCP.TcpServer<TcpServer, TcpSession> { }
    
    //继承TcpSession
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

#endregion
```
运行

```
 class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello TinySocketServer!");
 
            //本地侦听端口
            var localPoint = new IPEndPoint(IPAddress.Any, 9090);
            var tcp_server = new TcpServer();
            //启用TCP服务器，侦听地下和端口值 和上面的UDP一样
            tcp_server.Start(localPoint);
         
            while (true)
            {
                if (Console.ReadLine().ToUpper() == "EXIT") break;
            }
        }
    }
```
