using System;

namespace MarsLite.Web.Data
{
    public class WaitingListEntry
    {
        public int      Id            { get; set; }
        public int      WaitingListId { get; set; }
        public string   Ref           { get; set; }
        public string   PatientName   { get; set; }
        public DateTime PatientDob    { get; set; }
        public DateTime AddedOn       { get; set; }
        public string   Priority      { get; set; }
        public string   Status        { get; set; }

        public int DaysOnList(DateTime today) => (int)(today.Date - AddedOn.Date).TotalDays;
    }
}
