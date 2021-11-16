using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace UACloudLibClientLibrary.WhereExpressions
{
    /// <summary>
    /// Used if an attribute can have multiple possibilities that are accepted
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GroupedOrExpression<T> where T : Enum
    {
        public List<IWhereExpression<T>> Expressions
        {
            get; set;
        }

        public GroupedOrExpression(params IWhereExpression<T>[] clauses)
        {
            foreach (IWhereExpression<T> clause in clauses)
            {
                Expressions.Add(clause);
            }
        }

        /// <summary>
        /// Creates the string that is used for the where expression in the query
        /// </summary>
        /// <returns></returns>
        public string GetGroupedExpression()
        {
            IWhereExpression<T> last = Expressions.Last();
            string expression = "{groupedExpressions: [";

            foreach (IWhereExpression<T> clause in Expressions)
            {
                expression += "{";
                if (clause.Equals(last))
                {
                    expression += clause.GetExpression();
                }
                else
                {
                    expression += clause.GetExpression();
                    expression += "connector: \"or\"";
                }
                expression += "}";
            }

            expression += "]}";
            return expression;
        }
    }
}
