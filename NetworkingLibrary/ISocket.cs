using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    internal interface ISocket
    {
        void Close();
        void Shutdown(SocketShutdown how);
        int Send(byte[] buffer);
        int Receive(byte[] buffer);
        void Bind(EndPoint localEp);
        IAsyncResult BeginReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP, AsyncCallback callback, object state);
        int EndReceiveFrom(IAsyncResult asyncResult, ref EndPoint endPoint);
        IAsyncResult BeginSendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP, AsyncCallback callback, object state);
        int EndSend(IAsyncResult asyncResult);

        IAsyncResult AsyncState { get; }
    }
}
