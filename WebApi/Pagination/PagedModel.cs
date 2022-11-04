namespace WebApi.Pagination
{
    public class PagedModel
    {
        public int Page { get; set; }
        public int PageSize { get; set; }

        public PagedModel()
        {
            Page = 1;
            PageSize = 10;
        }
    }
}
