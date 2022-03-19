/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace UACloudLibClientLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
                    expression += clause.Expression;
                }
                else
                {
                    expression += clause.Expression;
                    expression += "connector: \"or\"";
                }
                expression += "}";
            }

            expression += "]}";
            return expression;
        }
    }
}
