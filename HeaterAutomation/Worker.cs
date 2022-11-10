using System.Diagnostics;

namespace HeaterAutomation;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return EnableHeating();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enableHeating = true;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await SetHeating(enableHeating);
            enableHeating = !enableHeating;
            await Task.Delay(TimeSpan.FromMinutes(_configuration.GetValue<int>("RunEveryMinutes")), stoppingToken);
        }
    }

    public Task SetHeating(bool enable) => enable ? EnableHeating() : DisableHeating();

    private async Task EnableHeating(int retry = 0)
    {
        if (retry > 5)
        {
            _logger.LogError($"Maximum retry count reached for {nameof(EnableHeating)}");
            return;
        }

        Process switchProcess = RunShortcutProcess("Switch Heater");
        
        await switchProcess.WaitForExitAsync();

        Process shortcutProcess = RunShortcutProcess("Heat On");

        await shortcutProcess.WaitForExitAsync();
        
        if (shortcutProcess.ExitCode != 0)
        {
            _logger.LogError(await shortcutProcess.StandardError.ReadToEndAsync());
            await Task.Delay(TimeSpan.FromSeconds(10));
            await EnableHeating(retry++);
        }
        
        _logger.LogInformation("Heating enabled at {time}", DateTimeOffset.Now);
    }

    private async Task DisableHeating()
    {
        Process switchProcess = RunShortcutProcess("Switch Heater");
        
        await switchProcess.WaitForExitAsync();
        
        var shortcutProcess = RunShortcutProcess("Heat Off");
        
        await shortcutProcess.WaitForExitAsync();

        if (shortcutProcess.ExitCode != 0)
        {
            _logger.LogError(await shortcutProcess.StandardError.ReadToEndAsync());
        }
        
        _logger.LogInformation("Heating disabled at {time}", DateTimeOffset.Now);
    }

    private Process RunShortcutProcess(string shortcutName) =>
        // Process.Start("zsh", $"shortcuts run \"${shortcutName}\"");
        Process.Start(new ProcessStartInfo
        {
            FileName = "shortcuts",
            Arguments = $"run \"{shortcutName}\"",
            RedirectStandardError = true
        })!;
}
