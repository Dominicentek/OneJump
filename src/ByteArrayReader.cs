using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneJump.src {
    public class ByteArrayReader {
        public int Pointer { get; set; } = 0;
        public int Size { get => arr.Length; }
        private readonly byte[] arr;
        public ByteArrayReader(byte[] data) => arr = data;

        public  byte   UByte  () => arr[Pointer++];
        public ushort  UShort () => (ushort)  SShort();
        public uint    UInt   () => (uint  )  SInt  ();
        public ulong   ULong  () => (ulong )  SLong ();
        public sbyte   SByte  () => (sbyte )  UByte ();
        public  short  SShort () => ( short)((UByte () <<  8) | UByte ());
        public  int    SInt   () => ( int  )((UShort() << 16) | UShort());
        public  long   SLong  () => ( long )((UInt  () << 32) | UInt  ());
        public  float   Float () => RawConvert<uint , float >(UInt ());
        public  double  Double() => RawConvert<ulong, double>(ULong());

        public string String() {
            string str = "";
            while (true) {
                byte character = UByte();
                if (character == 0) break;
                str += (char)character;
            }
            return str;
        }
        public string String(int length) {
            string str = "";
            for (int i = 0 ; i < length; i++) {
                str += (char)UByte();
            }
            return str;
        }
        public byte[] Binary(int length) {
            byte[] data = new byte[length];
            for (int i = 0; i < length; i++) {
                data[i] = UByte();
            }
            return data;
        }

        private unsafe T RawConvert<F, T>(F value)
            where F: unmanaged
            where T: unmanaged
        => *(T*)&value;
    }
}