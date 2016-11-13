using System;
using System.Collections.Generic;
using System.Text;

namespace MyBand.Entities
{
    /*class SmallEntities
    {
    }*/

    class Vibration
    {
        public static byte WithLeds = 0x00;
        public static byte Normal   = 0x01;
        public static byte Special  = 0x02; // no se que diferencia hay con el normal
    }

    enum WearLocation
    {
        LeftHand,
        RightHand,
        Neck
    }
}
