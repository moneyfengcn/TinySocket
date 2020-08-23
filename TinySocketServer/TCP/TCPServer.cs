using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TinySocketServer.Base;
using System.Threading;

namespace TinySocketServer.TCP
{
    abstract public class TcpServer<TServer, TSession>
             : ServerBase<TServer, TSession>
           where TServer : TcpServer<TServer, TSession>, new()
           where TSession : TcpSession<TServer, TSession>, new()
    {

        private ServerOption m_serverOption;

        private byte[] m_Buffer = null;
        private Stack<SocketAsyncEventArgs> m_eventPool = null;
        private List<TSession> m_Sessions = null;
        private bool m_blnRunning = false;
        private Semaphore m_Semaphore = null;
        private Socket m_sckServer = null;
        private Timer m_timerHeartbeat = null;



        public override void Close()
        {
            if (m_blnRunning)
            {
                m_blnRunning = false;

                if (m_sckServer != null)
                {
                    m_sckServer.Shutdown(SocketShutdown.Both);
                    m_sckServer.Close();
                    m_sckServer.Dispose();
                    m_sckServer = null;
                }

                if (m_timerHeartbeat != null)
                {
                    m_timerHeartbeat.Dispose();
                    m_timerHeartbeat = null;
                }

                if (m_Sessions != null)
                {
                    foreach (var item in m_Sessions)
                    {
                        item.Dispose();
                    }
                    m_Sessions.Clear();
                    m_Sessions = null;
                }

                if (m_eventPool != null)
                {
                    var lstPool = m_eventPool.ToList();

                    foreach (var item in lstPool)
                    {
                        item.Dispose();
                    }
                    m_eventPool.Clear();
                    m_eventPool = null;
                }

                if (m_Semaphore != null)
                {
                    m_Semaphore.Dispose();
                    m_Semaphore = null;
                }

                m_Buffer = null;
            }
        }

        public override void CloseSession(TSession session)
        {
            bool blnDel = false;
            lock (m_Sessions)
            {
                blnDel = m_Sessions.Remove(session);
            }

            if (blnDel)
            {
                RaiseEvent_OnSessionRemoveEvent(session, null);
            }
            if (!session.Closed)
            {
                session.Close();
            }
        }



        public override List<TSession> GetAllSessions()
        {
            List<TSession> lstSession = new List<TSession>();
            if (m_blnRunning)
            {
                lock (m_Sessions)
                {
                    lstSession = m_Sessions.ToList();
                }
            }
            return lstSession;
        }
        public override TSession FindSession(Func<TSession, bool> func)
        {
            TSession session = null;

            if (m_blnRunning)
            {
                lock (m_Sessions)
                {
                    foreach (var item in m_Sessions)
                    {
                        if (func(item))
                        {
                            session = item;
                            break;
                        }
                    }
                }
            }
            return session;
        }


        public void Start(IPEndPoint ep)
        {
            Start(ep, new ServerOption()
            {
                MaxConnection = 10000,
                SessionRecviceBuffer = 8192,
                SessionTimeOut = TimeSpan.FromSeconds(1 * 60),
                AcceptObject = 10
            });
        }

        public override void Start(IPEndPoint ep, ServerOption option)
        {
            try
            {

                m_serverOption = option;


                if (m_blnRunning) return;
                m_blnRunning = true;

                m_Sessions = new List<TSession>();

                m_sckServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_sckServer.Bind(ep);
                m_sckServer.Listen(10);

                m_Semaphore = new Semaphore(option.MaxConnection, option.MaxConnection);
                m_Buffer = new byte[option.MaxConnection * option.SessionRecviceBuffer];
                m_eventPool = new Stack<SocketAsyncEventArgs>(option.MaxConnection);

                var handler = new EventHandler<SocketAsyncEventArgs>(On_Completed);

                for (int i = 0; i < option.MaxConnection; i++)
                {
                    SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                    e.SetBuffer(m_Buffer, i * option.SessionRecviceBuffer, option.SessionRecviceBuffer);
                    e.Completed += handler;
                    m_eventPool.Push(e);
                }

                for (int i = 0; i < option.AcceptObject; i++)
                {
                    SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                    e.Completed += handler;
                    StartAccept(e);
                }

                m_timerHeartbeat = new Timer(new TimerCallback(OnTimerCallback), null, option.SessionTimeOut, option.SessionTimeOut);

            }
            catch (Exception ex)
            {
                Close();

                throw ex;
            }
        }

        /// <summary>
        /// 心跳检测
        /// </summary>
        /// <param name="obj"></param>
        private void OnTimerCallback(object obj)
        {
            try
            {
                if (m_blnRunning)
                {
                    var temp = new List<TSession>();
                    lock (m_Sessions)
                    {
                        foreach (var item in m_Sessions)
                        {
                            if (item.Heartbeat == 0)
                            {
                                temp.Add(item);
                            }
                            else
                            {
                                item.Heartbeat = 0;
                            }
                        }
                    }
                    foreach (var item in temp)
                    {
                        item.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void StartAccept(SocketAsyncEventArgs e)
        {
            try
            {
                if (m_blnRunning)
                {
                    e.AcceptSocket = null;
                    if (!m_sckServer.AcceptAsync(e))
                    {
                        On_Completed(null, e);
                    }
                }
                else
                {
                    e.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void StartReceive(TSession s, SocketAsyncEventArgs e)
        {
            try
            {
                if (m_blnRunning)
                {
                    e.SetBuffer(e.Offset, m_serverOption.SessionRecviceBuffer);
                    e.UserToken = s;
                    if (!s.Connection.ReceiveAsync(e))
                    {
                        On_Completed(s.Connection, e);
                    }
                }
                else
                {
                    e.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                throw ex;  //抛回上层
            }
        }

        private void On_Completed(object sender, SocketAsyncEventArgs e)
        {

            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    On_Accept(e);
                    break;

                case SocketAsyncOperation.Receive:
                    On_Receive(e);
                    break;

                default:
                    throw new NotImplementedException("未知操作");
            }

        }

        private void On_Receive(SocketAsyncEventArgs e)
        {
            bool flag = false;
            try
            {
                TSession session = e.UserToken as TSession;
                if (!session.Closed && m_blnRunning)
                {
                    if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                    {
                        session.Heartbeat = 1;

                        Span<byte> data = e.Buffer;

                        var tmp = session.Receive(data.Slice(e.Offset, e.BytesTransferred));
                        if (tmp != null)
                        {
                            RaiseEvent_OnSessionDataArrivalsEvent(session, tmp);
                        }
                        StartReceive(session, e);
                        flag = true;
                    }
                    else
                    {
                        session.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                if (!flag) Push_SocketAsyncEventArgs(e);
            }
        }

        private void On_Accept(SocketAsyncEventArgs e)
        {
            try
            {
                var s = e.AcceptSocket;
                if (e.SocketError == SocketError.Success && m_blnRunning)
                {
                    TSession session = new TSession
                    {
                        Connection = s,
                        Server = this as TServer,
                        RemoteHost = s.RemoteEndPoint
                    };


                    lock (m_Sessions)
                    {
                        m_Sessions.Add(session);
                    }
                    RaiseEvent_OnNewSessionEvent(session, null);

                    SocketAsyncEventArgs sae = null;
                    try
                    {
                        sae = Pop_SocketAsyncEventArgs();
                        StartReceive(session, sae);
                    }
                    catch
                    {
                        if (sae != null) Push_SocketAsyncEventArgs(sae);
                        if (session != null)
                        {
                            session.Close();
                        }
                    }

                }
                else
                {
                    if (s != null)
                    {
                        s.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                StartAccept(e);
            }
        }

        private SocketAsyncEventArgs Pop_SocketAsyncEventArgs()
        {
            SocketAsyncEventArgs e = null;
            m_Semaphore.WaitOne();
            lock (m_eventPool)
            {
                e = m_eventPool.Pop();
            }
            return e;
        }
        private void Push_SocketAsyncEventArgs(SocketAsyncEventArgs e)
        {
            e.UserToken = null;
            lock (m_eventPool)
            {
                m_eventPool.Push(e);
            }
            m_Semaphore.Release();
        }


        /// <summary>
        /// 在线连接数
        /// </summary>
        public override int SessionCount
        {
            get
            {
                int count = 0;
                if (m_Sessions != null)
                {
                    count = m_Sessions.Count;
                }
                return count;
            }
        }

        public override bool IsRunning => m_blnRunning;
    }
}
