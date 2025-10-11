namespace FatSecret.DAL.Interfaces;

public interface IEntityRepository<T> where T : class
{
    IQueryable<T> All();
    T FindById(string login);
    void Add(T entity);
    void Update(T entity);
    void Delete(T entity);
    void SaveChanges();
    Task<int> SaveChangesAsync();
}