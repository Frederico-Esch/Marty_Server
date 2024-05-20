using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Net.Http.Headers;
using MVP_Server.Model;

namespace MVP_Server.InputFormatter
{
    public class MinimumSensorDataInputFormatter : Microsoft.AspNetCore.Mvc.Formatters.InputFormatter
    {
        static readonly string mediaType = "application/sensor-data";

        public MinimumSensorDataInputFormatter() { SupportedMediaTypes.Add(mediaType); }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            using var ms = new MemoryStream();
            await context.HttpContext.Request.Body.CopyToAsync(ms);
            var bytes = ms.ToArray();

            //idSensor sizeof(int) = 4
            //Data     sizeof(double) = 8
            //sizeof(packed struct) = 12
            //sizeof(struct) = 16


            if (bytes.Length % 12 == 0) //packed struct
            {
                var minimumSensorData = new List<MinimumSensorData>(bytes.Length / 12);
                for (int i = 0; i < bytes.Length; i += 12)
                {
                    var idSensor = BitConverter.ToInt32(bytes.AsSpan(i, 4));
                    var data = BitConverter.ToDouble(bytes.AsSpan(i + 4, 8));
                    minimumSensorData.Add(new MinimumSensorData
                    {
                        IdSensor = idSensor,
                        Data = data
                    });
                }
                return InputFormatterResult.Success(minimumSensorData);
            }

            if (bytes.Length % 16 == 0) //non packed struct
            {
                var minimumSensorData = new List<MinimumSensorData>(bytes.Length / 16);
                for (int i = 0; i < bytes.Length; i += 16)
                {
                    var idSensor = BitConverter.ToInt32(bytes.AsSpan(i, 4));
                    var data = BitConverter.ToDouble(bytes.AsSpan(i + 8, 8));

                    minimumSensorData.Add(new MinimumSensorData
                    {
                        IdSensor = idSensor,
                        Data = data
                    });
                }

                return InputFormatterResult.Success(minimumSensorData);
            }

            return InputFormatterResult.Failure();
        }
    }
}
