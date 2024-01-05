using CommandLine;
using FluentValidation;

namespace RabbitMQ.CLI.CommandLineOptions;

[Verb("queue", HelpText = "Fetch queues or output details about a single queue" +
    "\nUsage (example): rabbitcli queue get --limit 10 --sort name --desc" +
    "\nMore help: rabbitcli queue --help")]
public class QueueOptions : DefaultListOptions, ICommandLineOption
{
    public enum Actions
    {
        Get
    }

    [Value(0, MetaName = "Action", HelpText = "Currently you can only \"get\" queues by applying filters or sort")]
    public string Action { get; set; }
    [Option("queue", Required = false, HelpText = "Provide a queue name to request information about the single queue")]
    public string QueueName { get; set; }
    [Option("qid", Required = false, HelpText = "Provide a queue hash to request information about the single queue")]
    public string QueueId { get; set; }
    [Option("sort", Required = false, HelpText = "Provide a field-name of a queue to sort for it")]
    public string Sort { get; set; }
    [Option("desc", Required = false, HelpText = "Sort descending")]
    public bool Descending { get; set; }
    [Option("info", Required = false, HelpText = "Shows summary of applied query manipulations.")]
    public bool ShowQueryInfo { get; set; }

    private sealed class Validator : AbstractValidator<QueueOptions>
    {
        public Validator()
        {
            RuleFor(x => x.Action)
                .NotEmpty()
                .IsEnumName(typeof(Actions), false)
                .WithMessage("Invalid action. Consider using --help");
        }
    }

    public void Validate()
    {
        new Validator().ValidateAndThrow(this);
    }
}