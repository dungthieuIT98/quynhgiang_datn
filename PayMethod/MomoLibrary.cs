using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;

namespace DoAn.PayMethod
{
    public class MomoLibrary
    {
        private string partnerCode;
        private string accessKey;
        private string secretKey;
        private string endpoint;
        private string returnUrl;
        private string notifyUrl;

        public MomoLibrary()
        {
            this.partnerCode = System.Configuration.ConfigurationManager.AppSettings["momo_PartnerCode"];
            this.accessKey = System.Configuration.ConfigurationManager.AppSettings["momo_AccessKey"];
            this.secretKey = System.Configuration.ConfigurationManager.AppSettings["momo_SecretKey"];
            this.endpoint = System.Configuration.ConfigurationManager.AppSettings["momo_Endpoint"];
            this.returnUrl = System.Configuration.ConfigurationManager.AppSettings["momo_ReturnUrl"];
            this.notifyUrl = System.Configuration.ConfigurationManager.AppSettings["momo_NotifyUrl"];
        }

        public string CreatePaymentRequest(string orderId, string orderInfo, long amount, string customerName, string customerEmail, string customerPhone)
        {
            try
            {
                string requestId = DateTime.Now.Ticks.ToString();
                string orderType = "momo_wallet";
                string requestType = "captureWallet";
                string extraData = "";

                //Build body json request
                var rawBody = new
                {
                    partnerCode = this.partnerCode,
                    partnerName = "An Phát Computer",
                    storeId = "An Phát Computer",
                    requestId = requestId,
                    amount = amount,
                    orderId = orderId,
                    orderInfo = orderInfo,
                    redirectUrl = this.returnUrl,
                    ipnUrl = this.notifyUrl,
                    lang = "vi",
                    extraData = extraData,
                    requestType = requestType,
                    items = new[]
                    {
                        new
                        {
                            id = orderId,
                            name = orderInfo,
                            description = orderInfo,
                            category = "Thanh toán đơn hàng",
                            imageUrl = "https://anphatcomputer.com/logo.png",
                            price = amount,
                            quantity = 1
                        }
                    },
                    userInfo = new
                    {
                        name = customerName,
                        email = customerEmail,
                        phoneNumber = customerPhone
                    }
                };

                string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(rawBody);
                string signature = ComputeHmacSha256(jsonBody, this.secretKey);

                var request = (HttpWebRequest)WebRequest.Create(this.endpoint);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add("X-PartnerCode", this.partnerCode);
                request.Headers.Add("X-AccessKey", this.accessKey);
                request.Headers.Add("X-Signature", signature);

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(jsonBody);
                }

                var response = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    JObject jresult = JObject.Parse(result);
                    return jresult["payUrl"].ToString();
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(messageBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        public bool ValidateCallback(string rawBody, string signature)
        {
            string computedSignature = ComputeHmacSha256(rawBody, this.secretKey);
            return computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
        }
    }
} 