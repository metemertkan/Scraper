namespace Shared.Event
{
    public class EventShow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<EventCast> Casts { get; set; }
    }
}
