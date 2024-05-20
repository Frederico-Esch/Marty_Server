using System.ComponentModel.DataAnnotations.Schema;

namespace MVP_Server.Model.ENTITY
{
    [Table("Readings")]
    public class ReadingEntity
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }

        public virtual ICollection<SensorDataEntity> SensorData { get; set; }
    }
}
