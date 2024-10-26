namespace TaskZ.API.Models.Reports
{
    public class TaskCompletionReport
    {
        public Guid UserId { get; set; }
        public int CompletedTasksLast30Days { get; set; }
        public double AverageTasksPerDay { get; set; }
    }
}
