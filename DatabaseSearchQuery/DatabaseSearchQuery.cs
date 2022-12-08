using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using ClientCompanyName.Domain.Abstractions.DataModels;
using ClientCompanyName.Domain.Abstractions.Repositories;
using ClientCompanyName.Domain.Abstractions.ValueTypes;
using System.Data.Entity;

namespace ClientCompanyName.Domain.Abstractions.Queries
{
    public class DatabaseSearchQuery<TDataModel, TDto, TPrimaryKey> : 
        IQuery<PagedDataSource<TDto>> 
            where TDataModel : class, IDataModel<TPrimaryKey>
            where TDto: class
    {
        private readonly IMapper _mapper;

        protected readonly ISearchRepository<TDataModel> SearchRepository;
        protected readonly int Take;
        protected readonly int Skip;
        protected IEnumerable<Expression<Func<TDto, object>>> OrderKeysByDto;
        protected IEnumerable<Expression<Func<TDataModel, object>>> OrderKeysByDataModel;
        protected bool SortAscending;
        private readonly Expression<Func<TDataModel, bool>> _predicate;
        private readonly bool _skipCount;
        private readonly bool _postProjection;

        protected long Count { get; set; }

        public DatabaseSearchQuery(DbSearchQueryInput<TDataModel, TDto> parameters, IMapper mapper)
        {
            _mapper = mapper;
            SearchRepository = parameters.SearchRepository;
            _skipCount = parameters.SkipCount;
            Take = parameters.Take;
            Skip = parameters.Skip;
            if (parameters.OrderByDataModelSelectors != null && parameters.OrderByDtoSelectors != null)
                throw new ApplicationException("Using two sets of OrderBy clauses in one query is not allowed.");
            if (parameters.OrderByDtoSelectors != null && parameters.PostProjection)
                throw new ApplicationException("PostProjection is disabled along with the ordering by TDto");
            OrderKeysByDto = parameters.OrderByDtoSelectors;
            OrderKeysByDataModel = parameters.OrderByDataModelSelectors;
            _postProjection = parameters.PostProjection;
            SortAscending = parameters.OrderAscending;
            _predicate = parameters.Predicate;
        }

        public virtual PagedDataSource<TDto> Execute()
        {
            return GetPagedDataSource();
        }

        public virtual async Task<PagedDataSource<TDto>> ExecuteAsync()
        {
            return await GetPagedDataSourceAsync();
        }

        private PagedDataSource<TDto> GetPagedDataSource()
        {
            IQueryable<TDataModel> queryable = SearchRepository.Where(_predicate);

            if (Count == 0)
            {
                Count = GetCount(queryable);
            }

            var pagedDataSource = new PagedDataSource<TDto>
            {
                Total = Count,
                Data = GetData(queryable)
            };

            return pagedDataSource;
        }

        private async Task<PagedDataSource<TDto>> GetPagedDataSourceAsync()
        {
            IQueryable<TDataModel> queryable = SearchRepository.Where(_predicate);

            if (Count == 0)
            {
                Count = await GetCountAsync(queryable);
            }

            var pagedDataSource = new PagedDataSource<TDto>
            {
                Total = Count,
                Data = await GetDataAsync(queryable)
            };

            return pagedDataSource;
        }

        protected virtual IEnumerable<TDto> GetData(IQueryable<TDataModel> filtered)
        {
            //  If ordering by DataModel fields is set, then apply it
            var orderedBefore = OrderKeysByDataModel == null
                ? filtered
                : AddOrderToQueryable(filtered, OrderKeysByDataModel);
            //  And then apply paging immediately
            if (OrderKeysByDataModel != null)
                orderedBefore = orderedBefore.Skip(Skip).Take(Take);

            IEnumerable<TDto> projection;
            //  Apply projection (mapping) of DataModel fields to DTO
            if (typeof(TDto) != typeof(TDataModel))
            {
                //  Projection is done in memory after data are fetched from DB
                if (_postProjection)
                {
                    projection = orderedBefore.ToList().Select(_ => _mapper.Map<TDto>(_));
                }
                //  Projection is done by SQL
                else
                    projection = orderedBefore.ProjectTo<TDto>(_mapper.ConfigurationProvider).ToList();
            }
            //  No projection needed
            else
                projection = orderedBefore.Cast<TDto>();

            //  If ordering by DTO fields is set, then apply it
            if (OrderKeysByDto == null)
                return OrderKeysByDataModel == null 
                    ? projection.Skip(Skip).Take(Take)
                    : projection;

            return AddOrderToEnumerable(projection, OrderKeysByDto.Select(x => x.Compile()))
                .Skip(Skip).Take(Take);
        }

        protected virtual async Task<IEnumerable<TDto>> GetDataAsync(IQueryable<TDataModel> filtered)
        {
            //  If ordering by DataModel fields is set, then apply it
            var orderedBefore = OrderKeysByDataModel == null
                ? filtered
                : AddOrderToQueryable(filtered, OrderKeysByDataModel);

            //  And then apply paging immediately
            if (OrderKeysByDataModel != null)
            {
                orderedBefore = orderedBefore.Skip(Skip).Take(Take);
            }

            IEnumerable<TDto> projection;
            //  Apply projection (mapping) of DataModel fields to DTO
            if (typeof(TDto) != typeof(TDataModel))
            {
                //  Projection is done in memory after data are fetched from DB
                if (_postProjection)
                {
                    projection = (await orderedBefore.ToListAsync()).Select(_ => _mapper.Map<TDto>(_));
                }
                //  Projection is done by SQL
                else
                {
                    projection = await orderedBefore.ProjectTo<TDto>(_mapper.ConfigurationProvider).ToListAsync();
                }
            }
            //  No projection needed
            else
            {
                projection = await orderedBefore.Cast<TDto>().ToListAsync();
            }

            //  If ordering by DTO fields is set, then apply it
            if (OrderKeysByDto == null)
            {
                return OrderKeysByDataModel == null
                    ? projection.Skip(Skip).Take(Take)
                    : projection;
            }

            return AddOrderToEnumerable(projection, OrderKeysByDto.Select(x => x.Compile()))
                .Skip(Skip).Take(Take);
        }
        protected IQueryable<T> AddOrderToQueryable<T>(IQueryable<T> query,
            IEnumerable<Expression<Func<T, object>>> orderKeys)
        {
            var ordered = query;

            var isFirst = true;
            foreach (var keyExpression in orderKeys)
            {
                if (isFirst)
                    ordered = SortAscending
                        ? ordered.OrderBy(keyExpression)
                        : ordered.OrderByDescending(keyExpression);
                else
                    ordered = SortAscending
                        ? ((IOrderedQueryable<T>) ordered).ThenBy(keyExpression)
                        : ((IOrderedQueryable<T>) ordered).ThenByDescending(keyExpression);
                isFirst = false;
            }

            return ordered;
        }


        protected IEnumerable<T> AddOrderToEnumerable<T>(IEnumerable<T> query,
            IEnumerable<Func<T, object>> orderKeys)
        {
            var ordered = query;

            var isFirst = true;
            foreach (var keyExpression in orderKeys)
            {
                if (isFirst)
                    ordered = SortAscending
                        ? ordered.OrderBy(keyExpression)
                        : ordered.OrderByDescending(keyExpression);
                else
                    ordered = SortAscending
                        ? ((IOrderedEnumerable<T>) ordered).ThenBy(keyExpression)
                        : ((IOrderedEnumerable<T>) ordered).ThenByDescending(keyExpression);
                isFirst = false;
            }

            return ordered;
        }
        
        protected virtual long GetCount(IQueryable<TDataModel> filtered)
        {
            return _skipCount ? 0 : SearchRepository.GetCount(_predicate);
        }

        protected virtual async Task<long> GetCountAsync(IQueryable<TDataModel> filtered)
        {
            return _skipCount ? 0 : await SearchRepository.GetCountAsync(_predicate);
        }
    }
}
