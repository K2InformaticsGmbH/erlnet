using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using K2Informatics.Erlnet;
using System.Net.Sockets;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpClient client = new TcpClient("localhost", 8125);
            NetworkStream s = client.GetStream();

            OtpErlangObject o = Erlnet.CallMFASync(new ErlStream(s), "io", "format", null);
            Console.WriteLine(o.ToString());
        }
    }
}
