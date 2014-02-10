using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;

namespace K2Informatics.Erlnet
{
    public class ErlStream
    {
        private Stream _stream;

        public ErlStream(NetworkStream ns) { _stream = ns; }
        public ErlStream(SslStream ssl) { _stream = ssl; }

        public bool CanRead
        {
            get { return _stream.CanRead; }
        }

        public bool DataAvailable
        {
            get
            {
                if (_stream.GetType() == typeof(SslStream))                
                    return true;
                else
                    return ((NetworkStream)_stream).DataAvailable;
                    
            }
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public void Flush()
        {
            _stream.Flush();
        }

    }
}
