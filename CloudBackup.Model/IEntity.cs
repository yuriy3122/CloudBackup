
namespace CloudBackup.Model
{
    public interface IEntity
    {
        int Id { get; set; }

        byte[] RowVersion { get; set; }
    }
}
