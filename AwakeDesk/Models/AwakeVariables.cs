using AwakeDesk.Helpers;

namespace AwakeDesk.Models
{
    public class AwakeVariables
    {
        private DateTime closingDateTime;
        public DateTime ClosingDateTime
        {
            get { return closingDateTime; }
            set
            {
                if (closingDateTime != value)
                {
                    closingDateTime = value;
                    alarmDateTime = value.AddMinutes(-App.AwakeDeskSettings.AlarmDelayMinutes);
                }
            }
        }
        DateTime alarmDateTime;
        public DateTime AlarmDateTime
        {
            get { return alarmDateTime; }
        }
        public string ClosingTime { get; set; }

        public AwakeDeskHelpers.Point MouseDestinationStartingPoint { get; set; }
        public AwakeVariables()
        {
            ClosingDateTime = DateTime.Now.AddMinutes(-30);
            ClosingTime = "00:00";

            MouseDestinationStartingPoint = new() { X = 40, Y = 40 };
        }

    }
}
