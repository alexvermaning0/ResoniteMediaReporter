using NetCoreServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ResoniteMediaReporter
{
    public class ResoniteWSServer : WsServer
    {
        public ResoniteWSServer(string address, int port) : base(address, port) {}
        public Config Config { get; set; }
        protected override TcpSession CreateSession() { return new ResoniteWSSession(this); }
    }
}
