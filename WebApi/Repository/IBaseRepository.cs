using WebApi.Pagination;
using Shared.Db;

namespace WebApi.Repository
{
    public interface IBaseRepository
    {
        Task<PagedResult<DbShow>> GetAll(PagedModel pagedModel);
        Task<PagedResult<DbShow>> GetAllWithCache(PagedModel pagedModel);
    }
}
