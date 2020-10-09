using IBM.WMQ;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace IBMMQResilientClient
{
    public class MQClient : IMQClient
    {
        private readonly object InstanceLoker = new object();
        private readonly QueueOptions _queueOptions;
        private Hashtable _connectionOptions;
        private MQQueueManager _manager;
        private readonly ILogger<MQClient> _logger;
        private readonly RetryPolicy _defaultPolicy = Policy.Handle<MQException>()
                                .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        public MQClient(QueueOptions mqOptions, ILogger<MQClient> logger)
        {
            _queueOptions = mqOptions;
            _logger = logger;
            CreateConnectionOptions();
            if (_queueOptions.InstallCert)
            {
                InstallCerts();
            }
        }


        #region Helpers
        private MQQueueManager Manager
        {
            get
            {
                if (_manager == null || _manager?.IsConnected == false || _manager?.ReasonCode != MQC.OK)
                {
                    lock (InstanceLoker)
                    {
                        if (_manager == null || _manager?.IsConnected == false || _manager?.ReasonCode != MQC.OK)
                            GetMqManager();
                    }

                }

                return _manager;
            }
        }


        public MQQueue GetQueue(string queueName, int openOptions)
        {
            MQQueue queue = null;
            try
            {
                queue = Manager.AccessQueue(queueName, openOptions);

            }
            catch (MQException ex)
            {
                _logger.LogError($"Error while trying to access IBM MQ {queueName} - {ex.Message}");
                DisposeQueueManagerConnection();
                throw;
            }
            return queue;
        }


        public MQQueue GetResilientQueue(string queueName, int openOptions)
        {
            var policyResult = _defaultPolicy.ExecuteAndCapture(() => GetQueue(queueName ?? _queueOptions.Name, openOptions));
            EnsureSuccess(policyResult);
            return policyResult.Result;
        }


        private void EnsureSuccess(PolicyResult<MQQueue> policyResult)
        {
            if (policyResult.Outcome == OutcomeType.Failure && policyResult.FinalException != null)
            {
                throw policyResult.FinalException;
            }
        }


        private MQQueueManager GetMqManager()
        {
            var QManagerName = _queueOptions.ManagerName;
            if (_manager?.IsConnected == true) return _manager;
            _logger.LogInformation($"Attempting to connect to queue manager: {QManagerName}");

            try
            {
                _manager = new MQQueueManager(QManagerName, _connectionOptions);
                _logger.LogInformation($"Connected to queue manager: {QManagerName}");
            }
            catch (MQException ex)
            {
                _logger.LogError(ex, $"A WebSphere MQ error occurred while creating Connection to QManager [{QManagerName}] : {ex.ToString()}");
                throw;
            }

            return _manager;
        }


        private void CreateConnectionOptions()
        {
            if (_queueOptions == null)
                throw new ArgumentException("No IBM MQ config options was found!");

            _connectionOptions = new Hashtable()
                {
                    { MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_MANAGED },
                    {MQC.APPNAME_PROPERTY, _queueOptions.AppName },
                    { MQC.CHANNEL_PROPERTY, _queueOptions.Channel },
                    { MQC.CONNECT_OPTIONS_PROPERTY, MQC.MQCNO_RECONNECT_Q_MGR },
                    { MQC.CONNECTION_NAME_PROPERTY, string.Join(",", _queueOptions.MqHostOptionsList.Select(opt => $"{opt.HostName}({opt.Port})"))},
                    { MQC.USER_ID_PROPERTY, $"{_queueOptions.Username}"},
                    { MQC.PASSWORD_PROPERTY, $"{_queueOptions.Password}" }
                };
            if (_queueOptions.InstallCert)
            {
                //TLS
                _connectionOptions.Add(MQC.SSL_CIPHER_SPEC_PROPERTY, "TLS_RSA_WITH_AES_128_CBC_SHA256");
                _connectionOptions.Add(MQC.SSL_CERT_STORE_PROPERTY, "*USER");
            }
        }

        private void InstallCerts()
        {
            var userStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            var rootStore = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var clientCertPath = Path.Combine(baseDirectory, _queueOptions.ClientCert);
            var serverCertPath = Path.Combine(baseDirectory, _queueOptions.ServerCert);
            var serverCert = new X509Certificate2(serverCertPath);
            var clientCert = new X509Certificate2(clientCertPath, _queueOptions.CertPassword);
            var serverCollection = new X509Certificate2Collection
                {
                    serverCert,
                };
            var clientCollection = new X509Certificate2Collection
                {
                    clientCert
                };
            ////set client cert friendly name for windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var user = WindowsIdentity.GetCurrent().Name.Split(new[] { "\\" }, StringSplitOptions.None)[1].ToLower();
                clientCert.FriendlyName = $"ibmwebspheremq{user}";
            }

            userStore.Open(OpenFlags.ReadWrite);
            userStore.AddRange(clientCollection);
            userStore.Close();

            rootStore.Open(OpenFlags.ReadWrite);
            rootStore.AddRange(serverCollection);
            rootStore.Close();

        }

        private void DisposeQueueManagerConnection()
        {
            if (_manager?.IsConnected == true)
            {
                _logger.LogInformation("Disconnecting Queue Manager.");
                _manager?.Disconnect();
                _logger.LogInformation("Queue Manager disconnected.");
            }
            _logger.LogInformation("Disposing Queue Manager.");
            ((IDisposable)_manager)?.Dispose();
            _logger.LogInformation("Disposed Queue Manager.");
            _manager = null;
        }

        #endregion
    }
}
