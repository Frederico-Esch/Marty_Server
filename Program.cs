using MVP_Server.DAL;
using MVP_Server.InputFormatter;

namespace MVP_Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers(options =>
            {
                options.InputFormatters.Add(new MinimumSensorDataInputFormatter());
            });

            builder.Services.AddDbContext<DataContext>();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if(app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Configure the HTTP request pipeline.

            //app.UseHttpsRedirection();
            //app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
