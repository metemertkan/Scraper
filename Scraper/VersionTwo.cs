using Microsoft.EntityFrameworkCore;
using Polly;
using Shared.Db;
using Shared.Input;
using System.Net.Http.Json;

namespace Scraper
{
    public class VersionTwo
    {
        public async Task Execute()
        {
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.tvmaze.com/")
            };

            using var dbContext = new ShowDbContext();
            dbContext.Database.EnsureCreated();

            var policy = Policy<bool>.Handle<HttpRequestException>(ex => ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests).WaitAndRetryForeverAsync(
                attempt => TimeSpan.FromMilliseconds(
                10 * Math.Pow(2, attempt)));

            var fallbackPolicyOnNotFound = Policy<bool>.Handle<HttpRequestException>(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound).FallbackAsync(async ct =>
            {
                Console.WriteLine("Got 404 stopping");
                return await Task.FromResult(true);
            });

            var policyWrap = fallbackPolicyOnNotFound.WrapAsync(policy);

            var pageNumber = 0;
            var internalCancel = false;
            while (!internalCancel)
            {
                internalCancel = await policyWrap.ExecuteAsync(async () =>
                {
                    var result = await httpClient.GetFromJsonAsync<List<Show>>($"shows?page={pageNumber}");
                    foreach (var item in result)
                    {
                        Console.Write($"Show Id: {item.Id} ");
                        var castResult = await httpClient.GetFromJsonAsync<List<Cast>>($"shows/{item.Id}/cast");
                        castResult = castResult.GroupBy(x => x.Person.Id).Select(g => g.First()).ToList();
                        var foundShow = await dbContext.Shows.FirstOrDefaultAsync(x => x.Id.Equals(item.Id));
                        if (foundShow is null)
                        {
                            var dbShow = new DbShow
                            {
                                Id = item.Id,
                                Name = item.Name
                            };

                            foreach (var castItem in castResult)
                            {
                                var foundCast = await dbContext.Casts.FirstOrDefaultAsync(x => x.Id.Equals(castItem.Person.Id));
                                if (foundCast is null)
                                {
                                    dbShow.Cast.Add(new DbCast { Id = castItem.Person.Id, Name = castItem.Person.Name, Birthday = castItem.Person.Birthday });
                                }
                                else
                                {
                                    if (dbShow.Cast.FirstOrDefault(x => x.Id.Equals(castItem.Person.Id)) is null)
                                    {
                                        dbShow.Cast.Add(foundCast);
                                    }
                                }
                            }
                            await dbContext.AddAsync(dbShow);
                            await dbContext.SaveChangesAsync();
                            Console.WriteLine("done");
                        }
                    }
                    return await Task.FromResult(false);
                });
                pageNumber++;
            }
        }
    }
}
