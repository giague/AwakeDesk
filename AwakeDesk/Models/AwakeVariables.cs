using AwakeDesk.Helpers;
using System.ComponentModel;

namespace AwakeDesk.Models
{
    public class AwakeVariables : INotifyPropertyChanged
    {
        public const int NEXT_POS_AREA_WIDTH = 300;
        public const int NEXT_POS_AREA_HEIGHT = 300;

        public event PropertyChangedEventHandler? PropertyChanged;
        private string closingTime;
        private DateTime closingDateTime;
        public DateTime ClosingDateTime
        {
            get { return closingDateTime; }
            set
            {
                if (closingDateTime != value)
                {
                    closingDateTime = value;
                    alarmDateTime = value.AddMinutes(-App.ADSettings.AlarmDelayMinutes);
                }
            }
        }
        DateTime alarmDateTime;
        public DateTime AlarmDateTime
        {
            get { return alarmDateTime; }
        }

        public string ClosingTime
        {
            get => closingTime;
            set
            {
                if (closingTime != value)
                {
                    closingTime = value;
                    OnPropertyChanged(nameof(ClosingTime));
                }
            }
        }

        public AwakeDeskHelpers.AreaRect MouseDestinationAreaRect { get; set; }

        AwakeDeskHelpers.Point mouseDestinationAreaPoint;


        public AwakeDeskHelpers.Point MouseDestinationAreaPoint
        {
            get => mouseDestinationAreaPoint;
            set
            {
                mouseDestinationAreaPoint = value;
                MouseDestinationAreaRect = new AwakeDeskHelpers.AreaRect(value);
            }
        }

        public AwakeVariables()
        {
            ClosingDateTime = DateTime.Now.AddMinutes(-30);
            ClosingTime = "00:00";
            closingTime = "00:00";
            MouseDestinationAreaPoint = new() { X = 40, Y = 40 };
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
