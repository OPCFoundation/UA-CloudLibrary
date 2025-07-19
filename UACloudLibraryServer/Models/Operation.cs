
namespace AdminShell
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class Operation : SubmodelElement
    {
        [DataMember(Name = "inoutputVariables")]
        [XmlArray(ElementName = "inoutputVariables")]
        public List<OperationVariable> InoutputVariables { get; set; } = new();

        [DataMember(Name = "inputVariables")]
        [XmlArray(ElementName = "inputVariables")]
        public List<OperationVariable> InputVariables { get; set; } = new();

        [DataMember(Name = "outputVariables")]
        [XmlArray(ElementName = "outputVariables")]
        public List<OperationVariable> OutputVariables { get; set; } = new();

        public List<OperationVariable> this[int dir]
        {
            get
            {
                if (dir == 0)
                    return InputVariables;
                else
                if (dir == 1)
                    return OutputVariables;
                else
                    return InoutputVariables;
            }
            set
            {
                if (dir == 0)
                    InputVariables = value;
                else
                if (dir == 1)
                    OutputVariables = value;
                else
                    InoutputVariables = value;
            }
        }

        public Operation()
        {
            ModelType = ModelTypes.Operation;
        }

        public Operation(SubmodelElement src)
            : base(src)
        {
            if (!(src is Operation op))
            {
                return;
            }

            ModelType = ModelTypes.Operation;

            for (int i = 0; i < 2; i++)
            {
                if (op[i] != null)
                {
                    if (this[i] == null)
                    {
                        this[i] = new List<OperationVariable>();
                    }

                    foreach (var ov in op[i])
                    {
                        this[i].Add(new OperationVariable(ov));
                    }
                }
            }
        }
    }
}
