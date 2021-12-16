using System;
using RabbitMQ.Client;
using RabbitMQ.Library.Helper;

namespace RabbitMQ.Library
{
    public class TemporaryExchange : IDisposable
    {
        private readonly IModel _model;

        public string Name { get; set; }

        public TemporaryExchange(IModel model, string name)
        {
            Name = name;
            _model = model;
            _model.ExchangeDeclare(Name, "fanout", true);
        }

        public static TemporaryExchange Create(IModel model)
        {
            var exchangeName = Randomizer.GenerateWithGuid("RMQM_MV_");
            return new TemporaryExchange(model, exchangeName);
        }

        public TemporaryExchange BindTo(string queue)
        {
            _model.QueueBind(queue, Name, "");
            return this;
        }

        public void Publish(IBasicProperties properties, byte[] body, string routingKey = null)
        {
            _model.BasicPublish(Name, routingKey ?? "", false, properties, body);
        }

        public void Delete()
        {
            _model.ExchangeDelete(Name, false);
        }

        public void Dispose()
        {
            Delete();
        }
    }
}