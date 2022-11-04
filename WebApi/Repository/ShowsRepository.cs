using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Db;
using WebApi.Helper;
using WebApi.Model;
using WebApi.Pagination;

namespace WebApi.Repository
{
    public class ShowsRepository : IBaseRepository
    {
        private ShowsDbContext _showsDbContext;
        private IDistributedCache _cache;
        public ShowsRepository(ShowsDbContext showsDbContext, IDistributedCache cache)
        {
            _showsDbContext = showsDbContext;
            _cache = cache;
        }

        public async Task<PagedResult<DbShow>> GetAll(PagedModel pagedModel)
        {
            var result = new PagedResult<DbShow>
            {
                CurrentPage = pagedModel.Page,
                PageSize = pagedModel.PageSize,
                RowCount = _showsDbContext.Shows.Count()
            };

            var pageCount = (double)result.RowCount / pagedModel.PageSize;
            result.PageCount = (int)Math.Ceiling(pageCount);

            var skip = (pagedModel.Page - 1) * pagedModel.PageSize;
            result.Results = await _showsDbContext.Shows.Skip(skip).Take(pagedModel.PageSize).Include(x => x.Cast).ToListAsync();

            return result;

        }

        public async Task<PagedResult<DbShow>> GetAllWithCache(PagedModel pagedModel)
        {

            var result = new PagedResult<DbShow>
            {
                CurrentPage = pagedModel.Page,
                PageSize = pagedModel.PageSize,
                RowCount = _showsDbContext.Shows.Count()
            };

            var pageCount = (double)result.RowCount / pagedModel.PageSize;
            result.PageCount = (int)Math.Ceiling(pageCount);


            var paginationRecordKey = $"p{pagedModel.Page}s{pagedModel.PageSize}";
            var paginationCache = await _cache.GetRecordAsync<List<int>>(paginationRecordKey);
            if (paginationCache is not null)
            {
                foreach (var item in paginationCache)
                {
                    var cachedShow = await _cache.GetRecordAsync<DbShow>($"s{item}");
                    if (cachedShow is not null)
                    {
                        result.Results.Add(cachedShow);
                    }
                    else
                    {
                        var foundDbShow = await _showsDbContext.Shows.FirstOrDefaultAsync(x => x.Id ==item);
                        if (foundDbShow is not null) 
                        {
                            result.Results.Add(foundDbShow);
                            await _cache.SetRecordAsync($"s{item}",foundDbShow);
                        }
                    }
                }
            }
            else
            {
                var dbResult = await GetAll(pagedModel);
                result = dbResult;
                var paginationRecordValue = new List<int>();
                foreach (var item in dbResult.Results)
                {
                    paginationRecordValue.Add(item.Id);
                    await _cache.SetRecordAsync($"s{item.Id}", item);
                }

                await _cache.SetRecordAsync(paginationRecordKey, paginationRecordValue);

            }

            return result;
        }
    }
}
