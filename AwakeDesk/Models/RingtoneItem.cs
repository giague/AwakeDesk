namespace AwakeDesk.Models
{
    public class RingtoneItem
    {
        public string Path { get; set; }
        public string Name { get; set; }

        public RingtoneItem()
        {
            Path = "";
            Name = "";
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
