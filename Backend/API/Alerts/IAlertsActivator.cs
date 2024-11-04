public interface IAlertsActivator
{
    void Activate(IEnumerable<Alert> alerts);
    void Deactivate(Alert alert);
}