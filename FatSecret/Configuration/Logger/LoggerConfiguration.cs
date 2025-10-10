using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace SmartTimber.Server.WebAPI.Configuration.Logger
{
    public static class LoggerConfigurator
    {
        public static ILogger GetSerilogLoggerConfiguration(LoggerConfigurationSettings settings)
        {
            if (settings.IsClientHosted)
            {
                return new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.File(
                        settings.LogExtensionPath,
                        rollingInterval: RollingInterval.Day,
                        rollOnFileSizeLimit: true,
                        outputTemplate: settings.OutputTemplate)
#if DEBUG
                    .WriteTo.Console()
#endif
                    .CreateLogger();
            }

            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
#if DEBUG
                .WriteTo.File(
                    settings.LogExtensionPath,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    outputTemplate: settings.OutputTemplate)
                .WriteTo.Console()
#endif
                .CreateLogger();
        }
    }
}