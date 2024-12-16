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
using System.Threading;
using System.Threading.Tasks;

namespace Thundagun
{
    public class MemoryObjectManagement
    {

        



        //first byte acts as a lock. I hope.
        public static MemoryMappedViewStream stream;
        public static List<byte> FinalBuffer = new List<byte>();
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
        internal static void ReleaseObject()
        {
            FinalBuffer.AddRange(buffer);
            buffer.Clear();
        }

        public static bool isconnected = false;
        public static void Release()
        {
            byte[] data = FinalBuffer.ToArray();
            FinalBuffer.Clear();
            MemoryMappedViewStream stream2 = Thundagun.MemoryFrooxEngine.CreateViewStream(0, 8);


            //nessary
            SpinWait.SpinUntil(() => stream2.Seek(7, SeekOrigin.Begin) == 0, -1);

            stream = Thundagun.MemoryFrooxEngine.CreateViewStream(9, data.Length);
            stream.Write(data, 0, data.Length);
            
            stream2.Write(BitConverter.GetBytes((ulong)data.Length), 0, 7);
            stream2.Close();
            stream.Close();
        }

        public static void Purge()
        {
            buffer.Clear();

        }

        public static void waitforconnect()
        {

            


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
