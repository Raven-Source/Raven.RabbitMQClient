namespace Raven.Message.RabbitMQ.Configuration
{
    public class ProducerConfiguration
    {
        public ProducerConfiguration(ushort maxWorker = 1, bool sendConfirm = true, int sendConfirmTimeout = 1000,
            bool messagePersistent = false, int messageDelay = 0)
        {
            MaxWorker = maxWorker;
            SendConfirm = sendConfirm;
            SendConfirmTimeout = sendConfirmTimeout;
            MessagePersistent = messagePersistent;
            MessageDelay = messageDelay;
        }
        public ushort MaxWorker { get; set; }
        public bool SendConfirm { get; set; }
        public int SendConfirmTimeout { get; set; }
        public bool MessagePersistent { get; set; }
        public int MessageDelay { get; set; }
        
    }
}
