using BepuPhysics;
using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Thundagun
{
    public class MemoryObjectManagement
    {

        



        //first byte acts as a lock. I hope.
        public static BinaryWriter stream;
        public static List<byte> buffer = new List<byte>();


        //public static bool iswriting = false;
        //public static void startBytes()
        //{

        //    if(iswriting == false)
        //    {
        //        iswriting = true;
        //        Thundagun.Msg("!!!WAITING FOR PYTHON TO FLUSH QUEUE!!!");
        //        waitforconnect();
        //        byte[] start = new byte[1];
        //        byte type = 255;



        //        stream.Write(type); //write beginning thing again.
        //        Thundagun.Msg("!!!WRITING BYTES!!!");
        //        iswriting = true;
        //    }


        //}


        public static bool isconnected = false;
        public static void Release()
        {
            byte[] data = buffer.ToArray();
            buffer.Clear();

            stream.Write(data, 0, data.Length);
            stream.Write("FINIS_FR".ToList().Select(o => ((byte)o)).ToArray(), 0, "FINIS_FR".Length);
            stream.Flush();

        }

        public static void Purge()
        {
            buffer.Clear();

        }

        public static void waitforconnect()
        {
            if (isconnected == false)
            {
                Thundagun.Msg("!!!WAITING FOR IPC CONNECTION PIPE!!!");
                
                
                isconnected = true;


                stream = new BinaryWriter(Thundagun.MemoryFrooxEngine);
                

                Thundagun.Msg("IPC PIPE CONNECTED!");


            }

            


        }

        private static long offset = 0;

        public static void Save(byte array)
        {
            waitforconnect();
            buffer.Add(array);
        }

        public static void Save(int array)
        {
            waitforconnect();
            buffer.AddRange(BitConverter.GetBytes(array));
        }

        public static void Save(bool array)
        {
            waitforconnect();
            buffer.AddRange(BitConverter.GetBytes(array));

        }

        public static void Save(float array)
        {
            waitforconnect();
            buffer.AddRange(BitConverter.GetBytes(array));
        }

        public static void Save(long array)
        {
            waitforconnect();
            buffer.AddRange(BitConverter.GetBytes(array));
        }

        public static void Save(ulong array)
        {
            waitforconnect();
            buffer.AddRange(BitConverter.GetBytes(array));
        }

        public static void Save(uint array)
        {
            waitforconnect();
            buffer.AddRange(BitConverter.GetBytes(array));
        }


        //arrays
        public static void SaveArray(byte[] array)
        {
            waitforconnect();
            int size = array.Length;

            buffer.AddRange(BitConverter.GetBytes(size));
            for (int i = 0; i < size; i++)
            {
                buffer.AddRange(BitConverter.GetBytes(array[i]));
            }
        }

        public static void SaveArray(int[] array)
        {
            waitforconnect();
            int size = array.Length;

            buffer.AddRange(BitConverter.GetBytes(size));
            for (int i = 0; i < size; i++)
            {
                buffer.AddRange(BitConverter.GetBytes(array[i]));
            }
        }

        public static void SaveArray(bool[] array)
        {
            waitforconnect();
            int size = array.Length;

            buffer.AddRange(BitConverter.GetBytes(size));
            for (int i = 0; i < size; i++)
            {
                buffer.AddRange(BitConverter.GetBytes(array[i]));
            }
        }

        public static void SaveArray(float[] array)
        {
            waitforconnect();
            int size = array.Length;

            buffer.AddRange(BitConverter.GetBytes(size));
            for (int i = 0; i < size; i++)
            {
                buffer.AddRange(BitConverter.GetBytes(array[i]));
            }
        }

        public static void SaveArray(long[] array)
        {
            waitforconnect();
            int size = array.Length;

            buffer.AddRange(BitConverter.GetBytes(size));
            for (int i = 0; i < size; i++)
            {
                buffer.AddRange(BitConverter.GetBytes(array[i]));
            }
        }

        public static void SaveArray(ulong[] array)
        {
            waitforconnect();
            int size = array.Length;

            buffer.AddRange(BitConverter.GetBytes(size));
            for (int i = 0; i < size; i++)
            {
                buffer.AddRange(BitConverter.GetBytes(array[i]));
            }
        }

        public static void SaveArray(uint[] array)
        {
            waitforconnect();
            int size = array.Length;

            buffer.AddRange(BitConverter.GetBytes(size));
            for (int i = 0; i < size; i++)
            {
                buffer.AddRange(BitConverter.GetBytes(array[i]));
            }
        }

        public static void SaveString(string array)
        {
            waitforconnect();
            int size = array.Length;

            buffer.AddRange(BitConverter.GetBytes(size));
            for (int i = 0; i < size; i++)
            {
                buffer.AddRange(BitConverter.GetBytes(array[i]));
            }
        }
    }
}
