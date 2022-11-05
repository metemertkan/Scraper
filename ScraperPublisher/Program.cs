
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Shared.Event;
using Shared.Input;
using System.Net.Http.Json;
using System.Text;

var factory = new ConnectionFactory() { HostName = "rabbitmq", UserName = "user", Password = "password" };


var policy = Policy<bool>.Handle<HttpRequestException>(ex => ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests).WaitAndRetryForeverAsync(
attempt => TimeSpan.FromMilliseconds(
10 * Math.Pow(2, attempt)));


var fallbackPolicyOnNotFound = Policy<bool>.Handle<HttpRequestException>(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound).FallbackAsync(async ct =>
{
    Console.WriteLine("Got 404 stopping");
    return await Task.FromResult(true);
});

var policyWrap = fallbackPolicyOnNotFound.WrapAsync(policy);

using (var connection = factory.CreateConnection())
using (var channel = connection.CreateModel())
{
    channel.QueueDeclare(queue: "shows",
                         durable: false,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);

    using var httpClient = new HttpClient()
    {
        BaseAddress = new Uri("https://api.tvmaze.com/")
    };

    var pageNumber = 0;
    var internalCancel = false;
    while (!internalCancel)
    {
        internalCancel = await policyWrap.ExecuteAsync(async () =>
        {
            var result = await httpClient.GetFromJsonAsync<List<Show>>($"shows?page={pageNumber}");

            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 10
            };

            await Parallel.ForEachAsync(result, parallelOptions, async (item, token) =>
            {

                await policy.ExecuteAsync(async () =>
                {
                    var castResult = await httpClient.GetFromJsonAsync<List<Cast>>($"shows/{item.Id}/cast");

                    var dbShow = new EventShow
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Casts = castResult.GroupBy(x => x.Person.Id).Select(g => g.First()).Select(x => new EventCast { Id = x.Person.Id, Name = x.Person.Name, Birthday = x.Person.Birthday }).ToList()
                    };

                    var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dbShow));

                    channel.BasicPublish(exchange: "",
                                         routingKey: "shows",
                                         basicProperties: null,
                                         body: body);

                    Console.WriteLine(" [x] Sent {0}", item.Id);

                    return await Task.FromResult(false);
                });

            });

            return await Task.FromResult(false);
        });
        pageNumber++;
    }

}









