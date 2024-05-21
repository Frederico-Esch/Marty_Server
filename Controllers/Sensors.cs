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
        public ActionResult CreateData([FromBody] MinimumSensorData data)
        {
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

            foreach (var data in sensorData)
            {
                _dataContext
                    .SensorData
                    .Add(new SensorDataEntity
                    {
                        IdSensor = data.IdSensor,
                        IdReading = lastReading.Id,
                        Data = data.Data
                    });
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


            return result;
        }
        [HttpGet("GetAllSensor")]
        public List<string> GetAllSensor()
        {
            var result = _dataContext.Sensors.Select(sensor => sensor.Name).ToList();



            return result;
        }
        [HttpGet("GetDateSensor")]
        public List<CompleteData> GetDateSensor(DateTime start, DateTime end)
        {
            var result = _dataContext.SensorData.Include(data => data.Reading).Where(date => date.Reading.Date > start && date.Reading.Date < end )
                .Include(data => data.Sensor)
                            .Select(data => new CompleteData
                            {
                                Name = data.Sensor.Name,
                                Data = data.Data,
                                Date = data.Reading.Date
                            }).ToList();


            return result;
        }
        [HttpGet("GetMediaSensor")]
        public CompleteMedia GetMediaSensor(DateTime start, DateTime end, int id)
        {
            var result = _dataContext.SensorData.Where(data => data.IdSensor == id)
                .Include(data => data.Reading).Where(date => date.Reading.Date > start && date.Reading.Date < end)
                .Include(data => data.Sensor).ToList();//Aggregate(new CompleteMedia { Media = 0, Begin = start, End = end}, (a,b) => a + b.Data )

            var media = result.Aggregate(new CompleteMedia { Media = 0, Begin = start, End = end }, (a, b) =>
            {
                a.Media += b.Data;
                a.Begin = a.Begin > b.Reading.Date ? a.Begin : b.Reading.Date;
                a.End = a.End < b.Reading.Date ? a.End : b.Reading.Date;
                return a;
            });
            media.Media /= result.Count;


            return media;
        }
        #endregion
    }

    public struct CompleteData
    {
        public string Name { get; set;}
        public DateTime Date { get; set; }
        public Double Data { get; set; }
    }
    public struct CompleteMedia
    {
        public string Name { get; set; }
        public DateTime Begin { get; set; }
        public DateTime End { get; set; }
        public double Media { get; set; }
    }

    public record MinimalSensor (
        int Id,
        string Name
    );
}
