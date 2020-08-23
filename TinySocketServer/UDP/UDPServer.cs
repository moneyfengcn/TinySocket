using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TinySocketServer.Base;

namespace TinySocketServer.UDP
{
    abstract public class UDPServer<TServer, TSession> : ServerBase<TServer, TSession>
           where TServer : UDPServer<TServer, TSession>, new()
           where TSession : UDPSession<TServer, TSession>, new()
    {

        private Socket m_sckServer = null;
        private ServerOption m_serverOption;
        private bool m_blnRunning;
        private Semaphore m_Semaphore;
        private byte[] m_Buffer;
        private Stack<SocketAsyncEventArgs> m_eventPool;
        private Timer m_timerHeartbeat;
        private Dictionary<string, TSession> m_Sessions = null;

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
                    var sessions = m_Sessions.Values.ToList();
                    foreach (var item in sessions)
                    {
                        CloseSession(item);
                    }
                    m_Sessions.Clear();
                    m_Sessions = null;
                }

                if (m_eventPool != null)
                {
                    foreach (var item in m_eventPool)
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
                blnDel = m_Sessions.Remove(session.RemoteHost.ToString());
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
                    lstSession = m_Sessions.Values.ToList();
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
                    foreach (var item in m_Sessions.Values)
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

                m_Sessions = new Dictionary<string, TSession>();

                m_sckServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                m_sckServer.Bind(ep);


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

                m_blnRunning = true;

                for (int i = 0; i < option.AcceptObject; i++)
                {
                    var e = Pop_SocketAsyncEventArgs();

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

        private void StartAccept(SocketAsyncEventArgs e)
        {
            try
            {
                if (m_blnRunning)
                {
                    e.UserToken = null;
                    e.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    e.SetBuffer(e.Offset, m_serverOption.SessionRecviceBuffer);

                    if (!m_sckServer.ReceiveFromAsync(e))
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

        private void OnTimerCallback(object state)
        {
            try
            {
                if (m_blnRunning)
                {
                    var temp = new List<TSession>();
                    lock (m_Sessions)
                    {
                        foreach (var item in m_Sessions.Values)
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

        private void On_Completed(object sender, SocketAsyncEventArgs e)
        {
            TSession session;

            switch (e.LastOperation)
            {

                case SocketAsyncOperation.ReceiveFrom:
                    lock (m_Sessions)
                    {
                        m_Sessions.TryGetValue(e.RemoteEndPoint.ToString(), out session);
                    }
                    if (session == null)
                    {
                        On_Accept(e);
                    }
                    else
                    {
                        On_Receive(session, e);
                    }
                    break;
                case SocketAsyncOperation.SendTo:
                    break;
                default:
                    break;
            }
        }

        private void On_Accept(SocketAsyncEventArgs e)
        {
            try
            {
                //新收到的会话数据
                var session = new TSession()
                {
                    Connection = this.m_sckServer,
                    Server = this as TServer,
                    RemoteHost = e.RemoteEndPoint
                };

                lock (m_Sessions)
                {
                    m_Sessions.Add(session.RemoteHost.ToString(), session);
                }

                Span<byte> span = m_Buffer;
                span = span.Slice(e.Offset, e.BytesTransferred);

                RaiseEvent_OnNewSessionEvent(session, null);

                var tmp = session.Receive(span);
                if (tmp != null)
                {
                    RaiseEvent_OnSessionDataArrivalsEvent(session, tmp);
                }

                StartReceive(session, e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                StartAccept(e);
            }
        }
        private void On_Receive(TSession session, SocketAsyncEventArgs e)
        {
            bool flag = false;
            try
            {
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

        private void StartReceive(TSession session, SocketAsyncEventArgs e)
        {
            try
            {
                if (m_blnRunning)
                {
                    e.UserToken = session;
                    e.RemoteEndPoint = session.RemoteHost;
                    e.SetBuffer(e.Offset, m_serverOption.SessionRecviceBuffer);

                    if (!m_sckServer.ReceiveFromAsync(e))
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
            e.RemoteEndPoint = null;
            lock (m_eventPool)
            {
                m_eventPool.Push(e);
            }
            m_Semaphore.Release();
        }
    }
}
