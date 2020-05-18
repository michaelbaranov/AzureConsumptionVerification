using System;
using System.Net;

namespace AzureConsumptionVerification
{
    internal class RestException : Exception
    {
        public string Code;
        public string ResponseMessage;
        public HttpStatusCode StatusCode;

        public RestException(string? message, HttpStatusCode statusCode, string code, string responseMessage) :
            base(message)
        {
            StatusCode = statusCode;
            Code = code;
            ResponseMessage = responseMessage;
        }
    }
}