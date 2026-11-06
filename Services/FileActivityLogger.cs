using System;
using System.IO;

namespace EventTicketing.Services
{
    public class FileActivityLogger : IActivityLogger
    {
        private readonly string _logFilePath = "activity_log.txt";

        public void LogAction(string userEmail, string actionDescription)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] User: {userEmail} | Action: {actionDescription}{Environment.NewLine}";
                File.AppendAllText(_logFilePath, logEntry);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
    }
}