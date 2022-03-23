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
    using System.Text;

    public enum SearchField
    {
        metadata_name,
        metadata_value,
        objecttype_name,
        objecttype_value,
        iD,
        name,
        lastModification,
        creationTimeStamp,
        description
    }

    public enum ComparisonType
    {
        Equal,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith,
        Like
    }

    public class WhereExpression
    {
        public string Expression { get; private set; }

        public string Value { get; private set; }

        public WhereExpression(SearchField path, string value, ComparisonType comparison = 0)
        {
            Value = value;

            if (!SetExpression(path, value, comparison))
            {
                throw new Exception("One or more arguments was incorrect");
            }
        }

        /// <summary>
        /// Checks if a clause is available and finalizes the statement
        /// </summary>
        /// <returns>Returns an empty string when no clause was transfered, otherwise the finalized where statement</returns>
        public static string Build(IEnumerable<WhereExpression> filter)
        {
            StringBuilder query = new StringBuilder();

            if (!filter.Any())
            {
                return "";
            }
            else
            {
                query.Append("[");

                if (filter != null)
                {
                    query.Append(string.Format(",", filter));
                }

                query.Append("]");

                return query.ToString();
            }
        }

        public bool SetExpression(SearchField path, string value, ComparisonType comparison, bool AndConnector = true)
        {
            if (string.IsNullOrEmpty(value) && Enum.IsDefined(path) && Enum.IsDefined(comparison))
            {
                if (comparison == ComparisonType.Like)
                {
                    value = LikeComparisonCompatibleString(value);
                }

                if (AndConnector)
                {
                    Expression = string.Format("{path: \"{0}\", comparison: {1}, value: \"{2}\", connector: and}", path, comparison, value);
                }
                else
                {
                    Expression = string.Format("{path: \"{0}\", comparison: {1}, value: \"{2}\", connector: or}", path, comparison, value);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private string LikeComparisonCompatibleString(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (value.StartsWith("%") && value.EndsWith("%"))
                {
                    return value;
                }
                else
                {
                    return string.Format("%" + value + "%");
                }
            }
            else
            {
                throw new ArgumentNullException("Parameter 'value' is null!");
            }
        }
    }
}
