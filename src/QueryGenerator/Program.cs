using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Newtonsoft.Json;
using System.Dynamic;
using System.Text.RegularExpressions;

namespace QueryGenerator
{
    public class Thing
    {
        public int entities { get; internal set; }
        public int id { get; set; }
    }

    public class Program
    {
        public void Main(string[] args)
        {
            var things = new[] { new Thing { id = 3 , entities = 11},
            new Thing { id = 4 , entities = 10}};

            var result = things.Where(BuildQuery<Thing>());

            foreach (var thing in result)
            {
                Console.WriteLine(JsonConvert.SerializeObject(thing));
            }

            Console.Read();
        }

        private readonly Regex termRegex = new Regex(@"(\w+)(=|>=|<=|<|>)(\d+)");
        private IEnumerable<dynamic> GetQueryTerms(string queryString)
        {

            foreach (var termString in queryString.Split(' '))
            {
                var matches = termRegex.Match(termString);
                yield return new
                {
                    propertyName = matches.Groups[1].Value,
                    Comparison = matches.Groups[2].Value,
                    constantValue = matches.Groups[3].Value
                };
            }
        }
        public Func<T, bool> BuildQuery<T>()
        {
            var parameter = Expression.Parameter(typeof(T), "t");

            var queryTerms = new[] {
                new { propertyName = "id", Comparison = "=", constantValue = 3 } ,
                new { propertyName = "entities", Comparison = ">=", constantValue = 10 } };

            var foo = GetQueryTerms("id=3 alerts>=5 entities<10").ToList();

            var operatorToExpressionMap = new Dictionary<string, Func<MemberExpression, ConstantExpression, BinaryExpression>>() {
                {"=", Expression.Equal },
                {">", Expression.GreaterThan },
                { ">=", Expression.GreaterThanOrEqual },
                { "<=", Expression.LessThanOrEqual },
                { "<", Expression.LessThan }};

            var firstQueryTerm = queryTerms.First();

            var comparisonExpression = queryTerms.Skip(1)
                .Aggregate(GetComparisonExpression(parameter, firstQueryTerm.propertyName, (int)(firstQueryTerm.constantValue), operatorToExpressionMap[firstQueryTerm.Comparison]),
                (antecendent, term) => Expression.And(antecendent, GetComparisonExpression(parameter, term.propertyName, term.constantValue, operatorToExpressionMap[term.Comparison])));//Expression.GreaterThanOrEqual(propertyExpression, constantExpression);

            var lambda = Expression.Lambda<Func<T, bool>>(comparisonExpression, parameter).Compile();

            return lambda;
        }

        private static BinaryExpression GetComparisonExpression(ParameterExpression parameter, string propertyName, object constant, Func<MemberExpression, ConstantExpression, BinaryExpression> getComparisonExpressionFunc)
        {
            MemberExpression propertyExpression = GetPropertyExpression(parameter, propertyName);
            ConstantExpression constantExpression = GetConstantExpression(constant);

            return getComparisonExpressionFunc(propertyExpression, constantExpression);
        }

        private static ConstantExpression GetConstantExpression(object constantValue)
        {
            var constantExpression = Expression.Constant(constantValue, typeof(int));
            return constantExpression;
        }

        private static MemberExpression GetPropertyExpression(ParameterExpression parameter, string propertyName)
        {
            var propertyExpression = Expression.Property(parameter, propertyName);
            return propertyExpression;
        }
    }
}
