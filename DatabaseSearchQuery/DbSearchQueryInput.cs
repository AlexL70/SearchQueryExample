using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using ClientCompanyName.Domain.Abstractions.Repositories;

namespace ClientCompanyName.Domain.Abstractions.Queries
{
    public class DbSearchQueryInput<TDataModel, TDto>
    {
        public ISearchRepository<TDataModel> SearchRepository { get; set; }
        public Expression<Func<TDataModel, bool>> Predicate { get; set; }
        public bool SkipCount { get; set; } = false;
        public int Skip { get; set; }
        public int Take { get; set; }
        /// <summary>
        /// Collection of OrderBy selectors based on DTO (final set of fields, after mapping)
        /// </summary>
        public IEnumerable<Expression<Func<TDto, object>>> OrderByDtoSelectors { get; set; } = null;
        /// <summary>
        /// Collection of OrderBy selectors based on DataModel (initial DB object's fields)
        /// </summary>
        public IEnumerable<Expression<Func<TDataModel, object>>> OrderByDataModelSelectors { get; set; } = null;
        public bool OrderAscending { get; set; } = true;
        /// <summary>
        /// If this parameter is true, then the projection is done by C#, not by SQL,
        /// i.e. a sequence of TDataModel is fetched from DB first, and then it is
        /// projected to the sequence of TDto. PostProjection is possible if either
        /// ordering is defined by TDataModel or no order at all.
        /// </summary>
        public bool PostProjection { get; set; } = false;
    }
}