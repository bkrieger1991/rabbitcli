using CommandLine;

namespace RabbitMQ.CLI.CommandLineOptions
{
    [Verb("get-messages", HelpText = "Request and filter messages from a queue.")]
    public class GetMessagesOptions : DefaultListOptions
    {
        [Option("qid", Required = false, HelpText = "Provide a queue hash, to focus information output to that single queue.")]
        public string QueueId { get; set; }

        [Option("queue", Required = false, HelpText = "Provide a queue name, to focus information output to that single queue.")]
        public string QueueName { get; set; }

        [Option("live-view", Required = false, HelpText = "ATTENTION: Experimental; attach to a queue using a cloned queue with same exchange bindings. Shows you every incoming message. CTRL+C to cancel viewing and delete the temporary queue.")]
        public bool LiveView { get; set; }

        [Option("dump", Required = false, HelpText = "Provide a directory to dump fetched messages into. Also works with 'live-view'.")]
        public string DumpDirectory { get; set; }

        [Option("dump-metadata", Required = false, HelpText = "Provide this argument, if you want to also dump metadata (like headers and properties) of the event.")]
        public bool DumpMetadata { get; set; }

        [Option("body", Required = false, HelpText = "Outputs the message body instead of information about the message itself.")]
        public bool ContentOnly { get; set; }
        
        [Option("hash", Required = false, HelpText = "Provide a message hash, to focus information output to that single message.")]
        public string Hash { get; set; }

        [Option("headers", Required = false, HelpText = "Enable showing headers of messages")]
        public bool ShowHeaders { get; set; }

        [Option("json", Required = false, HelpText = "Outputs full information for each message in a queue, as json")]
        public bool OutputJsonList { get; set; }
    }
}