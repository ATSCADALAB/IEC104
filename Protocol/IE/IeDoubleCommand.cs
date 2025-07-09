using IEC104.Protocol.IE.Base;
using System.IO;

namespace IEC104.Protocol.IE
{
    public class IeDoubleCommand : InformationElement
    {
        private byte commandState;
        private byte qualifier;

        public IeDoubleCommand(bool commandValue, byte qualifier = 0)
        {
            // Double command: 0=Not permitted, 1=Off, 2=On, 3=Not permitted
            this.commandState = (byte)(commandValue ? 2 : 1);
            this.qualifier = qualifier;
        }

        public IeDoubleCommand(byte commandState, byte qualifier = 0)
        {
            this.commandState = commandState;
            this.qualifier = qualifier;
        }

        public IeDoubleCommand(BinaryReader reader)
        {
            byte dcoByte = reader.ReadByte();
            commandState = (byte)(dcoByte & 0x03);  // Bits 0-1
            qualifier = (byte)((dcoByte >> 2) & 0x3F); // Bits 2-7
        }

        public bool GetCommandValue()
        {
            return commandState == 2; // On
        }

        public byte GetCommandState()
        {
            return commandState;
        }

        public byte GetQualifier()
        {
            return qualifier;
        }

        public override string ToString()
        {
            string state;
            switch (commandState)
            {
                case 0:
                    state = "Not permitted";
                    break;
                case 1:
                    state = "Off";
                    break;
                case 2:
                    state = "On";
                    break;
                case 3:
                    state = "Not permitted";
                    break;
                default:
                    state = $"Unknown({commandState})";
                    break;
            }
            return $"DoubleCommand: {state}";
        }
    }
}