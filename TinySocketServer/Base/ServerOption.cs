using System;
using System.Collections.Generic;
using System.Text;

namespace TinySocketServer.Base
{
    public struct ServerOption
    {
        /// <summary>
        /// 最大并发连接数
        /// </summary>
        public int MaxConnection { get; set; } 
        /// <summary>
        /// 每个会话接收的缓冲区大小
        /// </summary>
        public int SessionRecviceBuffer { get; set; }  
        /// <summary>
        /// 心跳检查间隔
        /// </summary>
        public TimeSpan SessionTimeOut { get; set; }  
        /// <summary>
        /// Socket接受对象队列 
        /// </summary>
        public int AcceptObject { get; set; }
    }
}
