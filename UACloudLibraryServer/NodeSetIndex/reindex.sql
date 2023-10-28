-- Deletes the nodeset index. Index will be re-created on next upload (even a failed upload)
DELETE FROM "ReferenceTypes";
DELETE FROM "RequiredModelInfo";
DELETE FROM "Nodes_OtherReferencedNodes";
DELETE FROM "Nodes_OtherReferencingNodes";
--DELETE FROM "ChildAndReference";
DELETE FROM "Methods";
DELETE FROM "DataVariables";
DELETE FROM "Properties";
DELETE FROM "DataTypes";
DELETE FROM "Objects";
DELETE FROM "Interfaces";
DELETE FROM "ObjectTypes";
DELETE FROM "Variables";
DELETE FROM "VariableTypes";
DELETE FROM "BaseTypes";

DELETE FROM "Nodes";
DELETE FROM "Nodes_Description";
DELETE FROM "Nodes_DisplayName";
DELETE FROM "StructureField";
DELETE FROM "StructureField_Description";
DELETE FROM "UaEnumField";
DELETE FROM "UaEnumField_Description";
DELETE FROM "UaEnumField_DisplayName";

DELETE FROM "NodeSets";
