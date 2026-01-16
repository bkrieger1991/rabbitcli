using CommandLine;
using FluentValidation;
using RabbitMQ.Library;

namespace RabbitMQ.CLI.CommandLineOptions;

[Verb("message", HelpText = "All about handling messages." +
    "\nGet, edit, move or purge messages. Restore messages from saved dumps." +
    "\nUsage: rabbitcli message <action> <options>" +
    "\nExample: rabbitcli message get --queue testqueue --limit 10" +
    "\nMore help: rabbitcli message --help")]
public class MessageOptions : DefaultListOptions, ICommandLineOption
{
    public enum Actions
    {
        Get, Edit, Move, Purge, Restore
    }

    [Value(0, MetaName = "Action", HelpText = "Provide one action of \"get\", \"edit\", \"move\", \"purge\" or \"restore\".")]
    public string Action { get; set; }

    // ========== Mixed usage
    [Option("qid", Required = false, HelpText = "Common: (Required) The queue id, alternative to --queue")]
    public string QueueId { get; set; }
    [Option("queue", Required = false, HelpText = "Common: (Required) The queue name, alternative to --qid")]
    public string QueueName { get; set; }
    [Option("hash", Required = false, HelpText = "Common: The hash of the message you want to edit, get or purge. If there are more messages with the same hash, it takes the first.")]
    public string Hash { get; set; }

    // ========= Move exclusive
    [Option("from", Required = false, HelpText = "Moving: Define the queue (name) you want to move messages from")]
    public string FromName { get; set; }
    [Option("from-qid", Required = false, HelpText = "Moving: Define the queue (id) you want to move messages from")]
    public string FromId { get; set; }
    [Option("to", Required = false, HelpText = "Moving: Define the queue (name) you want to move messages to")]
    public string ToName { get; set; }
    [Option("to-qid", Required = false, HelpText = "Moving: Define the queue (id) you want to move messages to")]
    public string ToId { get; set; }
    [Option("new", Required = false, HelpText = "Moving: If your queue in --to argument does not exist, you can create a new one providing this argument")]
    public bool CreateNew { get; set; }
    [Option("copy", Required = false, HelpText = "Moving: Instead of moving message you can also copy them over to a queue")]
    public bool Copy { get; set; }

    // ======== Get exclusive
    [Option("live-view", Required = false, HelpText = "Fetching: ATTENTION: Experimental; attach to a queue using a cloned queue with same exchange bindings. Shows you every incoming message. CTRL+C to cancel viewing and delete the temporary queue.")]
    public bool LiveView { get; set; }
    [Option("dump", Required = false, HelpText = "Fetching/Restore: Provide a directory to dump fetched messages into or restore messages from. Also works with 'live-view'.")]
    public string DumpDirectory { get; set; }
    [Option("dump-metadata", Required = false, HelpText = "Fetching: Provide this argument, if you want to also dump metadata (like headers and properties) of the event.")]
    public bool DumpMetadata { get; set; }
    [Option("body", Required = false, HelpText = "Fetching: Outputs the message body instead of information about the message itself.")]
    public bool ContentOnly { get; set; }
    [Option("headers", Required = false, HelpText = "Fetching: Enable showing headers of messages")]
    public bool ShowHeaders { get; set; }
    [Option("json", Required = false, HelpText = "Fetching: Outputs full information for each message in a queue, as json")]
    public bool OutputJsonList { get; set; }

    private sealed class Validator : AbstractValidator<MessageOptions>
    {
        public Validator()
        {
            RuleFor(x => x.Action)
                .NotEmpty()
                .IsEnumName(typeof(Actions), false)
                .WithMessage("Invalid action. Consider using --help");

            // ===== Rules for QueueId and QueueName
            RuleFor(x => x.QueueId)
                .NotEmpty()
                .When(x => x.QueueName.IsEmpty() && !x.Action.Is(Actions.Move))
                .WithMessage("Please provide either --qid (queue id) or --queue (queue name)");
            RuleFor(x => x.QueueName)
                .NotEmpty()
                .When(x => x.QueueId.IsEmpty() && !x.Action.Is(Actions.Move))
                .WithMessage("Please provide either --qid (queue id) or --queue (queue name)");
            RuleFor(x => x.QueueId)
                .Empty()
                .When(x => x.Action.Is(Actions.Move))
                .WithMessage("When moving messages, please use --to-id and --from-id");
            RuleFor(x => x.QueueName)
                .Empty()
                .When(x => x.Action.Is(Actions.Move))
                .WithMessage("When moving messages, please use --to and --from");

            // ====== Rules for Limit
            RuleFor(x => x.Limit)
                .Empty()
                .When(x => x.Action.Is(Actions.Purge))
                .WithMessage("Using --limit is not supported when purging");

            // ====== Rules for Hash
            RuleFor(x => x.Hash)
                .NotEmpty()
                .When(x => x.Action.Is(Actions.Edit))
                .WithMessage("When editing a message, you have to provide a message-hash");

            // ====== Rules for Exclude
            RuleFor(x => x.Exclude)
                .Empty()
                .WithMessage("Using --exclude is currently not supported when handling messages");

            // ====== Rules for FromName, FromId and ToName, ToId
            RuleFor(x => x.FromId)
                .Empty()
                .When(x => !x.Action.Is(Actions.Move))
                .WithMessage("Use --from-id only with move command");
            RuleFor(x => x.FromName)
                .Empty()
                .When(x => !x.Action.Is(Actions.Move))
                .WithMessage("Use --from only with move command");
            RuleFor(x => x.ToId)
                .Empty()
                .When(x => !x.Action.Is(Actions.Move))
                .WithMessage("Use --to-id only with move command");
            RuleFor(x => x.ToName)
                .Empty()
                .When(x => !x.Action.Is(Actions.Move))
                .WithMessage("Use --to-id only with move command");

            RuleFor(x => x.FromId)
                .NotEmpty()
                .When(x => x.Action.Is(Actions.Move) && x.FromName.IsEmpty())
                .WithMessage("When moving messages you must either provide --from-id or --from");
            RuleFor(x => x.FromName)
                .NotEmpty()
                .When(x => x.Action.Is(Actions.Move) && x.FromId.IsEmpty())
                .WithMessage("When moving messages you must either provide --from-id or --from");
            RuleFor(x => x.ToId)
                .NotEmpty()
                .When(x => x.Action.Is(Actions.Move) && x.ToName.IsEmpty())
                .WithMessage("When moving messages you must either provide --to-id or --to");
            RuleFor(x => x.ToName)
                .NotEmpty()
                .When(x => x.Action.Is(Actions.Move) && x.ToId.IsEmpty())
                .WithMessage("When moving messages you must either provide --to-id or --to");

            // ====== Rules for Copy, New
            RuleFor(x => x.CreateNew)
                .Must(x => !x)
                .When(x => !x.Action.Is(Actions.Move))
                .WithMessage("You can use --new option only when moving messages");
            RuleFor(x => x.Copy)
                .Must(x => !x)
                .When(x => !x.Action.Is(Actions.Move))
                .WithMessage("You can use --copy option only when moving messages");

            // ====== Rules for get-options
            RuleFor(x => x.DumpDirectory)
                .Empty()
                .When(x => !x.Action.Is(Actions.Get) || !x.Action.Is(Actions.Restore))
                .WithMessage("You can use --dump <directory> only when fetching messages with \"get\" or restoring a dump with \"restore\"");
            RuleFor(x => x.LiveView)
                .Must(x => !x)
                .When(x => !x.Action.Is(Actions.Get))
                .WithMessage("You can use --live-view option only when fetching messages with \"get\"");
            RuleFor(x => x.DumpMetadata)
                .Must(x => !x)
                .When(x => !x.Action.Is(Actions.Get))
                .WithMessage("You can use --dump-metadata option only when fetching messages with \"get\"");
            RuleFor(x => x.DumpMetadata)
                .Must(x => !x)
                .When(x => x.Action.Is(Actions.Get) && x.DumpDirectory.IsEmpty())
                .WithMessage("You can use --dump-metadata only when --dump <directory> option is provided.");
            RuleFor(x => x.ContentOnly)
                .Must(x => !x)
                .When(x => !x.Action.Is(Actions.Get))
                .WithMessage("You can use --body option only when fetching messages with \"get\"");
            RuleFor(x => x.ShowHeaders)
                .Must(x => !x)
                .When(x => !x.Action.Is(Actions.Get))
                .WithMessage("You can use --headers option only when fetching messages with \"get\"");
            RuleFor(x => x.OutputJsonList)
                .Must(x => !x)
                .When(x => !x.Action.Is(Actions.Get))
                .WithMessage("You can use --json option only when fetching messages with \"get\"");
        }
    }

    public void Validate()
    {
        new Validator().ValidateAndThrow(this);
    }
}