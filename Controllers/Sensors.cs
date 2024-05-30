using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using MVP_Server.DAL;
using MVP_Server.InputFormatter;
using MVP_Server.Model;
using MVP_Server.Model.ENTITY;

namespace MVP_Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class Sensors : ControllerBase
    {
        private readonly ILogger<Sensors> _logger;
        private readonly DataContext _dataContext;

        public Sensors(ILogger<Sensors> logger, DataContext dataContext) {
            _logger = logger;
            _dataContext = dataContext;
        }

        #region ESP
        [HttpGet("SensorId")]
        public List<MinimalSensor> GetSensorId([FromQuery] string name) =>
            _dataContext.Sensors.Where(sensor => sensor.Name.ToLower().Contains(name.ToLower())).Select(s => new MinimalSensor(s.Id, s.Name)).ToList();

        [HttpPost("CreateReading")]
        public ActionResult CreateReading()
        {
            var result = _dataContext
                            .Readings
                            .Add(new ReadingEntity { Date = DateTime.Now });
            _dataContext.SaveChanges();

            if (result != null) { return Ok(result.Entity.Id); }

            return StatusCode(400, -1);
        }

        [HttpPost("CreateData")]
        [Obsolete("Use SendData endpoint")]
        public ActionResult CreateData([FromBody] MinimumSensorData data) //nem funciona mais pq o input formatter só funciona com listas
        {
            return StatusCode(500, "Deprecated endpoint, use sendData");
            var lastReading = _dataContext.Readings.OrderBy(r => r.Date).LastOrDefault();
            if (lastReading == null) return StatusCode(400, -1);

            var result = _dataContext
                            .SensorData
                            .Add(new SensorDataEntity
                            {
                                IdSensor = data.IdSensor,
                                IdReading = lastReading.Id,
                                Data = data.Data
                            });
            _dataContext.SaveChanges();

            if (result == null) { return StatusCode(400, -1); };

            return Ok(result.Entity.Id);
        }

        [HttpPost("SendData")]
        public ActionResult SendData([FromBody] List<MinimumSensorData> sensorData)
        {
            var lastReading = _dataContext.Readings.OrderBy(r => r.Date).LastOrDefault();
            if (lastReading == null) return StatusCode(400, -1);

            var failedResults = sensorData.Select(data => new
            {
                Id = data.IdSensor,
                Result = _dataContext
                    .SensorData
                    .Add(new SensorDataEntity
                    {
                        IdSensor = data.IdSensor,
                        IdReading = lastReading.Id,
                        Data = data.Data
                    })
            })
            .Where(result => result.Result == null)
            .Select(fr => fr.Id)
            .ToList();

            _logger.LogInformation($"Failed Inserts {failedResults.Count}");
            if (failedResults.Count > 0) {
                _logger.LogError(failedResults.ToString());
                return StatusCode(450, failedResults);
            }
            _dataContext.SaveChanges();

            return Ok();
        }
        #endregion

        #region Front
        [HttpGet("GetAllData")]
        public List<CompleteData> GetAllData()
        {
            var result = _dataContext.SensorData.Include(data => data.Sensor).Include(data => data.Reading)
                            .Select(data => new CompleteData { 
                                Name = data.Sensor.Name,
                                Data = data.Data,
                                Date = data.Reading.Date
                            }).ToList();


            return result;
        }
        [HttpGet("GetDataSensor")]
        public List<CompleteData> GetDataSensor(List<int> ids)
        {
            var result = _dataContext.SensorData.Where(data => ids.Contains(data.IdSensor))
                .Include(data => data.Sensor).Include(data => data.Reading)
                            .Select(data => new CompleteData
                            {
                                Name = data.Sensor.Name,
                                Data = data.Data,
                                Date = data.Reading.Date
                            }).ToList();


            _logger.LogInformation($"GetDataSensor requested for {ids}: found {result.Count}");
            return result;
        }
        [HttpGet("GetAllSensor")]
        public List<MinimalSensor> GetAllSensor()
        {
            var result = _dataContext.Sensors.Select(sensor => new MinimalSensor(sensor.Id, sensor.Name)).ToList(); 

            return result;
        }
        #endregion
    }

    public struct CompleteData
    {
        public string Name { get; set;}
        public DateTime Date { get; set; }
        public double Data { get; set; }
    }
    public record MinimalSensor (
        int Id,
        string Name
    );
}
