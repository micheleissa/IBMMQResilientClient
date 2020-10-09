using IBM.WMQ;
using Microsoft.Extensions.Logging;
using System;

namespace IBMMQResilientClient
{
    public class QueueReader : IQueueReader
    {
        private readonly IMQClient _mqClient;
        private readonly ILogger<IQueueReader> _logger;
        private readonly MQGetMessageOptions _messageGetOptions = new MQGetMessageOptions();
        private DateTimeOffset LastQueueEmptyWarningReported;
        const int QueueEmptyReportTimeIntervalMinutes = 10;

        public QueueReader(IMQClient mQClient, ILogger<IQueueReader> logger)
        {
            _mqClient = mQClient;
            _logger = logger;
        }

        public QueueMessage Dequque()
        {
            QueueMessage queueMessage = null;
            MQQueue destination = null;
            try
            {
                destination = _mqClient.GetResilientQueue(queueMessage?.QueueName, MQC.MQOO_INPUT_AS_Q_DEF | MQC.MQOO_FAIL_IF_QUIESCING);
                MQMessage receivedMsg = new MQMessage();
                destination.Get(receivedMsg, _messageGetOptions);
                queueMessage = ToQueueMessage(receivedMsg);
            }
            catch (MQException ex)
            {
                if (ex.ReasonCode == MQC.MQRC_NO_MSG_AVAILABLE)
                {
                    ReportQueueEmpty(ex);
                }
                else
                {
                    _logger.LogError(ex, $"Error getting message from IBM MQ {ex.ToString()} {ex.GetErrorCodeDescription()}");
                    throw;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting message from IBM MQ {ex.ToString()} ");
            }
            finally
            {
                destination?.Close();
                ((IDisposable)destination)?.Dispose();
            }

            return queueMessage;
        }

        public QueueMessage Peek()
        {
            QueueMessage queueMessage = null;
            MQQueue destination = null;
            try
            {
                destination = _mqClient.GetResilientQueue(queueMessage?.QueueName, MQC.MQOO_BROWSE | MQC.MQOO_FAIL_IF_QUIESCING);
                MQMessage receivedMsg = new MQMessage();
                MQGetMessageOptions gmo = new MQGetMessageOptions
                {
                    Options = MQC.MQGMO_BROWSE_NEXT
                };
                destination.Get(receivedMsg, gmo);
                queueMessage = ToQueueMessage(receivedMsg);
            }
            catch (MQException ex)
            {
                if (ex.ReasonCode == MQC.MQRC_NO_MSG_AVAILABLE)
                {
                    ReportQueueEmpty(ex);
                }
                else
                {
                    _logger.LogError(ex, $"Error getting message from IBM MQ {ex.ToString()} {ex.GetErrorCodeDescription()}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting message from IBM MQ {ex.ToString()} ");
            }
            finally
            {
                destination?.Close();
                ((IDisposable)destination)?.Dispose();
            }
            return queueMessage;
        }


        #region Helpers
        private QueueMessage ToQueueMessage(MQMessage message)
        {
            QueueMessage resultMessage = null;
            if (message != null)
            {
                resultMessage = new QueueMessage
                {
                    Data = System.Text.Encoding.UTF8.GetString(message.ReadBytes(message.MessageLength))
                };
            }
            return resultMessage;
        }

        private void ReportQueueEmpty(MQException ex)
        {
            var timeSpan = DateTimeOffset.UtcNow.Subtract(LastQueueEmptyWarningReported);

            if (timeSpan.TotalMinutes > QueueEmptyReportTimeIntervalMinutes)
            {
                _logger.LogWarning($"Queue is empty error getting the message.{ex.ToString()} {ex.GetErrorCodeDescription()}");
                LastQueueEmptyWarningReported = DateTimeOffset.UtcNow;
            }

        }
        #endregion
    }
}
