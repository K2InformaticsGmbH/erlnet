﻿using System;
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
            buf = new byte[1024];
            MemoryStream resp = new MemoryStream();
            int readCount = 0;
            bool waitForMore = false;
            payloadLen = 0;
            do
            {
                readCount = stream.Read(buf, 0, buf.Length);
                if (resp.Length == 0 && readCount < 4)
                    throw new ErlnetException("TCP segment too small!");

                if (resp.Length == 0 && readCount >= 4)
                {
                    // added payload size (including protocol id byte)
                    // and the missing protocol id byte after it
                    payloadLen = ((uint)buf[3] & 0x000000FF)
                               + (((uint)buf[2] << 8) & 0x0000FF00)
                               + (((uint)buf[1] << 16) & 0x00FF0000)
                               + (((uint)buf[0] << 24) & 0xFF000000);
                    resp.Write(buf, 4, readCount - 4);
                }
                else
                    resp.Write(buf, 0, readCount);

                waitForMore = stream.DataAvailable;
                if (!waitForMore && resp.Length < payloadLen)
                    waitForMore = true;
            } while (waitForMore);

            // rebuild term
            OtpErlangTuple res = (OtpErlangTuple)OtpErlangObject.decode(new OtpInputStream(resp.GetBuffer()));
            //Console.WriteLine("RX " + res.elementAt(0).ToString());
            OtpErlangObject resObj = res.elementAt(1);
            return resObj;
        }

        public static OtpErlangObject UnwrapResult(OtpErlangObject obj)
        {
            if (obj is OtpErlangTuple
                && ((OtpErlangTuple)obj).arity() == 2
                && ((OtpErlangTuple)obj).elementAt(0) is OtpErlangAtom
                && ((OtpErlangAtom)((OtpErlangTuple)obj).elementAt(0)).atomValue() == "ok")
                return ((OtpErlangTuple)obj).elementAt(1);
            else return obj;
        }

        public static ArrayList TranslateResult(OtpErlangObject result)
        {
            ArrayList res = new ArrayList();

            if (result is OtpErlangTuple)
                TranslateToArray(res, ((OtpErlangTuple)result).elements());
            else if (result is OtpErlangList)
                TranslateToArray(res, ((OtpErlangList)result).elements());

            return res;
        }

        private static void TranslateToArray(ArrayList res, OtpErlangObject[] elements)
        {
            foreach (OtpErlangObject erlO in elements)
            {
                // Leaf node
                if ((erlO is OtpErlangTuple)
                  && (((OtpErlangTuple)erlO).arity() == 2)
                  && (((OtpErlangTuple)erlO).elementAt(0) is OtpErlangLong))
                {
                    ErlType mtyp = (ErlType)((OtpErlangLong)((OtpErlangTuple)erlO).elementAt(0)).intValue();
                    OtpErlangObject oeo = ((OtpErlangTuple)erlO).elementAt(1);
                    switch (mtyp)
                    {
                        case ErlType.EString:
                            if (oeo is OtpErlangString)
                                res.Add(((OtpErlangString)oeo).stringValue());
                            else if (oeo is OtpErlangBinary)
                                res.Add(((OtpErlangBinary)oeo).stringValue());
                            else if (oeo is OtpErlangList && ((OtpErlangList)oeo).arity() == 0)
                                res.Add("");
                            break;
                        default:
                            throw new Exception("Unknown type " + erlO.ToString());
                    }
                }
                else if (erlO is OtpErlangTuple || (erlO is OtpErlangList && ((OtpErlangList)erlO).arity() > 0))
                    res.Add(TranslateResult(erlO));
                else
                    res.Add(erlO.ToString());
            }
        }
    }
}
