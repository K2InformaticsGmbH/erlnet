using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Collections;

namespace Erlang.NET
{
    public enum MproType
    {
        MproString = 1
    };

    public class Mpro : Imem
    {
        protected Mpro(NetworkStream _stream) : base(_stream) { }

        public new static Mpro Connect(string host, int port)
        {
            TcpClient client = new TcpClient(host, port);
            NetworkStream s = client.GetStream();

            return new Mpro(s);
        }

        public ArrayList listDestinationChannels(string AppId)
        {
            return CallMproMFASync("listDestinationChannels", AppId);
        }
		public ArrayList putSourcePeerName(string AppId, string Key, string SpName)
        {
            return CallMproMFASync("putSourcePeerName", AppId, Key, SpName);
        }
        public ArrayList deleteSourcePeer(string AppId, string Key)
        {
            return CallMproMFASync("deleteSourcePeer", AppId, Key);
        }
        public ArrayList listSourcePeerKeys(string AppId)
        {
            return CallMproMFASync("listSourcePeerKeys", AppId);
        }
        public ArrayList listSourcePeers(string AppId)
        {
            return CallMproMFASync("listSourcePeers", AppId);
        }
        public ArrayList putSourcePeer(string AppId, string Key, string ChStr, string OptStr)
        {
            return CallMproMFASync("deleteWhitelist", AppId, Key, ChStr, OptStr);
        }
        public ArrayList putWhitelist(string AppId, string Key, string IpAddressStr, string OptStr)
        {
            return CallMproMFASync("deleteWhitelist", AppId, Key, IpAddressStr, OptStr);
        }
        public ArrayList deleteWhitelist(string AppId, string Key, string IpAddressStr)
        {
            return CallMproMFASync("deleteWhitelist", AppId, Key, IpAddressStr);
        }
        public ArrayList getSourcePeer(string AppId, string Key)
        {
            return CallMproMFASync("getSourcePeer", AppId, Key);
        }
        public ArrayList getWhitelist(string AppId, string Key)
        {
            return CallMproMFASync("getWhitelist", AppId, Key);
        }

        private ArrayList CallMproMFASync(string fun, params object[] argsRest)
        {
            OtpErlangObject[] mfaArgs = new OtpErlangObject[argsRest.Length];

            for (int i=0; i<mfaArgs.Length ; ++i)
                mfaArgs[i] = new OtpErlangString((string)argsRest[i]);

            OtpErlangTuple res = (OtpErlangTuple)CallMFASync(stream, "mpro_dal_prov", fun, mfaArgs);
            if (((OtpErlangAtom)res.elementAt(0)).atomValue() == "ok")
                return TranslateResult(res.elementAt(1));
            else
                return null;
        }

        private static ArrayList TranslateResult(OtpErlangObject result)
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
                // Leaf
                if ((erlO is OtpErlangTuple)
                  && (((OtpErlangTuple)erlO).arity() == 2)
                  && (((OtpErlangTuple)erlO).elementAt(0) is OtpErlangLong)) 
                {
                    MproType mtyp = (MproType)((OtpErlangLong)((OtpErlangTuple)erlO).elementAt(0)).intValue();
                    OtpErlangObject oeo = ((OtpErlangTuple)erlO).elementAt(1);
                    switch (mtyp) {
                        case MproType.MproString:
                            if (oeo is OtpErlangString)
                                res.Add(((OtpErlangString)oeo).stringValue());
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