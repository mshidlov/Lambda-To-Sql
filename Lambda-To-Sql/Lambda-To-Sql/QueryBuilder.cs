using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Lambda_To_Sql
{
    public class QueryBuilder<T> where T : class
    {
        public QueryBuilder()
        {
            _where = null;
            _groupBy = new List<Expression<Func<T, object>>>();
            _orderBy = new List<Expression<Func<T, object>>>();
            _projection = new List<Expression<Func<T, object>>>();
            _sum = new List<Expression<Func<T, object>>>();
            _count = new List<Expression<Func<T, object>>>();
        }

        private Expression<Func<T, bool>> _where { get; set; }
        private List<Expression<Func<T, object>>> _groupBy { get; set; }
        private List<Expression<Func<T, object>>> _orderBy { get; set; }
        private List<Expression<Func<T, object>>> _projection { get; set; }
        private List<Expression<Func<T, object>>> _sum { get; set; }
        private List<Expression<Func<T, object>>> _count { get; set; }
        
        private int _limit { get; set; }
        private int _offset { get; set; }


        public string Select()
        {
            return string.Format("SELECT {0}{1}{2} FROM {3} WHERE {4} {5} {6} {7} {8}", Projection(), Sum(), Count(),
                typeof (T).Name, Where(), GroupBy(), OrderBy(), Offset(), Limit());
        }

        public QueryBuilder<T> Where(Expression<Func<T, bool>> expression)
        {
            _where = _where == null
                ? expression
                : Expression.Lambda<Func<T, bool>>(
                    Expression.AndAlso(((BinaryExpression) _where.Body), (BinaryExpression) expression.Body),
                    _where.Parameters);
            return this;
        }

        public string Where()
        {
            var where = ConvertExpressionToString(_where.Body);
            return string.IsNullOrEmpty(where) ? string.Empty : string.Format("WHERE {0}", where);
        }

        public QueryBuilder<T> GroupBy(params Expression<Func<T, object>>[] expression)
        {
            _groupBy.AddRange(expression);
            return this;
        }

        public string GroupBy()
        {
            var groupBy = _groupBy.Select(e => ((MemberExpression) e.Body).Member.Name).ToList();
            return groupBy.Any() ? string.Format("GROUP BY {0}", string.Join(",", groupBy)) : string.Empty;
        }

        public QueryBuilder<T> OrderBy(params Expression<Func<T, object>>[] expression)
        {
            _orderBy.AddRange(expression);
            return this;
        }

        public string OrderBy()
        {
            var orderBy = _orderBy.Select(e => ((MemberExpression) e.Body).Member.Name).ToList();
            return orderBy.Any() ? string.Format("ORDER BY {0}", string.Join(",", orderBy)) : string.Empty;
        }


        public QueryBuilder<T> Limit(int limit)
        {
            _limit = limit;
            return this;
        }

        public string Limit()
        {
            return string.Format("LIMIT {0}", _limit);
        }

        public QueryBuilder<T> Offset(int offset)
        {
            _offset = offset;
            return this;
        }

        public string Offset()
        {
            return string.Format("OFFSET {0}", _offset);
        }


        public QueryBuilder<T> Sum(params Expression<Func<T, object>>[] expression)
        {
            _sum.AddRange(expression);
            return this;
        }
        public string Sum()
        {
            return string.Join(string.Empty, _sum.Select(x => string.Format(",SUM({0}) AS {0}", x)));
        }
        public QueryBuilder<T> Count(params Expression<Func<T, object>>[] expression)
        {
            _count.AddRange(expression);
            return this;
        }
        public string Count()
        {
            return string.Join(string.Empty, _count.Select(x => string.Format(",COUNT({0}) AS {0}", x)));
        }

        public List<string> Projections()
        {
            var custum =
                _projection.Select(e => ((MemberExpression)e.Body).Member.Name).ToList();
            var groupBy = _groupBy.Select(e => ((MemberExpression)e.Body).Member.Name).ToList();
            var properties = groupBy.Any() ? groupBy : Properties();
            if (custum.Any())
            {
                custum = custum.Intersect(properties).ToList();
            }
            return custum.Any() ? custum : properties;
        }

        public string Projection()
        {
            return string.Join(",", Projections());
        }

        private List<string> Properties()
        {
            var properties = typeof(T).GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(CustomColumn)));
            return properties.Select(prop => prop.Name).ToList();
        }

        private static string ConvertExpressionToString(Expression body)
        {
            if (body == null)
            {
                return string.Empty;
            }
            if (body is ConstantExpression)
            {
                return ValueToString(((ConstantExpression)body).Value);
            }
            if (body is MemberExpression)
            {
                var member = ((MemberExpression)body);
                if (member.Member.MemberType == MemberTypes.Property)
                {
                    return member.Member.Name;
                }
                var value = GetValueOfMemberExpression(member);
                if (value is IEnumerable)
                {
                    var sb = new StringBuilder();
                    foreach (var item in value as IEnumerable)
                    {
                        sb.AppendFormat("{0},", ValueToString(item));
                    }
                    return sb.Remove(sb.Length - 1, 1).ToString();
                }
                return ValueToString(value);
            }
            if (body is UnaryExpression)
            {
                return ConvertExpressionToString(((UnaryExpression)body).Operand);
            }
            if (body is BinaryExpression)
            {
                var binary = body as BinaryExpression;
                return string.Format("({0}{1}{2})", ConvertExpressionToString(binary.Left),
                    ConvertExpressionTypeToString(binary.NodeType),
                    ConvertExpressionToString(binary.Right));
            }
            if (body is MethodCallExpression)
            {
                var method = body as MethodCallExpression;
                return string.Format("({0} IN ({1}))", ConvertExpressionToString(method.Arguments[0]),
                    ConvertExpressionToString(method.Object));
            }
            if (body is LambdaExpression)
            {
                return ConvertExpressionToString(((LambdaExpression)body).Body);
            }
            return "";
        }

        private static string ValueToString(object value)
        {
            if (value is string)
            {
                return string.Format("'{0}'", value);
            }
            if (value is DateTime)
            {
                return string.Format("'{0:yyyy-MM-dd HH:mm:ss}'", value);
            }
            return value.ToString();
        }

        private static object GetValueOfMemberExpression(MemberExpression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        private static string ConvertExpressionTypeToString(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.And:
                    return " AND ";
                case ExpressionType.AndAlso:
                    return " AND ";
                case ExpressionType.Or:
                    return " OR ";
                case ExpressionType.OrElse:
                    return " OR ";
                case ExpressionType.Not:
                    return "NOT";
                case ExpressionType.NotEqual:
                    return "!=";
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                default:
                    return "";
            }
        }
    }

    
}
