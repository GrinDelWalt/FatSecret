namespace SmartTimber.Server.WebAPI.Configuration.Logger;

public class LoggerConfigurationSettings
{
    public string LogExtensionPath { get; set; }
    public string JsonExtensionPath { get; set; }
    public string OutputTemplate { get; set; }
    public bool IsClientHosted { get; set; }
}