using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Erlang.NET;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Collections;

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
            Mpro mpro = Mpro.Connect("localhost", 8125);
            mpro.Authenticate("admin", "change_on_install");
            mpro.Login();
            ArrayList l = mpro.listDestinationChannels("smpp");

            l = null;
            mpro = null;
        }
    }
}
