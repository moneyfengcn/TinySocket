using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace TinySocketServer.Base
{
    abstract public class ServerBase<TServer, TSession> : IServer<TSession>
            where TServer : ServerBase<TServer, TSession>, new()
            where TSession : SessionBase<TServer, TSession>, new()
    {
        virtual public int SessionCount { get; }
        virtual public bool IsRunning { get; }


        #region 回调事件
        public delegate void SessionEventHandle(TSession session, Span<byte> data);

        //新会话事件
        public event SessionEventHandle OnNewSessionEvent;
        //会话被清除事件
        public event SessionEventHandle OnSessionRemoveEvent;
        //会话数据到达事件
        public event SessionEventHandle OnSessionDataArrivalsEvent;

        protected void RaiseEvent_OnNewSessionEvent(TSession session, Span<byte> data)
        {
            OnNewSessionEvent?.Invoke(session, data);
        }

        protected void RaiseEvent_OnSessionRemoveEvent(TSession session, Span<byte> data)
        {
            OnSessionRemoveEvent?.Invoke(session, data);
        }

        protected void RaiseEvent_OnSessionDataArrivalsEvent(TSession session, Span<byte> data)
        {
            OnSessionDataArrivalsEvent?.Invoke(session, data);
        }


        #endregion

        #region IServer接口
        abstract public void Start(IPEndPoint ep, ServerOption option);
        abstract public void Close();
        abstract public List<TSession> GetAllSessions();
        abstract public void CloseSession(TSession session);
        abstract public TSession FindSession(Func<TSession, bool> func);


        #endregion


        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。
                Close();
                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~ServerBase() {
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
