using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.ServiceModel.Web;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Globalization;
using Amazon.SimpleDB.Model;

namespace NinePlaces_Service
{
    public static class GuidExtensions
    {
        public static string ToTiny(this Guid in_gGuid)
        {
            return Convert.ToBase64String(in_gGuid.ToByteArray()).Replace('/', '_').Replace('+', '-').Replace("=", "");
        }
    }

    public static class RetryUtility
    {
        public static void RetryAction(int in_nNumRetries, int in_nRetryTimeout, Action in_aActionToRetry)
        {
            do
            {
                try
                {
                    in_aActionToRetry();
                    return;
                }
                catch
                {
                    if (in_nNumRetries <= 0)
                    {
                        throw;
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(in_nRetryTimeout);
                    }
                }
            } while (in_nNumRetries-- > 0);
        }
    }

    public static class Helpers
    {
        public static void SetResponseCode(HttpStatusCode in_code, string in_Description)
        {
            Log.Warning("Response Code set to: " + in_code.ToString());
            WebOperationContext.Current.OutgoingResponse.StatusCode = in_code;
            WebOperationContext.Current.OutgoingResponse.StatusDescription = in_Description;
        }

        public static void SetResponseCode(HttpStatusCode in_code)
        {
            Log.Warning("Response Code set to: " + in_code.ToString());
            WebOperationContext.Current.OutgoingResponse.StatusCode = in_code;
        }
    }

    public class TrustAllCertificatePolicy : System.Net.ICertificatePolicy
    {
        public TrustAllCertificatePolicy()
        { }

        public bool CheckValidationResult(ServicePoint sp,
         X509Certificate cert, WebRequest req, int problem)
        {
            return true;
        }
    }

    public static class SimpleDBHelpers
    {

        public static bool AddAttributeValue(ref ReplaceableItem in_rToAdd, string strKey, string strValue, bool in_bReplace)
        {
            if (string.IsNullOrEmpty(strValue))
                return false;

            ReplaceableAttribute a = new ReplaceableAttribute();
            a.Name = strKey;
            a.Value = strValue;
            a.Replace = in_bReplace;
            in_rToAdd.Attribute.Add(a);
            return true;
        }

        public static string ValueFromSimpleDB(string in_strEncodedValue)
        {
            if (in_strEncodedValue.Length == 17 && in_strEncodedValue[0] == 'l')
            {
                // possibly a hex long.
                try
                {
                    byte[] bytes = SimpleDBHelpers.HexStringToByteArray(in_strEncodedValue.Substring(1));
                    return System.BitConverter.ToInt64(bytes, 0).ToString();
                }
                catch (Exception)
                { }
            }
            if (in_strEncodedValue.Length == 22 && in_strEncodedValue[0] == 'd')
            {
                try
                {
                    return SimpleDBHelpers.StringToDouble(in_strEncodedValue.Substring(1)).ToString();
                }
                catch (Exception)
                {
                }
            }
            return in_strEncodedValue;
        }

        public static string ValueToSimpleDB(string in_strUnencodedValue)
        {
            long lValue = long.MinValue;
            double dValue = double.MinValue;
            if (long.TryParse(in_strUnencodedValue, out lValue))
            {
                byte[] bytes = System.BitConverter.GetBytes(lValue);
                return "l" + SimpleDBHelpers.ByteArrayToHexString(bytes);
            }
            else if (double.TryParse(in_strUnencodedValue, out dValue))
            {
                return "d" + SimpleDBHelpers.DoubleToString(dValue);
            }

            else return in_strUnencodedValue;
        }

        // a quick implementation of http://tools.ietf.org/html/draft-wood-ldapext-float-00
        // could be much faster - i rely way too heavily on tostring, substring, convert.toint/todouble, etc.
        public static string DoubleToString(double in_dValue)
        {
            string strValue = in_dValue.ToString("+0.00000000000000000E+000;-0.00000000000000000E+000", CultureInfo.InvariantCulture);
            StringBuilder sb = new StringBuilder();
            // OK, now we have a mantissa, exponent, neg.
            bool bNegative = strValue[0] == '-';
            int nExponent = Convert.ToInt32(strValue.Substring(21, 4));
            double dMantissa = Convert.ToDouble(strValue.Substring(0, 19));

            // lets start turning it into a string.
            if (nExponent >= 0 && !bNegative)
            {
                // CASE 5
                sb.Append("5");
                sb.Append(nExponent.ToString("000"));
                sb.Append(dMantissa.ToString("0.0000000000000000").Replace(".", ""));
            }
            else if (nExponent < 0 && !bNegative)
            {
                // CASE 4
                sb.Append("4");
                sb.Append((nExponent + 999).ToString("000"));
                sb.Append(dMantissa.ToString("0.0000000000000000").Replace(".", ""));
            }
            else if (strValue == "+0.00000000000000000E+000")
            {
                // CASE 3
                sb.Append("300000000000000000000");
            }
            else if (nExponent < 0 && bNegative)
            {
                // CASE 2
                sb.Append("2");
                sb.Append((nExponent * -1).ToString("000"));
                dMantissa += 10;
                sb.Append(dMantissa.ToString("0.0000000000000000").Replace(".", ""));

            }
            else if (nExponent >= 0 && bNegative)
            {
                // CASE 1
                sb.Append("1");
                sb.Append(((nExponent * -1) + 999).ToString("000"));
                dMantissa += 10;
                sb.Append(dMantissa.ToString("0.0000000000000000").Replace(".", ""));
            }

            return sb.ToString();
        }

        public static double StringToDouble(string in_strValue)
        {
            char cCase = in_strValue[0];
            string strExp = in_strValue.Substring(1, 3);
            char cMantissaSig = in_strValue[4];
            string strMantissaInsig = in_strValue.Substring(5);

            StringBuilder sbToConvert = new StringBuilder();
            switch (cCase)
            {
                case '5':
                    sbToConvert.Append(cMantissaSig);
                    sbToConvert.Append('.');
                    sbToConvert.Append(strMantissaInsig);
                    sbToConvert.Append('e');
                    sbToConvert.Append(strExp);
                    break;
                case '4':
                    sbToConvert.Append(cMantissaSig);
                    sbToConvert.Append('.');
                    sbToConvert.Append(strMantissaInsig);
                    sbToConvert.Append("e");
                    sbToConvert.Append((Convert.ToInt32(strExp) - 999).ToString());
                    break;
                case '3':
                    return 0.0;
                case '2':
                    double dVal = Convert.ToDouble(cMantissaSig + "." + strMantissaInsig) - 10;
                    sbToConvert.Append(dVal.ToString());
                    sbToConvert.Append("e-");
                    sbToConvert.Append(strExp);
                    break;
                case '1':
                    double dVal2 = Convert.ToDouble(cMantissaSig + "." + strMantissaInsig) - 10;
                    sbToConvert.Append(dVal2.ToString());
                    sbToConvert.Append("e");
                    sbToConvert.Append(((Convert.ToInt32(strExp) - 999) * -1).ToString());
                    break;
            }

            return Convert.ToDouble(sbToConvert.ToString());
        }

        static string HexAlphabet = "0123456789ABCDEF";
        public static string ByteArrayToHexString(byte[] Bytes)
        {
            StringBuilder Result = new StringBuilder();

            foreach (byte B in Bytes)
            {
                Result.Append(HexAlphabet[(int)(B >> 4)]);
                Result.Append(HexAlphabet[(int)(B & 0xF)]);
            }

            return Result.ToString();
        }

        static int[] HexValue = new int[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
                                     0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x0B, 0x0C, 0x0D,
                                     0x0E, 0x0F };

        public static byte[] HexStringToByteArray(string Hex)
        {
            byte[] Bytes = new byte[Hex.Length / 2];

            for (int x = 0, i = 0; i < Hex.Length; i += 2, x += 1)
            {
                Bytes[x] = (byte)(HexValue[Char.ToUpper(Hex[i + 0]) - '0'] << 4 |
                                  HexValue[Char.ToUpper(Hex[i + 1]) - '0']);
            }

            return Bytes;
        }
    }
}