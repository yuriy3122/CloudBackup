using System.ComponentModel.DataAnnotations;

namespace CloudBackup.Model
{
    public abstract class EntityBase : IEntity
    {
        public EntityBase()
        {
            RowVersion = default!;
        }

        public int Id { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}