namespace Services.Configuration
{
    public class GoalHistoryConfiguration
    {
        public StorageFormat Format { get; set; } = StorageFormat.PlainText;
        public string LogDirectory { get; set; } = "log";
        public string FileName { get; set; } = "goal-history";
    }

    public enum StorageFormat
    {
        PlainText,
        Json
    }
}