namespace FatSecret.Initialization;

public interface IDbInitializer
{
    public Task StartAsync(CancellationToken cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken);
}