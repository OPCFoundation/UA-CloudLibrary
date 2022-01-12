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

namespace UACloudLibrary
{
    using GraphQL.Types;
    using GraphQL.EntityFramework;

    public class UaCloudLibQuery : QueryGraphType<AppDbContext>
    {
        public UaCloudLibQuery(IEfGraphQLService<AppDbContext> efGraphQLService) : base(efGraphQLService)
        {
            //AddSingleField(name: "datatype", resolve: context => context.DbContext.datatype);
            AddQueryConnectionField(name: "datatypes", resolve: context => context.DbContext.datatype);
            AddQueryConnectionField(name: "metadata", resolve: context => context.DbContext.metadata);
            AddQueryConnectionField(name: "objecttype", resolve: context => context.DbContext.objecttype);
            AddQueryConnectionField(name: "referencetype", resolve: context => context.DbContext.referencetype);
            AddQueryConnectionField(name: "variabletype", resolve: context => context.DbContext.variabletype);
            Name = "query";
        }

        //public UaCloudLibQuery(UaCloudLibRepo cloudLibRepo)
        //{
        //    Name = "UACloudLibraryQuery";

        //    Field<ListGraphType<DatatypeType>>(
        //        "datatype",
        //        resolve: context => cloudLibRepo.GetDataTypes()
        //    );

        //    Field<ListGraphType<MetadataType>>(
        //        "metadata",
        //        resolve: context => cloudLibRepo.GetMetaData()
        //    );

        //    Field<ListGraphType<ObjecttypeType>>(
        //        "objecttype",
        //        resolve: context => cloudLibRepo.GetObjectTypes()
        //    );

        //    Field<ListGraphType<ReferencetypeType>>(
        //        "referencetype",
        //        resolve: context => cloudLibRepo.GetReferenceTypes()
        //    );

        //    Field<ListGraphType<VariabletypeType>>(
        //        "variabletype",
        //        resolve: context => cloudLibRepo.GetVariableTypes()
        //    );
        //}
    }
}
