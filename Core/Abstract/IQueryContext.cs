using Core.Enum;

namespace Core.Abstract
{
    public interface IQueryContext
    {
        QueryMode Mode { get; set; }
    }

    public class QueryContext : IQueryContext
    {
        public QueryMode Mode { get; set; } = QueryMode.Public; // default frontend
    }
}
