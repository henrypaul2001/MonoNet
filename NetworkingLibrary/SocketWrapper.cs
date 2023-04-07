using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    internal class SocketWrapper : ISocket
    {
        private Socket socket;

        internal SocketWrapper(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            socket = new Socket(addressFamily, socketType, protocolType);
        }

        public Socket Socket
        { 
            get { return socket; } 
        }

        public virtual IAsyncResult BeginReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP, AsyncCallback callback, object state)
        {
            try
            {
                return Socket.BeginReceiveFrom(buffer, offset, size, socketFlags, ref remoteEP, callback, state);
            }
            catch (Exception e) 
            {
                Debug.WriteLine(e);
                return null;
            }
        }

        public virtual IAsyncResult BeginSendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP, AsyncCallback callback, object state)
        {
            return Socket.BeginSendTo(buffer, offset, size, socketFlags, remoteEP, callback, state);
        }

        public virtual void Bind(EndPoint localEp)
        {
            Socket.Bind(localEp);
        }

        public virtual void Close()
        {
            Socket.Close();
        }

        public virtual int EndReceiveFrom(IAsyncResult asyncResult, ref EndPoint endPoint)
        {
            try
            {
                return Socket.EndReceiveFrom(asyncResult, ref endPoint);
            } catch (Exception e) 
            {
                Debug.WriteLine(e);
                return -1;
            }
        }

        public virtual int EndSend(IAsyncResult asyncResult)
        {
            return Socket.EndSend(asyncResult);
        }

        public virtual int Receive(byte[] buffer)
        {
            return Socket.Receive(buffer);
        }

        public virtual int Send(byte[] buffer)
        {
            return Socket.Send(buffer);
        }

        public virtual void Shutdown(SocketShutdown how)
        {
            Socket.Shutdown(how);
        }
    }
}
