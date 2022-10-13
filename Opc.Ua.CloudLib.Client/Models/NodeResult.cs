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

    /// <summary>
    /// Captures a GraphQL node and it's cursor (GraphQL edge)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GraphQlNodeAndCursor<T>
    {
        /// <summary>
        /// Cursor of this NodeSet: can be used to retrieve previous of next page
        /// </summary>
        public string Cursor { get; set; }
        /// <summary>
        /// Data for the node
        /// </summary>
        public T Node { get; set; }
    }

    /// <summary>
    /// Captures pagination related information in a GraphQl response
    /// </summary>
    public class GraphQlPaginationInfo
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public GraphQlPaginationInfo() { }
        /// <summary>
        /// Copy Constructor 
        /// </summary>
        /// <param name="paginationInfo"></param>
        public GraphQlPaginationInfo(GraphQlPaginationInfo paginationInfo)
        {
            TotalCount = paginationInfo.TotalCount;
            PageInfo = paginationInfo.PageInfo;
        }

        /// <summary>
        /// The total number of nodesets matching the query (ignoring pagination)
        /// </summary>
        public int TotalCount { get; set; }
        /// <summary>
        /// Information about the returned page
        /// </summary>
        public GraphQlPageInfo PageInfo { get; set; }
    }
    /// <summary>
    /// Information about the returned page
    /// </summary>
    public class GraphQlPageInfo
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public GraphQlPageInfo() { }
        /// <summary>
        /// Copy Constructor
        /// </summary>
        /// <param name="pageInfo"></param>
        public GraphQlPageInfo(GraphQlPageInfo pageInfo)
        {
            HasNextPage = pageInfo.HasNextPage;
            HasPreviousPage = pageInfo.HasPreviousPage;
            EndCursor = pageInfo.EndCursor;
            StartCursor = pageInfo.StartCursor;
        }

        /// <summary>
        /// Indicates if more pages are available (due to pagination)
        /// </summary>
        public bool HasNextPage { get; set; }
        /// <summary>
        /// Indicates if more pages are available (due to pagination)
        /// </summary>
        public bool HasPreviousPage { get; set; }
        /// <summary>
        /// Cursor of the last node returned. Can be used to retrieve the next page.
        /// </summary>
        public string EndCursor { get; set; }
        /// <summary>
        /// Cursor of the first node returned. Can be used to retrieve the previous page.
        /// </summary>
        public string StartCursor { get; set; }
    }

    /// <summary>
    /// Result including pagination information
    /// </summary>
    public class GraphQlResult<T> : GraphQlPaginationInfo
    {
        /// <summary>
        /// Constructore
        /// </summary>
        public GraphQlResult() 
        {
        }
        /// <summary>
        /// Copy constructor, except for the Edges
        /// </summary>
        /// <param name="paginationInfo"></param>
        public GraphQlResult(GraphQlPaginationInfo paginationInfo): base(paginationInfo)
        {
        }

        /// <summary>
        /// Node and Cursor combinations (GraphQl Edges)
        /// </summary>
        public List<GraphQlNodeAndCursor<T>> Edges { get; set; } = new List<GraphQlNodeAndCursor<T>>();
    }
}
