﻿using CommandLine;

namespace RabbitMQ.CLI.CommandLineOptions
{
    [Verb("proxy", HelpText = "Starts a WebHost that helps you easily publish messages to RabbitMQ by using an arbitrary tool to manage HTTP API Calls")]
    public class ProxyOptions : DefaultOptions
    {
        [Option("port", Required = false, Default = 15673, HelpText = "Provide a port if the default port collides with your environment")]
        public int Port { get; set; }
    }
}