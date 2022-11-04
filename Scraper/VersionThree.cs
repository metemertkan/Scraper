﻿using Microsoft.EntityFrameworkCore;
using Polly;
using Shared.Db;
using Shared.Input;
using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace Scraper
{
    public class VersionThree
    {
        public async Task Execute()
        {
            using var httpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://api.tvmaze.com/")
            };

            var policy = Policy<bool>.Handle<HttpRequestException>(ex => ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests).WaitAndRetryForeverAsync(
                attempt => TimeSpan.FromMilliseconds(
                10 * Math.Pow(2, attempt)));


            var fallbackPolicyOnNotFound = Policy<bool>.Handle<HttpRequestException>(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound).FallbackAsync(async ct =>
            {
                Console.WriteLine("Got 404 stopping");
                return await Task.FromResult(true);
            });

            var policyWrap = fallbackPolicyOnNotFound.WrapAsync(policy);

            using var showsDbContext = new ShowDbContext();
            showsDbContext.Database.EnsureCreated();

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
                        Console.Write($"Show Id: {item.Id} ");

                        await policy.ExecuteAsync(async () => 
                        {
                            using var dbContext = new ShowDbContext();

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

                                try
                                {
                                    await dbContext.AddAsync(dbShow);
                                    await dbContext.SaveChangesAsync();
                                }
                                catch (DbUpdateException ex)
                                {
                                    if (!ex.InnerException.Message.Contains("Duplicate entry"))
                                    {
                                        Console.WriteLine("Same Id tried to insert");
                                        throw;
                                    }
                                    
                                }
                               
                                Console.WriteLine("done");
                              
                            }
                            return await Task.FromResult(false);
                        });

                    });
  

                    return await Task.FromResult(false);
                });
                pageNumber++;
            }

        }
    }
}
