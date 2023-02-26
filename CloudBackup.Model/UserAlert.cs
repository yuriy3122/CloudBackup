namespace CloudBackup.Model
{
    public class UserAlert : EntityBase
    {
        public int UserId { get; set; }

        public int AlertId { get; set; }

        public bool IsNew { get; set; }

        public User? User { get; set; }

        public Alert? Alert { get; set; }
    }
}