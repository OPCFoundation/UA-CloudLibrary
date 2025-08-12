
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Cloud.Library;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

namespace AdminShell
{
    public class SimpleServer : StandardServer
    {
        private readonly ApplicationInstance _app;

        public SimpleServer(ApplicationInstance app, uint port)
        {
            _app = app;

            _app.ApplicationConfiguration.ServerConfiguration.BaseAddresses[0] = "opc.tcp://localhost:" + port;
        }

        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            List<INodeManager> nodeManagers = new()
            {
                new NodesetFileNodeManager(server, configuration)
            };

            return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
        }

        public async Task StartServerAsync()
        {
            Console.WriteLine("Starting OPC UA server...");

            try
            {
                await _app.Start(this).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("StartServerAsync: " + ex.Message);
                return;
            }

            Console.WriteLine("OPC UA server started.");
        }
    }
}
