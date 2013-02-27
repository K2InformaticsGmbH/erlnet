using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Erlang.NET;

namespace console
{
    class Program
    {
        static void Main(string[] args)
        {
            OtpErlangAtom atom = new OtpErlangAtom("test");
            OtpOutputStream otps = new OtpOutputStream(atom);
            byte[] buf = otps.GetBuffer();
            Console.WriteLine("Executed!" + buf);
        }
    }
}
