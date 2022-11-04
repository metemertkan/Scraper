namespace Shared.Db
{
    public class DbShow
    {
        public DbShow() => Cast = new HashSet<DbCast>();
        public int Id { get; set; }
        public string Name { get; set; }
        public virtual ICollection<DbCast> Cast { get; set; }
    }
}
