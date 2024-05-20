using System.ComponentModel.DataAnnotations.Schema;

namespace MVP_Server.Model.ENTITY
{
    [Table("SensorData")]
    public class SensorDataEntity
    {
        public int Id { get; set; }
        public int IdReading { get; set; }
        public int IdSensor { get; set; }
        public double Data { get; set; }

        public virtual SensorEntity Sensor { get; set; }
        public virtual ReadingEntity Reading { get; set; }
    }
}
