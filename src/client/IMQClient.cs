using IBM.WMQ;

namespace IBMMQResilientClient
{
    public interface IMQClient
    {
        MQQueue GetQueue(string queueName, int openOptions);
        MQQueue GetResilientQueue(string queueName, int openOptions);
    }
}
