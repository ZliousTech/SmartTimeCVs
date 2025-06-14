using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Web;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Azure;

namespace Common.Base
{
    public class SysBase
    {
        public static string Decrypt(string cipherText)
        {
            string EncryptionKey = "FiCrScho0220aNdeL";

            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6E, 0x20, 0x4D, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }
        
        public static bool PtrCode(string? CompanyGuidID, string? PrtCode)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string SmartTimeAPIUri = "https://smarttimeapi.zlioustech.com";
                    client.BaseAddress = new Uri($"{SmartTimeAPIUri}/api/Company/");
                    var endpoint = $"IsCompanyExist/{CompanyGuidID}";
                    var response = client.GetAsync(endpoint).Result;
                    if (response != null && response.IsSuccessStatusCode)
                    {
                        string jsonDataString = response.Content.ReadAsStringAsync().Result;
                        dynamic jsonData = JsonConvert.DeserializeObject(jsonDataString);
                        if (jsonData != null && jsonData?.status == "True")
                        {
                            string? IsExist = jsonData?.data.isExist;
                            if (IsExist is null) return false;
                            if (IsExist?.ToString().ToLower() == "false")
                            {
                                return false;
                            }
                            else //Company is exist
                            {
                                //Check PrtCode.
                                //if (CalculateSentTime(Decrypt(PrtCode)).ToLower() == "logout")
                                //{
                                //    return false;
                                //}
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }

            return true;
        }

        public static string CheckShortLink_SmartTimeCVs(string bsslValue)
        {
            string? CompanyGuidID = "";
            string? UseHomePageText = "";
            string? IsAllowBiographiesFeature = "False";

            try
            {
                using (var client = new HttpClient())
                {
                    string SmartTimeAPIUri = "https://smarttimeapi.zlioustech.com";
                    client.BaseAddress = new Uri($"{SmartTimeAPIUri}/api/Company/");
                    var endpoint = $"CheckShortLinkSTCV/{bsslValue}";
                    var response = client.GetAsync(endpoint).Result;
                    if (response != null && response.IsSuccessStatusCode)
                    {
                        string jsonDataString = response.Content.ReadAsStringAsync().Result;
                        dynamic jsonData = JsonConvert.DeserializeObject(jsonDataString);
                        if (jsonData != null && jsonData?.status == "True")
                        {
                            CompanyGuidID = jsonData?.data.companyGuidID;
                            UseHomePageText = jsonData?.data.useHomePageText;
                            IsAllowBiographiesFeature = jsonData?.data.isAllowBiographiesFeature;
                        }
                        else
                        {
                            return String.Concat(CompanyGuidID, "|", UseHomePageText, "|", IsAllowBiographiesFeature);
                        }
                    }
                    else
                    {
                        return String.Concat(CompanyGuidID, "|", UseHomePageText, "|", IsAllowBiographiesFeature);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return String.Concat(CompanyGuidID, "|", UseHomePageText, "|", IsAllowBiographiesFeature);
            }

            return String.Concat(CompanyGuidID, "|", UseHomePageText, "|", IsAllowBiographiesFeature);
        }

        private static string CalculateSentTime(string SentTime)
        {
            string[] _SentTime = SentTime.Split('|');
            string HH = _SentTime[0];
            string mm = _SentTime[1];
            string SS = _SentTime[2];

            TimeSpan diffTS;
            TimeSpan d1; TimeSpan d2;
            DateTime currTime = DateTime.Parse(DateTime.Now.ToString("hh:mm:ss tt"));
            DateTime sentTime;
            string currTime_str;
            string sentTime_str;
            int url_TimeH = int.Parse(_SentTime[0]);
            int url_TimeM = int.Parse(_SentTime[1]);
            int url_TimeS = int.Parse(_SentTime[2]);
            url_TimeH = (url_TimeH / 7) - 3;
            url_TimeM = (url_TimeM / 3) - 2;
            url_TimeS = (url_TimeS / 9) - 7;
            sentTime_str = url_TimeH.ToString() + ":" + url_TimeM.ToString() + ":" + url_TimeS.ToString();
            sentTime = DateTime.Parse(sentTime_str);
            currTime_str = currTime.ToString("hh:mm:ss");
            sentTime_str = sentTime.ToString("hh:mm:ss");
            d1 = TimeSpan.Parse(currTime_str);
            d2 = TimeSpan.Parse(sentTime_str);
            if (d1 > d2) { diffTS = d1.Subtract(d2); }
            else { diffTS = d2.Subtract(d1); }
            double diffTS_int = Math.Abs(diffTS.TotalSeconds);

            if (diffTS_int >= 0 && diffTS_int <= 20) return "Login";
            else return "Logout";
        }
    }
}
