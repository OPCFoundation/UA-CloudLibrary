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

namespace Opc.Ua.Cloud.Library.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>Enum to select the search field</summary>
    public enum SearchField
    {
        /// <summary>The name-field</summary>
        name,
        /// <summary>The value-field</summary>
        value,
        /// <summary>The orgname-field</summary>
        orgname,
        /// <summary>The orgContact-field</summary>
        orgContact,
        /// <summary>The nodesetTitle-field</summary>
        nodesetTitle,
        /// <summary>The nameSpaceName-field</summary>
        addressSpaceName,
        /// <summary>The nameSpaceDescription-field</summary>
        addressSpaceDescription,
        /// <summary>The lastModification-field</summary>
        lastModification,
        /// <summary>The creationTimeStamp-field</summary>
        creationTimeStamp,
        /// <summary>The description-field</summary>
        description
    }

    /// <summary>
    ///   Enum to define the search comperation
    /// </summary>
    public enum ComparisonType
    {
        /// <summary>equals comperation</summary>
        equals,
        /// <summary>contains comperation</summary>
        contains,
        /// <summary>like comperation</summary>
        like
    }

    /// <summary>Class to build a search-filter-expression</summary>
    public class WhereExpression
    {
        /// <summary>Gets the expression.</summary>
        /// <value>The expression.</value>
        public string Expression { get; private set; }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public string Value { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="WhereExpression" /> class.</summary>
        /// <param name="field">The field.</param>
        /// <param name="value">The value.</param>
        /// <param name="comparison">The comparison.</param>
        public WhereExpression(SearchField field, string value, ComparisonType comparison = 0)
        {
            SetExpression(field, value, comparison);
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
                return string.Empty;
            }
            else
            {
                query.Append("[");

                if (filter != null)
                {
                    foreach (WhereExpression e in filter)
                    {
                        query.Append(e.Expression);
                        query.Append(",");
                    }
                }

                query.Remove(query.Length - 1, 1);
                query.Append("]");

                return query.ToString();
            }
        }

        /// <summary>Sets the expression.</summary>
        /// <param name="field">The field.</param>
        /// <param name="value">The value.</param>
        /// <param name="comparison">The comparison.</param>
        public void SetExpression(SearchField field, string value, ComparisonType comparison)
        {
            if (!string.IsNullOrEmpty(value) && Enum.IsDefined(typeof(SearchField), field) && Enum.IsDefined(typeof(ComparisonType), comparison))
            {
                Expression = "{'" + field.ToString() + "': {'" + comparison.ToString() + "': '" + value + "'}}";

                Value = value;
            }
        }
    }
}
