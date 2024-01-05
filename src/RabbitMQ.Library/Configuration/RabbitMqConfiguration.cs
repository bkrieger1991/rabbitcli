using System;
using System.Linq;
using System.Web;

namespace RabbitMQ.Library.Configuration;

[Serializable]
public class RabbitMqConfiguration
{
    [Serializable]
    public class AmqpParameters
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }
        public int Port { get; set; }
        public string Hostname { get; set; }
        public bool IsAmqps { get; set; }
        public bool Unsecure { get; set; }
        public string TlsVersion { get; set; }
        public string TlsServerName { get; set; }

        public Uri ToUri()
        {
            return new Uri(
                string.Format(
                    "amqp{0}://{1}:{2}@{3}:{4}/{5}",
                    IsAmqps ? "s" : "",
                    Username,
                    HttpUtility.UrlEncode(Password),
                    Hostname,
                    Port,
                    VirtualHost
                )
            );
        }
    }

    [Serializable]
    public class WebParameters
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public string Hostname { get; set; }
        public bool Ssl { get; set; }
        public bool Unsecure { get; set; }

        public Uri ToUri()
        {
            return new Uri(
                string.Format(
                    "http{0}://{1}:{2}@{3}:{4}",
                    Ssl ? "s" : "",
                    Username,
                    HttpUtility.UrlEncode(Password),
                    Hostname,
                    Port
                )
            );
        }
    }

    public string Name { get; set; }
    public AmqpParameters Amqp { get; set; } = new();
    public WebParameters Web { get; set; } = new();

    public static RabbitMqConfiguration Create(
        string amqpConnectionString,
        bool ignoreInvalidCertificates,
        string amqpsTlsVersion, 
        string amqpsTlsServerName,
        string webConnectionString,
        string name
    )
    {
        if (name.IsEmpty())
        {
            name = "default";
        }

        var config = new RabbitMqConfiguration();
        config.Name = name;
        config.Amqp = ParseAmqpConnectionString(amqpConnectionString);
        config.Amqp.Unsecure = ignoreInvalidCertificates;
        config.Amqp.TlsVersion = amqpsTlsVersion;
        config.Amqp.TlsServerName = amqpsTlsServerName;
        config.Web = ParseWebConnectionString(webConnectionString);
        config.Web.Unsecure = ignoreInvalidCertificates;

        if (config.Amqp.Username == null && config.Web.Username == null)
        {
            throw new ArgumentException(
                "You must provide at least one set of credentials in the connection strings"
            );
        }

        // Since user has the possibility to provide user credentials once, as long as they are equal for both protocols,
        // Take the opposite credentials, if one of both are not provided.
        if (config.Amqp.Username == null && config.Web.Username != null)
        {
            config.Amqp.Username = config.Web.Username;
            config.Amqp.Password = config.Web.Password;
        }
        if (config.Web.Username == null && config.Amqp.Username != null)
        {
            config.Web.Username = config.Amqp.Username;
            config.Web.Password = config.Amqp.Password;
        }

        return config;
    }

    private static AmqpParameters ParseAmqpConnectionString(string amqpConnectionString)
    {
        if (!TryParseUri(amqpConnectionString, out var amqpUri))
        {
            throw new ArgumentException(
                "Error: invalid amqp connection string",
                nameof(amqpConnectionString)
            );
        }
        (var amqpUser, var amqpPassword) = SplitUserInfo(amqpUri.UserInfo);

        return new AmqpParameters()
        {
            Hostname = amqpUri.Host,
            Port = amqpUri.Port,
            Username = amqpUser,
            Password = amqpPassword,
            VirtualHost = amqpUri.Segments.Length == 2 ? amqpUri.Segments[1] : "/",
            IsAmqps = amqpUri.Scheme == "amqps"
        };
    }

    private static WebParameters ParseWebConnectionString(string webConnectionString)
    {
        if (!TryParseUri(webConnectionString, out var webUri))
        {
            throw new ArgumentException(
                "Error: invalid web connection string",
                nameof(webConnectionString)
            );
        }

        (var webUser, var webPassword) = SplitUserInfo(webUri.UserInfo);

        return new WebParameters()
        {
            Hostname = webUri.Host,
            Port = webUri.Port,
            Username = webUser,
            Password = webPassword,
            Ssl = webUri.Scheme == "https"
        };
    }

    private static bool TryParseUri(string uri, out Uri parsed)
    {
        try
        {
            parsed = new Uri(uri);
            return true;
        }
        catch
        {
            parsed = null;
            return false;
        }
    }

    private static (string User, string Password) SplitUserInfo(string userInfo)
    {
        if (string.IsNullOrWhiteSpace(userInfo))
        {
            return (null, null);
        }

        if (!userInfo.Contains(":"))
        {
            throw new ArgumentException(
                "The username and password part must contain a ':' like \"username:password\""
            );
        }

        var splitted = userInfo.Split(":");
        
        var username = splitted[0];
        var password = HttpUtility.UrlDecode(splitted[1]);
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username must not be empty");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password must not be empty");
        }
        return (username, password);
    }
}