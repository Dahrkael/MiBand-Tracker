using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MyBand.Entities
{
    // esto creo que son los datos en crudo del sensor de movimiento
    // son tres valores asi que a lo mejor son XYZ
    class SensorData
    {
        // variables del sensor
        private short sensorLastDataIndex = -1;
        private MemoryStream sensorSourceStream;

        public SensorData()
        {
            sensorLastDataIndex = -1;
            sensorSourceStream = new System.IO.MemoryStream();
        }

        public void NewPacket(Byte[] buffer)
        {
            // short 	        2 bytes 	-32768 to 32767 
            // unsigned short	2 bytes 	0 to 65535 
            // short i = (short)((buffer[0] & 0xFF) | ((buffer[1] & 0xFF) << 8));
            short i = BitConverter.ToInt16(buffer, 0);
            if (i == -1)
            {
                Debug.WriteLine("[SensorData] sensor data notify packages index is -1 !!!");
                Debugger.Break();
                return;
            }

            if (sensorLastDataIndex != -2 || i != 0) // FIXME WTF
            {
                if ((sensorLastDataIndex + 1 == i) && (i > 1))
                {
                    // todo correcto, son sucesivos
                }
                else
                {
                    Debug.WriteLine("[SensorData] sensor data notify packages index is not continuous!!!");
                    /*
                    if (x.a())
                    {
                        IMiLiProfile.LEParams leparams = _getLEParams();
                        stringbuilder.append("connInt = ").append(leparams.connInt).append(",latency = ").append(leparams.latency).append(",connIntMin = ").append(leparams.connIntMin).append(",connIntMax = ").append(leparams.connIntMax);
                    }
                    */
                }
            }
            this.sensorLastDataIndex = i;

            byte[] buffer2 = new byte[buffer.Length - 2];
            Array.Copy(buffer, 2, buffer2, 0, buffer.Length - 2);
            sensorSourceStream.Write(buffer2, 0, buffer2.Length);
        }

        private void parse()
        {
            Byte[] buffer = new Byte[6];
            try
            {
                sensorSourceStream.Read(buffer, 0, 6);
            }
            catch (Exception exception)
            {
                Debug.WriteLine("[SensorData] Error al leer bloque del stream");
                return;
            }
            short k1;
            short l1;
            short i2;
            short word0;
            short word1;
            short word2;

            k1 = BitConverter.ToInt16(buffer, 0);
            l1 = BitConverter.ToInt16(buffer, 2);
            i2 = BitConverter.ToInt16(buffer, 4);
            word0 = (short)(((k1 & 0xFFF) << 20) >> 20);
            word1 = (short)(((l1 & 0xFFF) << 20) >> 20);
            word2 = (short)(((i2 & 0xFFF) << 20) >> 20);

            process(word0, word1, word2);
        }

        private void process(short word0, short word1, short word2)
        {

        }
        /*
        private void a(short word0, short word1, short word2)
        {
            Set set = e.entrySet();
            try
            {
                Iterator iterator = set.iterator();
                do
                {
                    if (!iterator.hasNext())
                    {
                        break;
                    }
                    java.util.Map.Entry entry = (java.util.Map.Entry)iterator.next();
                    a a1 = (a)entry.getValue();
                    if (a1.a(word0, word1, word2))
                    {
                        ((n)entry.getKey()).a((n)entry.getKey(), a1.c(), a1.g());
                    }
                } while (true);
            }
            catch (Exception exception) { }
        }

        public boolean a(int i1, int j1, int k1)
    {
        if (!k)
        {
            throw new Exception("receive sample when there is not sport");
        }
          goto _L1
        Exception exception;
        exception;
        x.d("gaocept", exception.getMessage());
        i = true;
_L5:
        return false;
_L1:
        ArrayList arraylist;
        f[e] = i1;
        g[e] = j1;
        h[e] = k1;
        e = 1 + e;
        if (b == null || e <= 25)
        {
            continue; 
        }
        arraylist = new ArrayList();
        int l1 = 0;
_L3:
        if (l1 >= e)
        {
            break; 
        }
        arraylist.add(Short.valueOf((short)f[l1]));
        arraylist.add(Short.valueOf((short)g[l1]));
        arraylist.add(Short.valueOf((short)h[l1]));
        l1++;
        if (true) goto _L3; else goto _L2
_L2:
        b.a(arraylist);
        e = 0;
        if (j) goto _L5; else goto _L4
_L4:
        boolean flag = a.receive(i1, j1, k1);
        return flag;
    }
    }
         * */
    }
}
