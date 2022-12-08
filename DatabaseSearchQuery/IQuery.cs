namespace ClientCompanyName.Domain.Abstractions.Queries
{
    public interface IQuery<out TResult>
    {
        TResult Execute();
    }
}