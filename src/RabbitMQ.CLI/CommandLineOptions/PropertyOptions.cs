using System;
using CommandLine;
using FluentValidation;
using RabbitMQ.Library;

namespace RabbitMQ.CLI.CommandLineOptions;

[Serializable]
[Verb("property", HelpText = "Read or edit global configuration properties." +
    "\nUsage: 'rabbitcli property get' or 'rabbitcli property set --name <property> --value <new value>'" +
    "\nHelp: rabbitcli property --help")]
public class PropertyOptions : ICommandLineOption
{
    public enum Actions 
    {
        Get, Set
    }

    [Value(0, Required = true, MetaName = "Action", HelpText = "Either \"get\" or \"set\" a connection configuration.")]
    public string Action { get; set; }

    [Option("name", Required = false, HelpText = "Provide the property name you want to set")]
    public string Name { get; set; }

    [Option("value", Required = false, HelpText = "Provide the value you want to set into property, provided with --set")]
    public string Value { get; set; }

    private sealed class Validator : AbstractValidator<PropertyOptions>
    {
        public Validator()
        {
            RuleFor(x => x.Action)
                .NotEmpty()
                .IsEnumName(typeof(Actions), false)
                .WithMessage("Invalid action. Consider using --help");
            RuleFor(x => x.Name)
                .NotEmpty()
                .When(x => x.Action.Is(Actions.Set))
                .WithMessage("You must provide the --name option");
            RuleFor(x => x.Value)
                .NotEmpty()
                .When(x => x.Action.Is(Actions.Set))
                .WithMessage("You must provide the --value option");
        }
    }

    public void Validate()
    {
        new Validator().ValidateAndThrow(this);
    }
}