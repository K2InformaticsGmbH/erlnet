using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace Erlang.NET
{
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
        public static OtpErlangObject CallMFASync(NetworkStream stream,
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

            OtpErlangTuple mfaTouple = new OtpErlangTuple(mfaArray);
            eouts = new OtpOutputStream();
            mfaTouple.encode(eouts);

            byte[] buf = new byte[eouts.Length + 1];
            buf[0] = 131; // missing protocol id added to front
            Array.Copy(eouts.GetBuffer(), 0, buf, 1, buf.Length - 1); // rest of the buffer copied

            stream.Write(buf, 0, buf.Length);

            // wait for data
            while (!stream.DataAvailable) Thread.Sleep(100);

            // read till empty
            buf = new byte[1024];
            MemoryStream resp = new MemoryStream();
            int readCount = 0;
            do
            {
                readCount = stream.Read(buf, 0, buf.Length);
                resp.Write(buf, 0, readCount);
            } while (stream.DataAvailable);

            // rebuild term
            OtpErlangTuple res = (OtpErlangTuple)OtpErlangObject.decode(new OtpInputStream(resp.GetBuffer()));
            Console.WriteLine("RX "+res.elementAt(0).ToString());
            OtpErlangObject resObj = res.elementAt(1);
            if (resObj is OtpErlangTuple
                && ((OtpErlangTuple)resObj).elementAt(0) is OtpErlangAtom
                && ((OtpErlangAtom)((OtpErlangTuple)resObj).elementAt(0)).atomValue() == "error")
            {
                OtpErlangTuple excp = (OtpErlangTuple)((OtpErlangTuple)resObj).elementAt(1);
                throw new Exception(excp.ToString());
            }
            else
                return resObj;
        }
    }
}
