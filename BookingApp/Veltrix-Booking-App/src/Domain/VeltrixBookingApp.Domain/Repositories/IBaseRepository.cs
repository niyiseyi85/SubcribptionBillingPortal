using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace VeltrixBookingApp.Domain.Repositories
{
    public interface IBaseRepository<T> where T : class
    {
        IQueryable<T> QueryAll();
        Task AddAsync(T entity);
        Task AddManyAsync(IEnumerable<T> entities);
        Task<List<T>> FindAll(Expression<Func<T, bool>> expression);
        Task<List<T>> GetAll();
        Task<T?> GetByIdAsync(string id);
        Task DeleteAsync(T entity);
        Task UpDateAsync(T entity);
    }
}
