namespace EventTicketing.Services
{
    public interface IActivityLogger
    {
        void LogAction(string userEmail, string actionDescription);
    }
}