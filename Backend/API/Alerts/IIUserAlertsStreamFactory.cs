
public interface IUserAlertsStreamFactory
{
    UserAlertsStream Create(Guid subscriptionId);
}