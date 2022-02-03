# RabbitMQ HTTP-Proxy
First, I would to thank you for giving the RabbitMQ HTTP-Proxy a chance :)

# Purpose of this tool
When hosted in your environment, you gain an advantage of integrating applications to publish messages into your rabbitmq instance by just using the HTTP-Protocol.

When you do not have the need of integrate applications to publish messages to your rabbitmq instance, you still can use this tool for your development or staging environment, to simulate publishing messages to rabbitmq, using your favorite HTTP-Tool (*Postman*, etc.) and take advantage of stored requests, shared across your team.

The port inside the container, where the HTTP-API is available, is `15673`.

# Quick references
**GitHub repository:** https://github.com/bkrieger1991/rabbitcli<br>
**Issues on GitHub:**: https://github.com/bkrieger1991/rabbitcli/issues<br>
**Source of this readme:**: [https://github.com/bkrieger1991/.../readme.md](https://github.com/bkrieger1991/rabbitcli/blob/master/src/RabbitMQ.CLI.Proxy/readme.md)

# Content
- [RabbitMQ HTTP-Proxy](#rabbitmq-http-proxy)
- [Purpose of this tool](#purpose-of-this-tool)
- [Quick references](#quick-references)
- [Content](#content)
- [How to use this image](#how-to-use-this-image)
  - [Provide settings](#provide-settings)
  - [Full example](#full-example)
    - [Copy & Paste docker-compose.yaml](#copy--paste-docker-composeyaml)
  - [Define configuration in a `json` file](#define-configuration-in-a-json-file)
- [Using the proxy](#using-the-proxy)
  - [Body](#body)
  - [Parameters](#parameters)
    - [Content-Type](#content-type)
    - [Queue or Exchange](#queue-or-exchange)
    - [RoutingKey](#routingkey)
    - [RabbitMQ Properties](#rabbitmq-properties)
    - [Message-Headers](#message-headers)
    - [Example request](#example-request)
    - [Blacklist request headers](#blacklist-request-headers)
  - [Passthrough basic-auth](#passthrough-basic-auth)

# How to use this image
```sh
docker run --net host --name rabbitmq-proxy -d flux1991/rabbitmq-http-proxy:latest
```
...where `rabbitmq-proxy` is the name of your container

The container will try to connect a rabbitmq-instance running on `localhost:5672` with `guest:guest` credentials, when you publish a message to the container API on `http://localhost:15673`.

## Provide settings
All required settings to connect to the proper rabbitmq-instance will be passed using environment-parameters:

Environment parameter|Default value
---|---
RabbitMQ__Host|localhost
RabbitMQ__Port|5672
RabbitMQ__Username|guest
RabbitMQ__Password|guest
RabbitMQ__VirtualHost|/

## Full example
```sh
docker run -d --name rabbitmq-proxy -p 15673:15673 \
    -e "RabbitMQ__Host|your.rabbitmq.instance" \
    -e "RabbitMQ__Port|5672" \
    -e "RabbitMQ__Username|rabbitmquser" \
    -e "RabbitMQ__Password|superpassword" \
    -e "RabbitMQ__VirtualHost|myvhost" \
    flux1991/rabbitmq-http-proxy:latest
```

### Copy & Paste docker-compose.yaml
```yaml
services:
  rabbitmq-proxy:
    image: flux1991/rabbitmq-http-proxy:latest
    container_name: rabbitmq-proxy
    ports:
      - 15673:15673
    environment:
      - RabbitMQ__Host=your.rabbitmq.instance
      - RabbitMQ__Port=5672
      - RabbitMQ__Username=rabbitmquser
      - RabbitMQ__Password=superpassword
      - RabbitMQ__VirtualHost=myvhost
    restart: unless-stopped
```

## Define configuration in a `json` file
If you - for whatever reasons - want to define the configuration parameters in a file, mounted to the filesystem, that also works.

Here is an example of how a configuration file should look:
```json
{
    "RabbitMQ": {
        "Host": "localhost",
        "Port": 5672,
        "Username": "guest",
        "Password": "guest",
        "VirtualHost": "/"
    }
}
```
*appsettings.docker.json*

When starting the container, mount this settings file into the app directory:
```yaml
services:
  rabbitmq-proxy:
    image: flux1991/rabbitmq-http-proxy:latest
    container_name: rabbitmq-proxy
    ports:
      - 15673:15673
    environment:
      - ASPNETCORE_ENVIRONMENT=docker
    volumes:
      - /path/to/your/config.json:/app/appsettings.docker.json
    restart: unless-stopped
```

> The provided configuration gets loaded by the application and overwrites default settings.

The filename-part `docker` or `appsettings.<something>.json`, where `<something>` must equal the value, you provide for the environment-parameter `ASPNETCORE_ENVIRONMENT`.

This configuration pattern is a built-in feature of Microsoft's `ASP.NET Core`.

# Using the proxy
Once your container is up and running, you can start publishing messages using `HTTP POST` requests to the host, where your container is available, using the mapped port (default: `15673`).

`POST https://<your-host>:<port>/`

## Body
The body of your http-request will exactly be the message-content, read and published with `UTF-8` encoding.

## Parameters
There are three types of parameters: 
- fixed parameters to define for e.g. queue, exchange and routing-key
- parameters, prefixed with `RMQ_` to get the values mapped into message-properties
- parameters that are taken as message-headers

**All parameters are provided as HTTP Request Header.**

### Content-Type
Provide the `Content-Type` parameter, to define the content-type of your published message. Use e.g. `application/json`.

### Queue or Exchange
You can either publish a message to an exchange (default usage) or directly into a queue. *But you can only do one of both at a time.*

Provide the `X-Queue` **OR** `X-Exchange` request header.

### RoutingKey
To define a certain routing-key, provide the `X-RoutingKey` request header.

### RabbitMQ Properties
Provide one of the following headers, to set a message-property:

RabbitMQ Property|Request Header|Example value
---|---|---
AppId|`RMQ_AppId`|`"MyApp"`
CorrelationId|`RMQ_CorrelationId`|`"1234-correlation-id"`
DeliveryMode|`RMQ_DeliveryMode`|`1` or `0`
Expiration|`RMQ_Expiration`|`"10000"`
MessageId|`RMQ_MessageId`|`"1234-message-id"`
Persistent|`RMQ_Persistent`|`1` or `"true"` or `0` or `"false"`
Priority|`RMQ_Priority`|`1` to `9`
ReplyTo|`RMQ_ReplyTo`|`"ReplyTo"`
Type|`RMQ_Type`|`"type"`
UserId|`RMQ_UserId`|`"userid"`

*List of supported message-properties*

For more details and an explanation of the message-properties, visit the official rabbitmq-documentation: https://www.rabbitmq.com/publishers.html#message-properties

### Message-Headers
To define message headers published along with your payload, simply provide more headers.

For example, if you provide a header with name `X-MyCustomHeader` or `MyOtherCustomHeader`, the header and value will exactly be published as defined, as long as it is not contained in the **blacklisted** headers.

### Example request
```http
POST https://rabbitmq.proxy:15673
Content-Type: application/json
X-Exchange: example
X-RoutingKey: my-routing-key
CustomMessageHeader: somevalue

{
    "hello": "world"
}
```

### Blacklist request headers
You can configure blacklisted request headers, in order your application sends headers, that you do not want to get published along with a message.

Simply configure it in your configuration json file:

```json
{
    "RabbitMQ": {
        "HeaderBlacklist": "BlacklistedHeaderKey,OtherBlacklistedKey,OtherKey"
    }
}
```
*appsettings.docker.json*


Or provide this configuration setting as environment-parameter when running container:
```sh
docker run ... -e "RabbitMQ__HeaderBlacklist=OtherKey" ...
```

There are some default headers configured, that are always ignored:
- Content-Length
- Host
- User-Agent
- Accept
- Accept-Encoding
- Connection
- Cache-Control

If you want to clear that pre-configured defaults; you just have to overwrite this setting, too:

```json
{
    "RabbitMQ": {
        "HeaderBlacklist": "OtherKey",
        "DefaultHeaderBlacklist": "User-Agent,Accept"
    }
}
```
*appsettings.docker.json*

## Passthrough basic-auth
You can provide authentication information per request, regardless of the values you defined for `Username`, `Password` and `VirtualHost` (even if you leave this values empty).

Define a `Authorization` request-header with each request, that contains base64 encoded `username:password` credentials and provide the `X-VirtualHost` request header, to define the virtual host to publish the message into.

The authentication will made against a user-account you must create in your rabbitmq instance. This way, you can outsource the credential-configuration into your application that tries to contact the rabbitmq-instance.

Example:

```http
POST https://rabbitmq.proxy:15673
Content-Type: application/json
Authorization: Basic Z3Vlc3Q6Z3Vlc3Q=
X-VirtualHost: /
X-Exchange: example
X-RoutingKey: my-routing-key

{
    "hello": "world"
}
```
*HTTP request code*

Where the `Authorization` request header contains the basic-authentication credentials of `guest:guest`.