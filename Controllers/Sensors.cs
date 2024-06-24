using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using MVP_Server.DAL;
using MVP_Server.InputFormatter;
using MVP_Server.Model;
using MVP_Server.Model.ENTITY;
using System.Linq.Expressions;

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
        public List<MinimalSensor> GetSensorId([FromQuery] string name)
        {

            _logger.LogInformation("Searching for sensor with name {Name}", name);
            try //hidden control flow
            {
               return _dataContext.Sensors
                    .Where(sensor => sensor.Name.ToLower().Contains(name.ToLower()))
                    .Select(s => new MinimalSensor(s.Id, s.Name))
                    .ToList();
            }
            catch(Exception e) {
                _logger.LogError("Error while finding sensor {ErrorMessage}", e.Message);
                return [];
            }

        }

        [HttpPost("CreateReading")]
        public ActionResult CreateReading()
        {
            try
            {
                var result = _dataContext
                                .Readings
                                .Add(new ReadingEntity { Date = DateTime.Now });
                _dataContext.SaveChanges();
                if (result != null) {
                    _logger.LogInformation("Reading created, {ID} - {Date}", result.Entity.Id, result.Entity.Date);
                    return Ok(result.Entity.Id);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error while creating reading {ErrorMessage}", e.Message);
                return StatusCode(400, -1);
            }

            _logger.LogError($"Unknown error while creating reading");
            return StatusCode(400, -1);
        }

        [HttpPost("CreateData")]
        [Obsolete("Use SendData endpoint")]
        public ActionResult CreateData([FromBody] MinimumSensorData data) //nem funciona mais pq o input formatter só funciona com listas
        {
            _logger.LogError("Deprecated endpoint acessed");
            return StatusCode(500, "Deprecated endpoint, use sendData");
            //var lastReading = _dataContext.Readings.OrderBy(r => r.Date).LastOrDefault();
            //if (lastReading == null) return StatusCode(400, -1);

            //var result = _dataContext
            //                .SensorData
            //                .Add(new SensorDataEntity
            //                {
            //                    IdSensor = data.IdSensor,
            //                    IdReading = lastReading.Id,
            //                    Data = data.Data
            //                });
            //_dataContext.SaveChanges();
            //if (result == null) { return StatusCode(400, -1); };

            //return Ok(result.Entity.Id);
        }

        [HttpPost("SendData")]
        public ActionResult SendData([FromBody] List<MinimumSensorData> sensorData)
        {
            ReadingEntity? lastReading;
            try
            {
                lastReading = _dataContext.Readings.OrderBy(r => r.Date).LastOrDefault();
                if (lastReading == null)
                {
                    _logger.LogError("No last reading found");
                    return StatusCode(400, -1);
                }
            }
            catch(Exception e)
            {
                _logger.LogError("Error while getting last reading {ErrorMessage}", e.Message);
                return StatusCode(400, -1);
            }

            try
            {
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

                if (failedResults.Count > 0) {
                    _logger.LogError("Failed Inserts {FailCount}\n{FailedResults}", failedResults.Count, failedResults.ToString());
                    return StatusCode(450, failedResults);
                }
                _dataContext.SaveChanges();
            }
            catch (Exception e)
            {
                _logger.LogError("Error while inserting sensor data {ErrorMessage}", e.Message);
                return StatusCode(400, -1);
            }
            _logger.LogInformation("Insert and commit successful");

            return Ok();
        }
        #endregion

        #region Front
        [HttpGet("GetAllData")]
        public List<CompleteData> GetAllData()
        {
            _logger.LogInformation("Request for all data");
            try
            {
                var result = _dataContext.SensorData.Include(data => data.Sensor).Include(data => data.Reading)
                                .Select(data => new CompleteData { 
                                    Name = data.Sensor.Name,
                                    Data = data.Data,
                                    Date = data.Reading.Date
                                }).ToList();
                return result;
            }
            catch(Exception e)
            {
                _logger.LogError("Error while getting all data {ErrorMessage}", e.Message);
                return [];
            }
        }

        [HttpGet("GetSensorData")]
        public List<CompleteData> GetSensorData(List<int> ids)
        {
            _logger.LogInformation("GetSensorData requested for {Ids}", ids);
            try
            {
                var result = _dataContext.SensorData.Where(data => ids.Contains(data.IdSensor))
                    .Include(data => data.Sensor).Include(data => data.Reading)
                                .Select(data => new CompleteData
                                {
                                    Name = data.Sensor.Name,
                                    Data = data.Data,
                                    Date = data.Reading.Date
                                }).ToList();


                _logger.LogInformation("Found {results}", result);
                return result;
            }
            catch(Exception e)
            {
                _logger.LogError("Error while getting sensor data {ErrorMessage}", e.Message);
                return [];
            }
        }

        [HttpGet("GetAllSensor")]
        public List<MinimalSensor> GetAllSensor()
        {
            try
            {
                _logger.LogInformation("Request for all sensors");
                return _dataContext.Sensors.Select(sensor => new MinimalSensor(sensor.Id, sensor.Name)).ToList();
            }
            catch(Exception e)
            {
                _logger.LogError("Error while retrieving all sensors {ErrorMessage}", e.Message);
                return [];
            }

        }
        [HttpGet("GetByDate")]
        public List<CompleteData> GetByDate(DateTime date)
        {
            var result = _dataContext.Readings
                .Where(reading => date.Day == reading.Date.Day && date.Month == reading.Date.Month && date.Year == reading.Date.Year)
                .Include(reading => reading.SensorData)
                .ThenInclude(SensorData => SensorData.Sensor)
                .ToList()
                .SelectMany(reading => reading.SensorData
                    .Select(sensorData => new CompleteData
                    {
                        Name = sensorData.Sensor.Name,
                        Data = sensorData.Data,
                        Date = reading.Date
                    })).ToList();

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
