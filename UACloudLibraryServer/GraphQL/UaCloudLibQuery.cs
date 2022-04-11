/* Copyright (c) 1996-2022 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation Corporate Members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
*/

namespace UACloudLibrary
{
    using GraphQL.Types;

    public class UaCloudLibQuery : ObjectGraphType
    {
        public UaCloudLibQuery(UaCloudLibRepo cloudLibRepo)
        {
            Name = "UACloudLibraryQuery";

            Field<ListGraphType<DatatypeType>>(
                "datatype",
                resolve: context => cloudLibRepo.GetDataTypes()
            );

            Field<ListGraphType<MetadataType>>(
                "metadata",
                resolve: context => cloudLibRepo.GetMetaData()
            );

            Field<ListGraphType<ObjecttypeType>>(
                "objecttype",
                resolve: context => cloudLibRepo.GetObjectTypes()
            );

            Field<ListGraphType<ReferencetypeType>>(
                "referencetype",
                resolve: context => cloudLibRepo.GetReferenceTypes()
            );

            Field<ListGraphType<VariabletypeType>>(
                "variabletype",
                resolve: context => cloudLibRepo.GetVariableTypes()
            );
        }
    }
}
