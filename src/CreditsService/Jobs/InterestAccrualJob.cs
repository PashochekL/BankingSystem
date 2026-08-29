namespace CreditsService.Jobs;

public sealed class InterestAccrualJob : IInterestAccrualJob
{
    public Task RunAsync()
    {
        return Task.CompletedTask;
    }
}
