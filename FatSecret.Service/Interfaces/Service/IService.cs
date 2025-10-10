namespace FatSecret.Service.Interfaces.Service;

public interface IService<TIn, TOut> where TIn : class where TOut : class
{
    Task<TOut> Execute(TIn request = null);
}

public interface IService<TOut> where TOut : class
{
    Task<TOut> Execute();
}