using System;
using System.Net;
using System.Text;

namespace AzureConsumptionVerification
{
    internal class RestException : Exception
    {
        public string Code;
        public string ResponseMessage;
        public HttpStatusCode StatusCode;

        public RestException(string message, HttpStatusCode statusCode, string code, string responseMessage) :
            base(message)
        {
            StatusCode = statusCode;
            Code = code;
            ResponseMessage = responseMessage;
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder(base.ToString());
            stringBuilder.AppendLine("Code:");
            stringBuilder.AppendLine(Code);
            stringBuilder.AppendLine("Message");
            stringBuilder.AppendLine(Message);
            return stringBuilder.ToString();
        }
    }
}