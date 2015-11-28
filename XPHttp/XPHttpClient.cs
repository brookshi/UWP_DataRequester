﻿#region License
//   Copyright 2015 Brook Shi
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 
#endregion

using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;
using XPHttp.HttpFilter;
using XPHttp.Serializer;

namespace XPHttp
{
    public class XPHttpClient
    {
        public static readonly XPHttpClient DefaultClient = new XPHttpClient();

        private HttpClient _httpClient;

        private HttpRetryFilter _httpRetryFilter;

        private HttpBaseProtocolFilter _baseFilter = new HttpBaseProtocolFilter();

        public XPHttpClientConfig HttpConfig { get; private set; }

        public XPRequestParam RequestParamBuilder {
            get
            {
                return new XPRequestParam();
            }
        }

        public XPHttpClient()
        {
            _httpRetryFilter = new HttpRetryFilter();
            _httpClient = new HttpClient(_httpRetryFilter);
             HttpConfig = new XPHttpClientConfig(_httpClient, _httpRetryFilter, ApplyConfig);
            ApplyConfig();
        }

        void ApplyConfig()
        {
            _httpRetryFilter.RetryTimes = HttpConfig.RetryTimes;
            _httpRetryFilter.RetryHttpCodes = HttpConfig.HttpStatusCodesForRetry;
            HttpConfig.CustomHttpFilter.InnerFilter = _baseFilter;
        }

        string BuildUrl(string functionUrl, XPRequestParam param)
        {
            var url = HttpConfig.BaseUrl + functionUrl;
            if (param != null)
            {
                foreach (var segment in param.UrlSegments)
                {
                    url = url.Replace("{" + segment.Key + "}", segment.Value.UrlEncoding());
                }

                foreach (var queryString in param.QueryStrings)
                {
                    url = url.AppendQueryString(queryString);
                }
            }

            return url;
        }

        void ConfigRequest(HttpRequestMessage request, XPRequestParam httpParam)
        {
            httpParam.ApplyToRequester(request);
        }

        public void GetAsync(string functionUrl, XPRequestParam httpParam, IResponseHandler responseHandler)
        {
            SendRequestAsync(HttpMethod.Get, functionUrl, httpParam, responseHandler);
        }

        public void PostAsync(string functionUrl, XPRequestParam httpParam, IResponseHandler responseHandler)
        {
            SendRequestAsync(HttpMethod.Post, functionUrl, httpParam, responseHandler);
        }

        public void PutAsync(string functionUrl, XPRequestParam httpParam, IResponseHandler responseHandler)
        {
            SendRequestAsync(HttpMethod.Put, functionUrl, httpParam, responseHandler);
        }

        public void DeleteAsync(string functionUrl, XPRequestParam httpParam, IResponseHandler responseHandler)
        {
            SendRequestAsync(HttpMethod.Delete, functionUrl, httpParam, responseHandler);
        }

        public void PatchAsync(string functionUrl, XPRequestParam httpParam, IResponseHandler responseHandler)
        {
            SendRequestAsync(HttpMethod.Patch, functionUrl, httpParam, responseHandler);
        }

        public async Task<T> GetAsync<T>(string functionUrl, XPRequestParam httpParam, Action<HttpProgress> onProgress = null, Action<HttpRequestMessage> onCancel = null)
        {
            return await SendRequestAsync<T>(HttpMethod.Get, functionUrl, httpParam, onProgress, onCancel);
        }

        public async Task<T> PostAsync<T>(string functionUrl, XPRequestParam httpParam, Action<HttpProgress> onProgress = null, Action<HttpRequestMessage> onCancel = null)
        {
            return await SendRequestAsync<T>(HttpMethod.Post, functionUrl, httpParam, onProgress, onCancel);
        }

        public async Task<T> PutAsync<T>(string functionUrl, XPRequestParam httpParam, Action<HttpProgress> onProgress = null, Action<HttpRequestMessage> onCancel = null)
        {
            return await SendRequestAsync<T>(HttpMethod.Put, functionUrl, httpParam, onProgress, onCancel);
        }

        public async Task<T> DeleteAsync<T>(string functionUrl, XPRequestParam httpParam, Action<HttpProgress> onProgress = null, Action<HttpRequestMessage> onCancel = null)
        {
            return await SendRequestAsync<T>(HttpMethod.Delete, functionUrl, httpParam, onProgress, onCancel);
        }

        public async Task<T> PatchAsync<T>(string functionUrl, XPRequestParam httpParam, Action<HttpProgress> onProgress = null, Action<HttpRequestMessage> onCancel = null)
        {
            return await SendRequestAsync<T>(HttpMethod.Patch, functionUrl, httpParam, onProgress, onCancel);
        }


        public async void SendRequestAsync(HttpMethod httpMethod, string functionUrl, XPRequestParam httpParam, IResponseHandler responseHandler)
        {
            HttpRequestMessage request = new HttpRequestMessage(httpMethod, new Uri(BuildUrl(functionUrl, httpParam)));

            ConfigRequest(request, httpParam);

            IProgress<HttpProgress> progress = new Progress<HttpProgress>(p=> { if(responseHandler.OnProgress != null) responseHandler.OnProgress(p); });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            if (HttpConfig.TimeOut != int.MaxValue && HttpConfig.TimeOut > 0)
            {
                cancellationTokenSource.CancelAfter(HttpConfig.TimeOut * 1000);
            }

            HttpResponseMessage response = null;
            try
            {
                response = await _httpClient.SendRequestAsync(request).AsTask(cancellationTokenSource.Token, progress);
                responseHandler.Handle(response);
            }
            catch (TaskCanceledException)
            {
                if(responseHandler.OnCancel != null)
                {
                    responseHandler.OnCancel(request);
                }
            }
            catch (Exception ex)
            {
                if (responseHandler.OnFailed != null)
                {
                    responseHandler.OnFailed(new HttpResponseMessage() { Content = new HttpStringContent(ex.ToString()) });
                }
            }
        }

        public async Task<T> SendRequestAsync<T>(HttpMethod httpMethod, string functionUrl, XPRequestParam httpParam, Action<HttpProgress> onProgress, Action<HttpRequestMessage> onCancel)
        {
            HttpRequestMessage request = new HttpRequestMessage(httpMethod, new Uri(BuildUrl(functionUrl, httpParam)));

            ConfigRequest(request, httpParam);

            IProgress<HttpProgress> progress = new Progress<HttpProgress>(p => { if (onProgress != null) onProgress(p); });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            if (HttpConfig.TimeOut != int.MaxValue && HttpConfig.TimeOut > 0)
            {
                cancellationTokenSource.CancelAfter(HttpConfig.TimeOut * 1000);
            }

            try
            {
                return await _httpClient.SendRequestAsync(request).AsTask(cancellationTokenSource.Token, progress).ContinueWith(async responseTask =>
                {
                    var response = responseTask.Result;
                    if (!response.IsSuccessStatusCode)
                        return default(T);

                    var content = await response.Content.ReadAsStringAsync();
                    var serializer = SerializerFactory.GetSerializer(response.Content.Headers.ContentType.MediaType);

                    return serializer.Deserialize<T>(content);
                }).Unwrap();
            }
            catch (TaskCanceledException)
            {
                if(onCancel != null)
                    onCancel(request);

                return default(T);
            }
            catch(Exception)
            {
                return default(T);
            }
        }
    }
}
