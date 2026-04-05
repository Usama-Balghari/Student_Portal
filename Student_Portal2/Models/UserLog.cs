namespace Student_Portal2.Models
{
    public class UserLog
    {
        public int Id { get; set; }
        public string? TargetUser { get; set; }
        public string? Action { get; set; }
        public string? PreviousRole { get; set; }
        public string? CurrentRole { get; set; }
        public string? PerformedBy { get; set; }
        public DateTime TimeStamp { get; set; }
    }

}
