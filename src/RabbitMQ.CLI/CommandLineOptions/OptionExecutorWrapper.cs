using System;
using System.Threading.Tasks;
using CommandLine;

namespace RabbitMQ.CLI.CommandLineOptions;

public interface IOptionExecutorWrapper
{
    Task Execute(ParserResult<object> parseResult);
}

public class OptionExecutorWrapper<T> : IOptionExecutorWrapper where T : class, ICommandLineOption
{
    private readonly Func<T, Task> _func;

    public OptionExecutorWrapper(Func<T, Task> func)
    {
        _func = func;
    }

    public Task Execute(ParserResult<object> parseResult)
    {
        return parseResult.WithParsedAsync<T>(o =>
            {
                o.Validate();
                return _func(o);
            }
        );
    }
}