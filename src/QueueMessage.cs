namespace IBMMQResilientClient
{
    public class QueueMessage
    {
        //TBD: future use
        public string CorrelationId { get; set; }
        public string QueueName { get; set; }
        public string Data { get; set; }

        public override string ToString()
        {
            return $"QueueName: {QueueName} Message: {Data}";
        }
    }
}
