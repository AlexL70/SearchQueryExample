using System.Collections.Generic;

namespace ClientCompanyName.Domain.Abstractions.ValueTypes
{
    public class PagedDataSource<T>
    {
        public long Total { get; set; }
        public IEnumerable<T> Data { get; set; }
    }
}
