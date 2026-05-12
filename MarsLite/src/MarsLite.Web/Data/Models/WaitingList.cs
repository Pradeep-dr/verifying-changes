namespace MarsLite.Web.Data
{
    public class WaitingList
    {
        public int    Id              { get; set; }
        public int    ProviderId      { get; set; }
        public string Name            { get; set; }
        public string Specialty       { get; set; }
        public string Status          { get; set; }
        public int    MaxCapacity     { get; set; }
        public int    TargetWaitDays  { get; set; }
        public string DefaultPriority { get; set; }
        public bool   AutoAssign      { get; set; }
        public string Notes           { get; set; }
    }

    public class WaitingListSummary
    {
        public int    Id         { get; set; }
        public string Name       { get; set; }
        public string Specialty  { get; set; }
        public string Status     { get; set; }
        public int    EntryCount { get; set; }
    }
}
