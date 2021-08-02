using CommandLine;

namespace RabbitMQ.CLI.CommandLineOptions
{
    [Verb("get-queues", HelpText = "Request and filter queues.")]
    public class GetQueuesOptions : DefaultListOptions
    {
        [Option("queue", Required = false, HelpText = "Provide a queue name to request information about the single queue")]
        public string QueueName { get; set; }
        [Option("qid", Required = false, HelpText = "Provide a queue hash to request information about the single queue")]
        public string QueueId { get; set; }
        [Option("sort", Required = false, HelpText = "Provide a fieldname of a queue to sort for it")]
        public string Sort { get; set; }
        [Option("desc", Required = false, HelpText = "Sort descending")]
        public bool Descending { get; set; }
    }
}