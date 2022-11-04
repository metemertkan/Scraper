using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ScraperSubscriber;
using Shared.Db;
using Shared.Event;
using System.Text;

using var dbContext = new ShowDbContext();
dbContext.Database.EnsureCreated();

var factory = new ConnectionFactory() { HostName = "localhost", UserName = "user", Password = "password" };
using (var connection = factory.CreateConnection())
using (var channel = connection.CreateModel())
{
    channel.QueueDeclare(queue: "shows",
                         durable: false,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);

    var consumer = new EventingBasicConsumer(channel);
    consumer.Received += async (model, ea) =>
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var show = JsonConvert.DeserializeObject<EventShow>(message);

        var foundShow = await dbContext.Shows.FirstOrDefaultAsync(x => x.Id.Equals(show.Id));

        if (foundShow is null)
        {
            var dbShow = new DbShow
            {
                Id = show.Id,
                Name = show.Name
            };

            foreach (var cast in show.Casts)
            {
                var foundCast = await dbContext.Casts.FirstOrDefaultAsync(x => x.Id.Equals(cast.Id));
                if (foundCast is null)
                {
                    dbShow.Cast.Add(new DbCast { Id = cast.Id, Name = cast.Name, Birthday = cast.Birthday });
                }
                else
                {
                    if (dbShow.Cast.FirstOrDefault(x => x.Id.Equals(cast.Id)) is null)
                    {
                        dbShow.Cast.Add(foundCast);
                    }
                }
            }
            await dbContext.AddAsync(dbShow);
            await dbContext.SaveChangesAsync();


            Console.WriteLine(" [x] Received {0}", message);
        };

        channel.BasicConsume(queue: "shows",
                             autoAck: true,
                             consumer: consumer);
    };
}