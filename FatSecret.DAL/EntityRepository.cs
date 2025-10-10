using FatSecret.DAL.Context;
using FatSecret.Service.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace FatSecret.DAL;

public class EntityRepository<T>(FatSecretDbContext dbContext, DbSet<T> entities) : IEntityRepository<T>
    where T : class
{
    private readonly DbContext _context = dbContext;
    private readonly DbSet<T> _entities = entities;

    public IQueryable<T> All()
    {
        return _entities;
    }

    public T FindById(string login)
    {
        return _entities.Find(login);
    }

    public void Add(T entity)
    {
        _entities.Add(entity);
    }

    public void Update(T entity)
    {
        _context.Entry(entity).State = EntityState.Modified;
    }

    public void Delete(T entity)
    {
        _entities.Remove(entity);
    }

    public void SaveChanges()
    {
        _context.SaveChanges();
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}