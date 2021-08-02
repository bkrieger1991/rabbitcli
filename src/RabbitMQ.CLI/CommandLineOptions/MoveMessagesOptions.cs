using CommandLine;

namespace RabbitMQ.CLI.CommandLineOptions
{
    [Verb("move-messages", HelpText = "Moves messages from a source queue to a destination queue")]
    public class MoveMessagesOptions : DefaultListOptions
    {
        [Option("from", Required = false, HelpText = "Define the queue (name) you want to move messages from")]
        public string FromName { get; set; }
        [Option("from-qid", Required = false, HelpText = "Define the queue (name) you want to move messages from")]
        public string FromId { get; set; }

        [Option("to", Required = false, HelpText = "Define the queue (name) you want to move messages to")]
        public string ToName { get; set; }
        [Option("to-qid", Required = false, HelpText = "Define the queue (name) you want to move messages to")]
        public string ToId { get; set; }

        [Option("new", Required = false, HelpText = "If your queue in --to argument does not exist, you can create a new one providing this argument")]
        public bool CreateNew { get; set; }

        [Option("copy", Required = false, HelpText = "Instead of moving message you can also copy them over to a queue")]
        public bool Copy { get; set; }
    }
}