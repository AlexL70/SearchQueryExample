namespace ClientCompanyName.Domain.Abstractions
{
    /// <summary>
    /// An abstraction of a unique object.
    /// </summary>
    /// <typeparam name="TKey">The type of the object Id property.</typeparam>
    public interface IAmUnique<TKey>
    {
        TKey Id { get; set; }
    }
}