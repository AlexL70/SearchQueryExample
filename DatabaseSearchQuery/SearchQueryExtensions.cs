using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ClientCompanyName.Domain.Abstractions.Queries
{
    public static class SearchQueryExtensions
    {
        private static string ToPascalCase(string str)
        {
            return $"{str.Substring(0, 1).ToUpper()}{str.Substring(1)}";
        }

        /// <summary>
        /// Method forms list of lambda-expressions like x => x.SomeProperty, where "SomeProperty" is a name of a property of
        /// an arbitrary class <typeparam name="TDto"></typeparam>. Used to pass as a parameter to OrderBy and OrderByDescending methods.
        /// </summary>
        /// <param name="sortProperties">Array of names of the properties to order by. If this parameter contains at least one "." symbol,
        /// then it is treated as a chain of property accessors</param>
        public static IEnumerable<Expression<Func<TDto, object>>> GetOrderSelectors<TDto>(params string[] sortProperties)
        {
            if (sortProperties is null)
            {
                return null;
            }

            var result = new List<Expression<Func<TDto, object>>>();

            foreach (var property in sortProperties)
            {
                if (string.IsNullOrWhiteSpace(property))
                {
                    continue;
                }

                var (expProp, param) = GetPropExpression<TDto>(property);

                if (expProp == null || param == null)
                {
                    continue;
                }

                result.Add(Expression.Lambda<Func<TDto, object>>(expProp, param));
            }


            return result.Count > 0 ? result : null;
        }

        public static IEnumerable<Expression<Func<TDto, object>>> GetOneOrderSelector<TDto>(string sortProperty,
            IEnumerable<string> excluded = null)
        {
            if (excluded != null && excluded.Contains(sortProperty)
                || string.IsNullOrWhiteSpace(sortProperty))
            {
                return null;
            }

            return GetOrderSelectors<TDto>(sortProperty);
        }

        /// <summary>
        /// Used to form a chain of member access expressions like x.Property.SubProperty.SubSubProperty
        /// </summary>
        /// <typeparam name="TStarting">Type to start member access chain from.</typeparam>
        /// <param name="sortProperty">Property name or a chain of property names divided by "." (dot symbol)</param>
        /// <returns>A Tuple of MemberExpression and ParameterExpression</returns>
        private static (Expression exp, ParameterExpression param) GetPropExpression<TStarting>(string sortProperty)
        {
            Expression expProp = null;
            ParameterExpression parameter = null;
            var propStrings = sortProperty.Split('.');
            var type = typeof(TStarting);
            foreach (string propName in propStrings)
            {
                var prop = type.GetProperty(ToPascalCase(propName));
                if (prop == null)
                {
                    return (null, null);
                }

                Expression param;
                if (expProp == null)
                {
                    parameter = Expression.Parameter(type);
                    param = parameter;
                }
                else
                {
                    param = expProp;
                }
                expProp = Expression.Property(param, prop);
                if (expProp.Type.IsValueType)
                {
                    expProp = Expression.Convert(expProp, typeof(object));
                }
                type = prop.PropertyType;
            }

            return (expProp, parameter);
        }
    }
}