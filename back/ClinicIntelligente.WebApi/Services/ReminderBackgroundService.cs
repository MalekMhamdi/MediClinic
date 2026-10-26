using ClinicIntelligente.Core.Application.Interfaces.Services;

namespace ClinicIntelligente.WebApi.Services;

/// <summary>
/// Runs once per day at the configured hour (default 08:00)
/// and sends J-1 appointment reminder emails + in-app notifications.
/// </summary>
public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderBackgroundService> _log;
    private readonly TimeOnly _runAt;

    public ReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReminderBackgroundService> log,
        IConfiguration cfg)
    {
        _scopeFactory = scopeFactory;
        _log = log;

        // Configurable via appsettings: "Reminders:RunAtHour" (default 8)
        var hour = cfg.GetValue<int>("Reminders:RunAtHour", 8);
        _runAt = new TimeOnly(hour, 0);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("[Reminders] Background service started — will fire daily at {Time}", _runAt);

        while (!ct.IsCancellationRequested)
        {
            var now  = DateTime.Now;
            var next = DateTime.Today.Add(_runAt.ToTimeSpan());

            // If today's run time has already passed, schedule for tomorrow
            if (now >= next)
                next = next.AddDays(1);

            var delay = next - now;
            _log.LogInformation("[Reminders] Next run in {Hours:F1} h (at {Next:yyyy-MM-dd HH:mm})",
                delay.TotalHours, next);

            try { await Task.Delay(delay, ct); }
            catch (OperationCanceledException) { break; }

            await SendRemindersAsync(ct);
        }
    }

    private async Task SendRemindersAsync(CancellationToken ct)
    {
        var targetDate = DateTime.Today.AddDays(1);
        _log.LogInformation("[Reminders] Sending J-1 reminders for {Date:yyyy-MM-dd}", targetDate);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var appointmentService = scope.ServiceProvider.GetRequiredService<IAppointmentService>();
            await appointmentService.SendRemindersAsync(targetDate);
            _log.LogInformation("[Reminders] Done for {Date:yyyy-MM-dd}", targetDate);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[Reminders] Failed for {Date:yyyy-MM-dd}", targetDate);
        }
    }
}
