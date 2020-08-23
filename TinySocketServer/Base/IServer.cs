using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace TinySocketServer.Base
{
    public interface IServer<TSession> : IDisposable
    {
        void Start(IPEndPoint ep, ServerOption option);
        void Close();
        List<TSession> GetAllSessions();
        TSession FindSession(Func<TSession, bool> func);
        void CloseSession(TSession session);

    }
}
