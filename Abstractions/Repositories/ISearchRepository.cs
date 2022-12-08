using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ClientCompanyName.Domain.Abstractions.Repositories
{
    public interface ISearchRepository<TDataModel> : IDisposable
    {
        long GetCount(Expression<Func<TDataModel, bool>> predicate);

        Task<long> GetCountAsync(Expression<Func<TDataModel, bool>> predicate);

        IQueryable<TDataModel> Where(Expression<Func<TDataModel, bool>> predicate);
    }
}
