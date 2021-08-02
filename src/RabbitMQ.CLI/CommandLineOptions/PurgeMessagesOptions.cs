using CommandLine;

namespace RabbitMQ.CLI.CommandLineOptions
{
    [Verb("purge-messages", HelpText = "Purges messages from the queue. Alternatively apply a filter or limits.")]
    public class PurgeMessagesOptions : DefaultOptions
    {
        [Option("hash", Required = false, HelpText = "Provide a message hash to purge that single message")]
        public string Hash { get; set; }
        [Option("queue", Required = false, HelpText = "Queue name of queue you want to purge messages from")]
        public string QueueName { get; set; }
        [Option("qid", Required = false, HelpText = "Queue id/hash of queue you want to purge messages from")]
        public string QueueId { get; set; }
        [Option("filter", Required = false, Default = null, HelpText = "Provide a filter to purge messages, only matching that filter.")]
        public string Filter { get; set; }
    }
}