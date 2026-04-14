using CargoTransport.Desktop.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CargoTransport.Desktop.Repositories;

public interface IRepositoryBase<T> where T : class
{
    IQueryable<T> FindAll(bool trackChanges);
    IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges);
    void Create(T entity);
    void Update(T entity);
    void Delete(T entity);
    void AddRange(IEnumerable<T> entities);
}

public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class
{
    protected RepositoryBase(CargoTransportDbContext context)
    {
        Context = context;
    }

    protected CargoTransportDbContext Context { get; }

    public IQueryable<T> FindAll(bool trackChanges) =>
        trackChanges
            ? Context.Set<T>()
            : Context.Set<T>().AsNoTracking();

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges) =>
        trackChanges
            ? Context.Set<T>().Where(expression)
            : Context.Set<T>().Where(expression).AsNoTracking();

    public void Create(T entity) => Context.Set<T>().Add(entity);

    public void Update(T entity) => Context.Set<T>().Update(entity);

    public void Delete(T entity) => Context.Set<T>().Remove(entity);

    public void AddRange(IEnumerable<T> entities) => Context.Set<T>().AddRange(entities);
}
