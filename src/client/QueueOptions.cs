using System.Collections.Generic;

namespace IBMMQResilientClient
{
    public class QueueOptions
    {
        public string Name { get; set; }
        public string AppName { get; set; }
        public string ManagerName { get; set; }
        public string Channel { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        
        //TBD: future use
        public string SubscriptionName { get; set; }
        public string Topic { get; set; }

        public List<MqHostOptions> MqHostOptionsList { get; set; }
        public string CertPassword { get; set; }
        public string ClientCert { get; set; }
        public string ServerCert { get; set; }
        public bool InstallCert { get; set; }
    }

    public class MqHostOptions
    {
        public string HostName { get; set; }
        public string Port { get; set; }

    }
}
