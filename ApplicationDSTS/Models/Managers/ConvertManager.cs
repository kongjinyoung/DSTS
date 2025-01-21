using ApplicationDSTS.Models.Clients;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace ApplicationDSTS.Models.Managers
{
    public class ConvertManager
    {
        public byte[] ElementDataToArr(params object[] obj) // for Send Data into Array
        {
            byte[] d_storage = new byte[] { };  //동적 배열
            byte[] arr = { };   //가변 배열

            foreach (dynamic item in obj)
            {
                d_storage = BitConverter.GetBytes(item);
                int length = d_storage.Length;
                for (int j = 0; j < length; j++)
                {
                    Array.Resize(ref arr, arr.Length + 1);
                    arr[arr.Length - 1] = d_storage[j];
                }
            }
            return arr;
        }
        byte[] GetBytesBlock(double[] values)
        {
            var result = new byte[values.Length * sizeof(double)];
            Buffer.BlockCopy(values, 0, result, 0, result.Length);
            return result;
        }
        public byte[] StringToByte(StringBuilder sData) // Convert Ascii
        {
            return Encoding.ASCII.GetBytes(sData.ToString());
        }
        public byte[] StringToByte(string sData) // Convert Ascii
        {
            return Encoding.ASCII.GetBytes(sData);
        }

        public string ByteToHexString(byte[] bytes)
        {
            string hex = BitConverter.ToString(bytes);
            return hex.Replace("-", "");
        }
        public byte[] HexToByte(string hex)
        {
            byte[] convert = new byte[hex.Length / 2];

            int length = convert.Length;

            for (int i = 0; i < length; i++)
            {
                convert[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            return convert;
        }
        public float[] ConvertByteArrayToFloatArray(byte[] byteArray)
        {
            int floatArrayLength = byteArray.Length / 4; // 1 float = 4 bytes
            float[] floatArray = new float[floatArrayLength];

            for (int i = 0; i < floatArrayLength; i++)
            {
                // 4바이트씩 읽어와서 float으로 변환
                floatArray[i] = BitConverter.ToSingle(byteArray, i * 4);
            }

            return floatArray;
        }
        public string ConvertByteArrayToBase64(byte[] byteArray)
        {
            // byte array를 Base64로 변환하여 저장
            return Convert.ToBase64String(byteArray);
        }

        /// <summary>
        /// For Socket
        /// </summary>
        /// <param name="data"></param>
        /// <param name="client"></param>
        public void SendData(byte[] data, EthernetClient client)
        {
            try
            {
                if (SocketConnected(client.Socket))
                {
                    client.Socket.Send(data, 0, data.Length, SocketFlags.None);
                }
            }
            catch (Exception)
            {
                return;
            }
        }
        public bool SocketConnected(Socket soc)
        {
            try
            {
                bool part1 = soc.Poll(500, SelectMode.SelectRead);
                bool part2 = soc.Available == 0;
                if (part1 && part2)
                    return false; //연결상태 X
                else
                    return true; //연결상태 O
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
    /// <summary>
    /// Foreground Changed Value:int
    /// </summary>
    public class ForegroundConvertorByValue : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int color = (int)value;

            if (color == 0)
            {
                return new SolidColorBrush(Color.FromRgb(0x2E, 0xff, 0x00));
            }
            else if (color == 1)
            {
                return new SolidColorBrush(Color.FromRgb(0xf3, 0xff, 0x00));
            }
            else
            {
                return Brushes.Red;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as SolidColorBrush).Color;
        }
    }

    /// <summary>
    /// Foreground Changed Value:bool
    /// </summary>
    public class ForegroundConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool colorChk = (bool)value;

            if (colorChk) return Brushes.GreenYellow;
            else return new SolidColorBrush(Color.FromRgb(0xF7, 0x8A, 0x09));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Toggle Button, Changed Value
    /// </summary>
    public class ToggleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool IsRunning = (bool)value;

            if (IsRunning) return true;
            else return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
