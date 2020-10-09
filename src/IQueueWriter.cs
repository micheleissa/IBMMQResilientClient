namespace IBMMQResilientClient
{
    public interface IQueueWriter
    {
        void Enqueue(QueueMessage queueMessage);
    }
}
