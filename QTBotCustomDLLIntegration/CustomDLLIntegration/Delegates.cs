using Microsoft.Extensions.Logging;

namespace QTBot.CustomDLLIntegration
{
    public delegate void LogMessage(string integrationName, LogLevel level, string message);
    public delegate void MessageToTwitch(string message);
}
