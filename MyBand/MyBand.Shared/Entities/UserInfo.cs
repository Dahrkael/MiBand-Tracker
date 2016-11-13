using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections.Generic;
using System.Text;
using Windows.Storage.Streams;
using Windows.Storage;

namespace MyBand.Entities
{
    class UserInfo
    {
        private int    uid; // max 10 chars
        private bool   male;
        private byte   age; // años
        private byte   height; // cm
        private byte   weight; // kg
        private string alias; // 10 chars

        public UserInfo(bool Male, byte Age, byte Height, byte Weight, int UID = 0x01, string Alias = "User 0001")
        {
            this.uid    = UID;
            this.male   = Male;
            this.age    = Age;
            this.height = Height;
            this.weight = Weight;
            this.alias  = Alias;
        }
        public bool Valid   { get; set; }
        public int UID      { get { return this.uid;    } set { this.uid    = value; } }
        public bool Male    { get { return this.male;   } set { this.male   = value; } }
        public byte Age     { get { return this.age;    } set { this.age    = value; } }
        public byte Height  { get { return this.height; } set { this.height = value; } }
        public byte Weight  { get { return this.weight; } set { this.weight = value; } }
        public string Alias { get { return this.alias;  } set { this.alias  = value; } }


        public static UserInfo FromBuffer(Byte[] buffer, String MACAddress)
        {
            if(buffer.Length != 20)
            { 
                return null;
            }

            Byte[] crcBuffer = new Byte[19];
            Array.Copy(buffer, crcBuffer, 19);

            Int32 CRC = crc(crcBuffer);
            Int16 crcaddr = Int16.Parse(MACAddress.Substring(MACAddress.Length - 2, 2), System.Globalization.NumberStyles.HexNumber);
            if(buffer[19] != (byte)(CRC ^ crcaddr))
            { return null; }

            UserInfo userInfo = new UserInfo(((buffer[4] == 1) ? true : false), buffer[5], buffer[6], buffer[7], BitConverter.ToInt32(buffer, 0), Encoding.UTF8.GetString(buffer, 8, 11));

            return userInfo;
        }

        public IBuffer AsBuffer(String MACAddress, DataMode DataMode)
        {
            Byte[] buffer = new Byte[20];

            // copiamos el entero del uid a los cuatro primeros
            Array.Copy(BitConverter.GetBytes(this.uid), buffer, 4);
            buffer[4] = (byte)(male ? 1 : 0);
            buffer[5] = this.age;
            buffer[6] = this.height;
            buffer[7] = this.weight;
            buffer[8] = (byte)DataMode;
            Array.Copy(Encoding.UTF8.GetBytes(this.alias), 0, buffer, 9, this.alias.Length);

            Byte[] crcBuffer = new Byte[19];
            Array.Copy(buffer, crcBuffer, 19);
            Int32 CRC = crc(crcBuffer);

            string macEnd = MACAddress.Substring(MACAddress.Length - 2, 2);
            Int16 crcaddr = Int16.Parse(macEnd, System.Globalization.NumberStyles.HexNumber);
            CRC = (CRC ^ crcaddr) & 0xFF;
            buffer[19] = (byte)CRC;

            return buffer.AsBuffer();
        }

        private static Int32 crc(Byte[] buffer)
        {
            Int32 crc = 0;
            for(int i = 0; i < buffer.Length; i++)
            {
                crc = crc ^ (buffer[i] & 0xFF);
                for(int j = 0; j < 8; j++)
                {
                    if ((crc & 0x01) != 0)
                    {
                        crc = (crc >> 1) ^ 0x8C;
                    }
                    else
                    {
                        crc = crc >> 1;
                    }
                }
            }
            return crc;
        }

        public static UserInfo FromSetting(ApplicationDataCompositeValue setting)
        {
            UserInfo info = new UserInfo((bool)setting["male"], (byte)setting["age"], (byte)setting["height"], (byte)setting["weight"],
                                         (int)setting["uid"], (string)setting["alias"]);
            return info;
        }

        public ApplicationDataCompositeValue ToSetting()
        {
            ApplicationDataCompositeValue setting = new ApplicationDataCompositeValue();

            setting["uid"]    = this.uid;
            setting["male"]   = this.male;
            setting["age"]    = this.age;
            setting["height"] = this.height;
            setting["weight"] = this.weight;
            setting["alias"]  = this.alias;

            return setting;
        }
    }

    enum DataMode
    {
        Normal = 0,
        ClearData  = 1,
        RetainData = 2
    }
}
