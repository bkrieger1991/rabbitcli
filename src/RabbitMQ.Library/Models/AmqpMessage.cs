using System.Collections.Generic;

namespace RabbitMQ.Library.Models
{
    public class AmqpMessage
    {
        public string Identifier { get; set; }
        public string Content { get; set; }
        public string ContentType { get; set; }
        public AmqpMessageProperties Properties { get; set; }
        public AmqpMessageFields Fields { get; set; }
    }

    public class AmqpMessageFields
    {
        public string ConsumerTag { get; set; }
        public ulong DeliveryTag { get; set; }
        public string Exchange { get; set; }
        public bool Redelivered { get; set; }
        public string RoutingKey { get; set; }
    }

    public class AmqpMessageProperties
    {
        public string AppId { get; set; }
        public string ClusterId { get; set; }
        public string ContentEncoding { get; set; }
        public string ContentType { get; set; }
        public string CorrelationId { get; set; }
        public string DeliveryMode { get; set; }
        public string Expiration { get; set; }
        public string MessageId { get; set; }
        public string Priority { get; set; }
        public string ReplyTo { get; set; }
        public string Timestamp { get; set; }
        public string Type { get; set; }
        public string UserId { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }
}