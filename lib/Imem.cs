using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace Erlang.NET
{
    public class Imem : Erlnet
    {
        protected NetworkStream stream;
        protected OtpErlangLong seco;

        // instanciate through Connect
        protected Imem(NetworkStream _stream) { stream = _stream; }

        public static Imem Connect(string host, int port)
        {
            TcpClient client = new TcpClient(host, port);
            NetworkStream s = client.GetStream();

            return new Imem(s);
        }

        public void Authenticate(string user, string password)
        {
            OtpErlangObject[] mfaArgs = new OtpErlangObject[4];

            // args for fun
            mfaArgs[0] = new OtpErlangAtom("undefined");
            mfaArgs[1] = new OtpErlangAtom("adminSessionId");
            mfaArgs[2] = new OtpErlangBinary(Encoding.ASCII.GetBytes(user));

            OtpErlangObject[] pswdTuple = new OtpErlangObject[2];
            pswdTuple[0] = new OtpErlangAtom("pwdmd5");
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(password);
            byte[] hash = md5.ComputeHash(inputBytes);
            pswdTuple[1] = new OtpErlangBinary(hash);

            mfaArgs[3] = new OtpErlangTuple(pswdTuple);

            seco = (OtpErlangLong)CallMFASync(stream, "imem_sec", "authenticate", mfaArgs);
        }

        public void Login()
        {
            OtpErlangObject[] mfaArgs = new OtpErlangObject[1];

            mfaArgs[0] = seco;

            if (seco.bigIntegerValue() !=
                ((OtpErlangLong)CallMFASync(stream, "imem_sec", "login", mfaArgs)).bigIntegerValue())
                throw new Exception("imem login failed");
        }        
    }
}
