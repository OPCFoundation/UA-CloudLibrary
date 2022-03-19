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
    
    public enum AddressSpaceSearchField
    {
        addressspaceid,
        title,
        version,
        contributorId,
        contributorName,
        categoryId,
        categoryName,
        license,
        lastModified,
        creationTimeStamp
    }
    /// <summary>
    /// Defining the attributes the AddressSpace must have
    /// </summary>
    public class AddressSpaceWhereExpression : IWhereExpression<AddressSpaceSearchField>
    {
        public string Expression { get; private set; }
        public string Value { get; private set; }

        public AddressSpaceWhereExpression()
        {

        }

        public AddressSpaceWhereExpression(AddressSpaceSearchField path, string value, ComparisonType comparison = 0)
        {
            Value = value;
            if (SetExpression(path, value, comparison))
            {
                // succeeded
            }
            else
            {
                throw new Exception("One or more arguments was incorrect");
            }
        }

        public bool SetExpression(AddressSpaceSearchField path, string value, ComparisonType comparison, bool AndConnector = true)
        {
            bool success = false;
            if (!string.IsNullOrEmpty(value) && Enum.IsDefined(path) && Enum.IsDefined(comparison))
            {
                if (comparison == ComparisonType.Like)
                {
                    value = InternalMethods.LikeComparisonCompatibleString(value);
                }

                string asString = "{";
                switch (path)
                {
                    case AddressSpaceSearchField.contributorName:
                        {
                            asString += $"path: \"name\", comparison: {comparison}, value: \"{value}\"";
                            break;
                        }
                    case AddressSpaceSearchField.categoryName:
                        {
                            asString += $"path: \"name\", comparison: {comparison}, value: \"{value}\"";
                            break;
                        }
                    default:
                        {
                            asString += $"path: \"{path}\", comparison: {comparison}, value: \"{value}\"";
                            break;
                        }
                }

                if (AndConnector)
                {
                    asString += ", connector: and";
                }
                else
                {
                    asString += ", connector: or";
                }

                asString += "}";
                Expression = asString;
                success = true;
            }
            else
            {
                success = false;
            }
            return success;
        }
    }
}
