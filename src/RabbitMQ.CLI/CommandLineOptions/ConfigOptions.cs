using System;
using System.Security.Authentication;
using CommandLine;
using FluentValidation;
using RabbitMQ.Library;
using RabbitMQ.Library.Configuration;

namespace RabbitMQ.CLI.CommandLineOptions;

[Serializable]
[Verb("config", HelpText = "Handle configurations for different RabbitMQ instances." +
    "\nAdd, edit, get or delete configurations. " +
    "\nConfig file is located in your user directory as 'rabbitcli.json'." +
    "\nUsage: rabbitcli config <action> <options>" +
    "\nExample: rabbitcli config get --name default" +
    "\nMore help: rabbitcli config --help" +
    "\nHint with 'rabbitcli config use --name <name>' you can set any configuration as the default.")]
public class ConfigOptions : ICommandLineOption
{
    public enum Actions 
    {
        Add, Get, Edit, Use, Delete
    }

    [Value(0, Required = true, MetaName = "Action", HelpText = "Either \"add\", \"get\", \"edit\", \"use\" or \"delete\" a connection configuration.")]
    public string Action { get; set; }

    [Option("amqp", Required = false, HelpText = "Example: amqps://user:password@your.host.com:5671/my-vhost. Hint: You only have to define user and password once, if they stay the same for API and AMQP connections. You have to provide username and password as url-encoded strings")]
    public string AmqpConnectionString { get; set; }

    [Option("web", Required = false, HelpText = "Example: https://user:password@your.mgmt-url.com:15672")]
    public string WebConnectionString { get; set; }

    [Option("name", Required = false, HelpText = "Name of your config to refer to it. If empty, it will be stored with name 'default'")]
    public string ConfigName { get; set; }

    [Option("ignore-invalid-cert", Required = false, HelpText = "Provide this option, if you don't want certificates to be validated.")]
    public bool IgnoreInvalidCertificates { get; set; }

    [Option("amqps-tls-version", Required = false, HelpText = "Provide TLS version used by your amqps certificate")]
    public string AmqpsTlsVersion { get; set; }

    [Option("amqps-tls-server", Required = false, HelpText = "Provide server-name used for amqps certificate validation")]
    public string AmqpsTlsServerName { get; set; }
    
    private sealed class Validator : AbstractValidator<ConfigOptions>
    {
        public Validator()
        {
            RuleFor(x => x.Action)
                .NotEmpty()
                .IsEnumName(typeof(Actions), false)
                .WithMessage("Invalid action. Consider using --help");
            RuleFor(x => x.ConfigName)
                .NotEmpty()
                .When(x => x.Action.IsIn(new[] { Actions.Edit, Actions.Use, Actions.Delete }))
                .WithMessage("You must define a config-name with --name option");
            RuleFor(x => x.AmqpConnectionString)
                .NotEmpty()
                .When(x => x.Action.Is(Actions.Add))
                .WithMessage("You must provide the amqp connection string with --amqp option");
            RuleFor(x => x.WebConnectionString)
                .NotEmpty()
                .When(x => x.Action.Is(Actions.Add))
                .WithMessage("You must provide the web connection string with --web option");
            RuleFor(x => x.AmqpsTlsVersion)
                .IsEnumName(typeof(SslProtocols), false)
                .WithMessage("Invalid TLS version provided in --amqps-tls-version");
        }
    }

    public void Validate()
    {
        new Validator().ValidateAndThrow(this);
    }

    public RabbitMqConfiguration Parse()
    {
        return RabbitMqConfiguration.Create(
            AmqpConnectionString,
            IgnoreInvalidCertificates,
            AmqpsTlsVersion,
            AmqpsTlsServerName,
            WebConnectionString,
            ConfigName
        );
    }
}