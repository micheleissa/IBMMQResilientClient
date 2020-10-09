using IBM.WMQ;
using Microsoft.Extensions.Logging;
using System;

namespace IBMMQResilientClient
{
    public class QueueWriter: IQueueWriter
    {
        private readonly IMQClient _mqClient;
        private readonly ILogger<IQueueWriter> _logger;
        public QueueWriter(IMQClient mqClient, ILogger<IQueueWriter> logger)
        {
            _mqClient = mqClient;
            _logger = logger;
        }


        public void Enqueue(QueueMessage queueMessage)
        {
            var _messagePutOptions = new MQPutMessageOptions();
            MQQueue destination = null;
            MQMessage mqMessage = CreateMQMessage(queueMessage);
            try
            {
                destination = _mqClient.GetResilientQueue(queueMessage.QueueName, MQC.MQOO_OUTPUT | MQC.MQOO_FAIL_IF_QUIESCING);
                destination.Put(mqMessage, _messagePutOptions);
                mqMessage = null;
            }
            catch (MQException ex)
            {
                _logger.LogError(ex, $"Exception occurred while trying to PUT message on IBM MQ: {queueMessage} {ex.Message}");
                throw;
            }
            finally
            {
                destination?.Close();
                ((IDisposable)destination)?.Dispose();
            }
        }

        private MQMessage CreateMQMessage(QueueMessage queueMessage)
        {
            var newMsg = new MQMessage
            {
                Format = MQC.MQFMT_STRING,
                MessageType = MQC.MQMT_DATAGRAM,
                CorrelationId = MQC.MQCI_NONE
            };
            newMsg.WriteBytes(queueMessage.Data);
            return newMsg;
        }
    }
}
