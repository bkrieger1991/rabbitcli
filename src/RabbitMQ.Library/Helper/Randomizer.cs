using System;

namespace RabbitMQ.Library.Helper;

public static class Randomizer
{
    public static string GenerateWithGuid(string prefix = "")
    {
        return prefix + Guid.NewGuid().ToString("N");
    }
}