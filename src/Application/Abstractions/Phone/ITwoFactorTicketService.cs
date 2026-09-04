namespace Application.Abstractions.Phone;

public interface ITwoFactorTicketService
{
    string CreateTicket(Guid userId, Guid? tenantId, string email, bool isHost);
    bool TryValidateTicket(string ticket, out Guid userId, out Guid? tenantId, out bool isHost);
}
