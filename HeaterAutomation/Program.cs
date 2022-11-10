using System.Diagnostics;
using HeaterAutomation;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureHostConfiguration(config =>
    {
        config.SetBasePath(GetBasePath());
        config.AddJsonFile("appsettings.json", false);
    })
    .ConfigureServices(services => { services.AddHostedService<Worker>(); })
    .Build();

host.Run();


string GetBasePath()
{
    using var processModule = Process.GetCurrentProcess().MainModule;
    return Path.GetDirectoryName(processModule?.FileName)!;
}
