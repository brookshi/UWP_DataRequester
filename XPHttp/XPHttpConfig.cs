﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace XPHttp
{
    public class XPHttpConfig
    {
        public static XPHttpConfig Builder = new XPHttpConfig();

        private static HttpStatusCode[] _defaultHttStatuspCodeForRetry = { HttpStatusCode.ServiceUnavailable };

        public string BaseUrl { get; set; } = string.Empty;

        public Dictionary<string, string> DefaultHeaders { get; set; } = new Dictionary<string, string>();

        public int TimeOut { get; set; } = 30;

        public int RetryTimes { get; set; } = 3;

        public List<HttpStatusCode> RetryForHttpStatusCodes { get; set; } = new List<HttpStatusCode>(_defaultHttStatuspCodeForRetry);

        public Func<DateTime, string> DateFormatter { get; set; } = dateTime => { return dateTime.ToString("yyyy-MM-dd"); };

        public IHttpFilter CustomHttpFilter { get; set; }

        public XPHttpConfig SetBaseUrl(string baseUrl)
        {
            BaseUrl = baseUrl;
            return this;
        }

        public XPHttpConfig SetDefaultHeaders(string name, string value)
        {
            DefaultHeaders[name] = value;
            return this;
        }

        public XPHttpConfig SetTimeOut(int timeOut)
        {
            TimeOut = timeOut;
            return this;
        }

        public XPHttpConfig SetRetryTimes(int retryTimes)
        {
            RetryTimes = retryTimes;
            return this;
        }

        public XPHttpConfig SetDateFormatter(Func<DateTime, string> formatter)
        {
            DateFormatter = formatter;
            return this;
        }

        public XPHttpConfig SetHttpFilter(IHttpFilter httpFilter)
        {
            CustomHttpFilter = httpFilter;
            return this;
        }
    }
}
