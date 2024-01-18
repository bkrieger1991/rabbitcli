using System.Linq;
using System.Text;
using AutoMapper;
using RabbitMQ.Client;
using RabbitMQ.Library.Helper;
using RabbitMQ.Library.Models;

namespace RabbitMQ.Library.Mappings;

public class MessageMapping : Profile
{
    public MessageMapping()
    {
        CreateMap<BasicGetResult, AmqpMessage>()
            .ForMember(
                d => d.Content,
                o => o.MapFrom(s => Encoding.UTF8.GetString(s.Body.ToArray()))
            )
            .ForMember(d => d.ContentType, o => o.MapFrom(s => s.BasicProperties.ContentType))
            .ForMember(d => d.Identifier, o => o.MapFrom(s => CreateHash(s)))
            .ForPath(d => d.Fields.DeliveryTag, o => o.MapFrom(s => s.DeliveryTag))
            .ForPath(d => d.Fields.Exchange, o => o.MapFrom(s => s.Exchange))
            .ForPath(d => d.Fields.Redelivered, o => o.MapFrom(s => s.Redelivered))
            .ForPath(d => d.Fields.RoutingKey, o => o.MapFrom(s => s.RoutingKey))
            .ForMember(d => d.Properties, o => o.MapFrom(s => s.BasicProperties));

        CreateMap<IBasicProperties, AmqpMessageProperties>()
            .ForMember(d => d.Headers, o => o.MapFrom(s => s.Headers.ToDictionary(kv => kv.Key, kv => RabbitMqValueHelper.ConvertHeaderValue(kv.Value))))
            .ReverseMap();
    }

    private string CreateHash(BasicGetResult msg)
    {
        var hashValues = new[]
        {
            Encoding.UTF8.GetString(msg.Body.ToArray()),
            msg.Exchange,
            msg.RoutingKey,
            msg.BasicProperties.MessageId,
            msg.BasicProperties.CorrelationId,
            msg.BasicProperties.ContentEncoding,
            msg.BasicProperties.ContentType,
            msg.BasicProperties.ClusterId,
            msg.BasicProperties.UserId,
            msg.BasicProperties.AppId
        };

        var hashString = string.Join("-", hashValues);
        return Hash.GetShortHash(hashString);
    }
}