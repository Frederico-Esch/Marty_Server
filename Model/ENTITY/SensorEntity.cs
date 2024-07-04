using System.ComponentModel.DataAnnotations.Schema;

namespace MVP_Server.Model.ENTITY
{
    [Table("Sensors")]
    public class SensorEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string MeasurementUnit { get;set; }

        public virtual ICollection<SensorDataEntity> SensorData { get; set; }
    }
}
