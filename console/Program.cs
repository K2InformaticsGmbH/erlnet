using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Erlang.NET;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace console
{
    class Program
    {
        public static string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        static void Main(string[] args)
        {
            OtpErlangObject[] mfaList = new OtpErlangObject[3];
            OtpErlangObject[] mfaArgs = new OtpErlangObject[4];

            // args for fun
            mfaArgs[0] = new OtpErlangAtom("undefined");
            mfaArgs[1] = new OtpErlangAtom("adminSessionId");
            mfaArgs[2] = new OtpErlangBinary(Encoding.ASCII.GetBytes("admin"));

            OtpErlangObject[] pswdTuple = new OtpErlangObject[2];
            pswdTuple[0] = new OtpErlangAtom("pwdmd5");
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes("change_on_install");
            byte[] hash = md5.ComputeHash(inputBytes);
            pswdTuple[1] = new OtpErlangBinary(hash);

            mfaArgs[3] = new OtpErlangTuple(pswdTuple);

            // mod and fun and args
            mfaList[0] = new OtpErlangAtom("imem_sec");
            mfaList[1] = new OtpErlangAtom("authenticate");
            mfaList[2] = new OtpErlangList(mfaArgs);

            // imem_sec, authenticate, [undefined, adminSessionId, User, {pwdmd5, PswdMD5}]
            OtpErlangList erlMfa = new OtpErlangList(mfaList);

            OtpOutputStream otps = new OtpOutputStream(erlMfa);
            byte[] buf = otps.GetBuffer();

            byte[] newBuf = new byte[buf.Length + 1];
            newBuf[0] = 131;                                // set the prepended value
            Array.Copy(buf, 0, newBuf, 1, buf.Length); // copy the old values

            Console.WriteLine("Executed!" + buf);
            TcpClient client = new TcpClient("localhost", 8124);
            NetworkStream stream = client.GetStream();
            stream.Write(newBuf, 0, newBuf.Length);
        }
    }
}
