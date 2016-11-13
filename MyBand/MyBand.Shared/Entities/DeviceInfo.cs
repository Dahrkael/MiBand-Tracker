using System;
using System.Collections.Generic;
using System.Text;
using Windows.Storage;

namespace MyBand.Entities
{
    class DeviceInfo
    {
        public static byte STATUS_AUTHENTICATION_FAILED  = 1;
        public static byte STATUS_AUTHENTICATION_SUCCESS = 2;

        private  int    appearance;
        private  String deviceID;
        private  int    feature;
        private  int    firmwareVersion;
        private  int    hardwareVersion;
        private  int    profileVersion;

        public DeviceInfo(string DeviceID, int FirmwareVersion, int HardwareVersion, int ProfileVersion, int Appearance, int Feature)
        {
            this.deviceID        = DeviceID;
            this.firmwareVersion = FirmwareVersion;
            this.hardwareVersion = HardwareVersion;
            this.profileVersion  = ProfileVersion;
            this.appearance      = Appearance;
            this.feature         = Feature;
        }

        public DeviceInfo(Byte[] Data)
        {
            if(Data.Length != 16) { Valid = false; return; }

            Byte[] fw = new Byte[7];
            Array.Copy(Data, fw, fw.Length);

            int i = (crc(fw) ^ 0xFF) & Data[3];

            // FIXME esto da diferente resultado pero el algoritmo de crc es identico (en userinfo funciona)
            /*if ((byte)i != Data[7]) 
            {
                 // DeviceInfo CRC verification failed
                Valid = false;
                return;
            }*/
            deviceID = String.Format("{0:X02}{1:X02}{2:X02}{3:X02}{4:X02}{5:X02}{6:X02}", fw[0], fw[1], fw[2], fw[3], fw[4], fw[5], fw[6]);
            feature         = Int32.Parse(deviceID.Substring(8,  2), System.Globalization.NumberStyles.HexNumber);
            appearance      = Int32.Parse(deviceID.Substring(10, 2), System.Globalization.NumberStyles.HexNumber);
            hardwareVersion = Int32.Parse(deviceID.Substring(12, 2), System.Globalization.NumberStyles.HexNumber);
            profileVersion  = BitConverter.ToInt32(Data, 8);
            firmwareVersion = BitConverter.ToInt32(Data, 12);
        }

        public bool Valid { get; set; }

        public int Appearance      { get { return appearance;      } set { } }
        public String DeviceID     { get { return deviceID;        } set { } }
        public int Feature         { get { return feature;         } set { } }
        public int HardwareVersion { get { return hardwareVersion; } set { } }

        public int FirmwareVersionBuild
        {
            get { return 0xFF & firmwareVersion; }
        }

        public int FirmwareVersionMajor
        {
            get { return 0xFF & (firmwareVersion >> 24); }
        }

        public int FirmwareVersionMinor
        {
            get { return 0xFF & (firmwareVersion >> 16); }
        }

        public int FirmwareVersionRevision
        {
            get { return 0xFF & (firmwareVersion >> 8); }
        }

        public String FirmwareVersion
        {
            get { return FirmwareVersionMajor + "." + FirmwareVersionMinor + "." + FirmwareVersionRevision + "." + FirmwareVersionBuild; }
        }

        public String ProfileVersion
        {
            get { return ProfileVersionMajor + "." + ProfileVersionMinor + "." + ProfileVersionRevision + "." + ProfileVersionBuild; }
        }

        public int ProfileVersionBuild
        {
            get { return 0xFF & profileVersion; }
        }

        public int ProfileVersionMajor
        {
            get { return 0xFF & (profileVersion >> 24); }
        }

        public int ProfileVersionMinor
        {
            get { return 0xFF & (profileVersion >> 16); }
        }

        public int ProfileVersionRevision
        {
            get { return 0xFF & (profileVersion >> 8); }
        }

        private Int32 crc(Byte[] buffer)
        {
            Int32 crc = 0;
            for(int i = 0; i < buffer.Length; i++)
            {
                crc = crc ^ (buffer[i] & 0xFF);
                for(int j = 0; j < 8; j++)
                {
                    if ((crc & 0x01) != 0)
                    {
                        crc = 0x8C ^ 0xFF & crc >> 1;
                    }
                    else
                    {
                        crc = 0xff & crc >> 1;
                    }
                }
            }
            return crc;
        }

        public static DeviceInfo FromSetting(ApplicationDataCompositeValue setting)
        {
            DeviceInfo info = new DeviceInfo((string)setting["deviceID"], (int)setting["firmwareVersion"], (int)setting["hardwareVersion"],
                                             (int)setting["profileVersion"], (int)setting["appareance"], (int)setting["feature"]);
            return info;
        }

        public ApplicationDataCompositeValue ToSetting()
        {
            ApplicationDataCompositeValue setting = new ApplicationDataCompositeValue();

            setting["deviceID"]        = this.deviceID;
            setting["firmwareVersion"] = this.firmwareVersion;
            setting["hardwareVersion"] = this.hardwareVersion;
            setting["profileVersion"]  = this.profileVersion;
            setting["appareance"]      = this.appearance;
            setting["feature"]         = this.feature;

            return setting;
        }
    }
}
