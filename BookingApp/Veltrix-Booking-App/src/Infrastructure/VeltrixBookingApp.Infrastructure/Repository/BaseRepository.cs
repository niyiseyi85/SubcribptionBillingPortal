using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VeltrixBookingApp.Infrastructure.Persistence;
using VeltrixBookingApp.Domain.Repositories;

namespace VeltrixBookingApp.Infrastructure.Repository
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _entities;

        public BaseRepository(ApplicationDbContext context)
        {
            _context = context;
            _entities = _context.Set<T>();
        }

        public async Task DeleteAsync(T entity)
        {
            _context.Remove(entity);
            await Task.CompletedTask;
        }

        public async Task<List<T>> GetAll()
        {
            return await _entities.ToListAsync();
        }

        public async Task<List<T>> FindAll(Expression<Func<T, bool>> expression)
        {
            IQueryable<T> query = _context.Set<T>();
            return await query.Where(expression).ToListAsync();
        }

        public async Task<T?> GetByIdAsync(string id)
        {
            return await _entities.FindAsync(id).AsTask();
        }

        public IQueryable<T> QueryAll()
        {
            return _entities.AsQueryable();
        }

        public async Task AddAsync(T entity)
        {
            await _entities.AddAsync(entity);
        }

        public async Task UpDateAsync(T entity)
        {
            _entities.Update(entity);
            await Task.CompletedTask;
        }

        public async Task AddManyAsync(IEnumerable<T> entities)
        {
            await _entities.AddRangeAsync(entities);
        }
    }
}
