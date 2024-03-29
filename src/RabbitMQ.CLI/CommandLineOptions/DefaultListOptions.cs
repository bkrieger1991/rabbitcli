﻿using System.Collections.Generic;
using CommandLine;

namespace RabbitMQ.CLI.CommandLineOptions;

public class DefaultListOptions : DefaultOptions
{
    [Option("limit", Required = false, Default = 0, HelpText = "Limits the output of results to given amount. Defaults to 0 (limitless).")]
    public int Limit { get; set; }

    [Option("filter", Required = false, Default = null, HelpText = "Provide a filter pattern (linq) to show results, only matching that filter.")]
    public string Filter { get; set; }

    [Option("exclude", Separator = ',', Required = false, Default = null, HelpText = "Provide one or more (comma separated) strings to exclude. Works case insensitive.")]
    public IEnumerable<string> Exclude { get; set; }
}