using IEC104.Protocol.IE.Base;
using System.IO;

namespace IEC104.Protocol.IE
{
    public class IeQualifierOfSetPointCommand : InformationElement
    {
        private byte qualifier;
        private bool select;

        public IeQualifierOfSetPointCommand(byte qualifier, bool select)
        {
            this.qualifier = qualifier;
            this.select = select;
        }

        public IeQualifierOfSetPointCommand(BinaryReader reader)
        {
            byte qos = reader.ReadByte();
            qualifier = (byte)(qos & 0x7F);  // Bits 0-6
            select = (qos & 0x80) != 0;      // Bit 7
        }

        public byte GetQualifier()
        {
            return qualifier;
        }

        public bool IsSelect()
        {
            return select;
        }

        public override string ToString()
        {
            return $"Qualifier: {qualifier}, Select: {select}";
        }
    }