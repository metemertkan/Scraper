using Microsoft.AspNetCore.Mvc;
using Shared.Db;
using WebApi.Pagination;
using WebApi.Repository;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ShowsController : ControllerBase
    {
        private readonly IBaseRepository _baseRepository;
        public ShowsController(IBaseRepository baseRepository)
        {
            _baseRepository = baseRepository;
        }

        [HttpGet(Name = "GetShows")]
        public async Task<PagedResult<DbShow>> Get(int pageNumber, int pageSize)
        {
            var pagedModel = new PagedModel
            {
                Page = pageNumber,
                PageSize = pageSize
            };

            var foundShows = await _baseRepository.GetAll(pagedModel);

            foreach (var foundShow in foundShows.Results) 
            {
                foundShow.Cast = foundShow.Cast.OrderByDescending(x => x.Birthday).ToList();
            }

            return foundShows;
        }

        [HttpGet(Name = "GetShowsWithCache")]
        public async Task<PagedResult<DbShow>> GetWithCache(int pageNumber, int pageSize)
        {
            var pagedModel = new PagedModel
            {
                Page = pageNumber,
                PageSize = pageSize
            };

            var foundShows = await _baseRepository.GetAllWithCache(pagedModel);

            foreach (var foundShow in foundShows.Results)
            {
                foundShow.Cast = foundShow.Cast.OrderByDescending(x => x.Birthday).ToList();
            }

            return foundShows;
        }
    }
}