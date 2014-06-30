using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Collections;

namespace K2Informatics.Erlnet
{
    public enum ErlType
    {
        EString = 1
    };

    public class ErlnetException : Exception
    {
        public ErlnetException() { }
        public ErlnetException(string message) : base(message) { }
        public ErlnetException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class Erlnet
    {
        /// <summary>
        /// Executes a MFA over TCP stream and synchronously retrieves the data
        /// </summary>
        /// <param name="stream">TCP stream</param>
        /// <param name="module">Erlang Module atom</param>
        /// <param name="function">Erlang function atom</param>
        /// <param name="args">Function arguments (maybe empty/null)</param>
        /// <returns></returns>
        public static OtpErlangObject CallMFASync(ErlStream stream,
            string module, string function, OtpErlangObject[] args)
        {
            OtpErlangObject[] mfaArray = null;
            OtpOutputStream eouts = new OtpOutputStream();

            mfaArray = new OtpErlangObject[4];

            // mod and fun and args as list
            // ['mod', 'fun', arg1, arg2, ...]
            mfaArray[0] = new OtpErlangAtom("c#ref");
            mfaArray[1] = new OtpErlangAtom(module);
            mfaArray[2] = new OtpErlangAtom(function);
            if (null != args)
                mfaArray[3] = new OtpErlangList(args);
            else
                mfaArray[3] = new OtpErlangList();

            OtpErlangTuple mfaTouple = new OtpErlangTuple(mfaArray);
            eouts = new OtpOutputStream();
            mfaTouple.encode(eouts);

            uint payloadLen = (uint)(eouts.Length + 1);
            byte[] buf = new byte[eouts.Length + 5];

            // added payload size (including protocol id byte)
            // and the missing protocol id byte after it
            buf[0] = (byte)((payloadLen & 0xFF000000) >> 24);
            buf[1] = (byte)((payloadLen & 0x00FF0000) >> 16);
            buf[2] = (byte)((payloadLen & 0x0000FF00) >> 8);
            buf[3] = (byte)((payloadLen & 0x000000FF));
            buf[4] = 131;

            Array.Copy(eouts.GetBuffer(), 0, buf, 5, eouts.Length); // rest of the buffer copied

            stream.Write(buf, 0, buf.Length);

            // wait for data
            DateTime startToWaitForData = DateTime.Now;
            while (!stream.DataAvailable)
            {
                if ((DateTime.Now - startToWaitForData).Seconds > Properties.Settings.Default.StreamResponseTimeout)
                    throw new ErlnetException("Response timeout in call to " + module + ":" + function);
                else
                    Thread.Sleep(0);
            }

            // read till empty
            int readCount = 0;
            payloadLen = 0;

            // read payload length (4 byte header)
            byte[] payloadbuf = new byte[4];
            do {
                readCount += stream.Read(payloadbuf, readCount, payloadbuf.Length - readCount);
                if (readCount != 4)
                    continue;
                else
                    payloadLen = ((uint)payloadbuf[3] & 0x000000FF)
                               + (((uint)payloadbuf[2] << 8) & 0x0000FF00)
                               + (((uint)payloadbuf[1] << 16) & 0x00FF0000)
                               + (((uint)payloadbuf[0] << 24) & 0xFF000000);
                break;
            } while(true);
            //Console.WriteLine("RX " + payloadLen + " bytes");

            // read the payload of length 'payloadLen'
            readCount = 0;
            buf = new byte[payloadLen];
            MemoryStream resp = new MemoryStream();
            do
            {
                readCount += stream.Read(buf, readCount, buf.Length - readCount);
                if (readCount != payloadLen)
                    continue;
                else
                    break;
            } while (true);

            // rebuild term
            resp.Write(buf, 0, buf.Length);
            OtpErlangTuple res = (OtpErlangTuple)OtpErlangObject.decode(new OtpInputStream(resp.GetBuffer()));

            //Console.WriteLine("RX " + res.elementAt(0).ToString());
            return res.elementAt(1);
        }

        public static object[] UnwrapResult(OtpErlangObject obj)
        {
            OtpErlangObject uwobj = null;
            if (obj is OtpErlangTuple
                && ((OtpErlangTuple)obj).arity() == 2
                && ((OtpErlangTuple)obj).elementAt(0) is OtpErlangAtom
                && ((OtpErlangAtom)((OtpErlangTuple)obj).elementAt(0)).atomValue() == "ok")
                uwobj = ((OtpErlangTuple)obj).elementAt(1);
            else uwobj = obj;
            if (uwobj is OtpErlangTuple)
                return ((OtpErlangTuple)uwobj).elements();
            else if (uwobj is OtpErlangList)
                return ((OtpErlangList)uwobj).elements();
            else
                return new object[] { uwobj };
        }
    }
}
