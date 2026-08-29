using CreditsService.Entities;

namespace CreditsService.Repositories;

public interface ICreditRepository
{
    Task AddAsync(Credit credit, CreditOperation operation);
}
