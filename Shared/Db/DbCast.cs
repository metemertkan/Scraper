using System.Text.Json.Serialization;

namespace Shared.Db
{
    public class DbCast
    {
        public DbCast() => Shows = new HashSet<DbShow>();
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime? Birthday { get; set; }
        [JsonIgnore]
        public virtual ICollection<DbShow> Shows { get; set; }
    }
}
