using CommandLine;

namespace RabbitMQ.CLI.CommandLineOptions
{
    [Verb("edit-message", HelpText = "Use this command to modify the content of a message within a queue")]
    public class EditMessageOptions : DefaultOptions
    {
        [Option("qid", Required = false, HelpText = "The queue id where the message you want to edit is contained, alternative to --queue")]
        public string QueueId { get; set; }

        [Option("queue", Required = false, HelpText = "The queue name where the message you want to edit is contained, alternative to --qid")]
        public string QueueName { get; set; }

        [Option("hash", Required = true, HelpText = "The hash of the message you want to edit. If there are more messages with the same hash, it takes the first.")]
        public string Hash { get; set; }
    }
}