using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using GameBackendModule.Models;

namespace GameBackendModule.Services
{
    public interface IApiClient
    {
        void SetAuthToken(string token);
        void ClearAuthToken();
        IEnumerator Get<T>(string endpoint, Action<ApiResponse<T>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator Post<T>(string endpoint, object data, Action<ApiResponse<T>> onSuccess, Action<ErrorResponse> onError, IReadOnlyDictionary<string, string> extraHeaders = null);
        IEnumerator Put<T>(string endpoint, object data, Action<ApiResponse<T>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator Patch<T>(string endpoint, object data, Action<ApiResponse<T>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator Delete<T>(string endpoint, Action<ApiResponse<T>> onSuccess, Action<ErrorResponse> onError);
    }

    public class ApiClient : IApiClient
    {
        private const string HttpVerbPatch = "PATCH";

        private string authToken;
        private readonly string baseUrl;

        public ApiClient(string baseUrl = ApiConstants.BASE_URL)
        {
            this.baseUrl = baseUrl;
        }

        public void SetAuthToken(string token)
        {
            authToken = token;
        }

        public void ClearAuthToken()
        {
            authToken = null;
        }

        public IEnumerator Get<T>(string endpoint, Action<ApiResponse<T>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return SendRequest<T>(UnityWebRequest.kHttpVerbGET, endpoint, null, onSuccess, onError, null);
        }

        public IEnumerator Post<T>(string endpoint, object data, Action<ApiResponse<T>> onSuccess, Action<ErrorResponse> onError, IReadOnlyDictionary<string, string> extraHeaders = null)
        {
            yield return SendRequest<T>(UnityWebRequest.kHttpVerbPOST, endpoint, data, onSuccess, onError, extraHeaders);
        }

        public IEnumerator Put<T>(string endpoint, object data, Action<ApiResponse<T>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return SendRequest<T>(UnityWebRequest.kHttpVerbPUT, endpoint, data, onSuccess, onError, null);
        }

        public IEnumerator Patch<T>(string endpoint, object data, Action<ApiResponse<T>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return SendRequest<T>(HttpVerbPatch, endpoint, data, onSuccess, onError, null);
        }

        public IEnumerator Delete<T>(string endpoint, Action<ApiResponse<T>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return SendRequest<T>(UnityWebRequest.kHttpVerbDELETE, endpoint, null, onSuccess, onError, null);
        }

        private IEnumerator SendRequest<T>(string method, string endpoint, object data, Action<ApiResponse<T>> onSuccess, Action<ErrorResponse> onError, IReadOnlyDictionary<string, string> extraHeaders)
        {
            string url = baseUrl + endpoint;
            UnityWebRequest request = new UnityWebRequest(url, method);

            // Set headers
            request.SetRequestHeader(ApiConstants.CONTENT_TYPE_HEADER, ApiConstants.CONTENT_TYPE_JSON);
            request.SetRequestHeader("Accept", ApiConstants.CONTENT_TYPE_JSON);
            
            if (!string.IsNullOrEmpty(authToken))
            {
                request.SetRequestHeader(ApiConstants.AUTHORIZATION_HEADER, ApiConstants.BEARER_PREFIX + authToken);
            }

            if (extraHeaders != null)
            {
                foreach (var pair in extraHeaders)
                {
                    if (!string.IsNullOrEmpty(pair.Key) && !string.IsNullOrEmpty(pair.Value))
                        request.SetRequestHeader(pair.Key, pair.Value);
                }
            }

            // Set request body for POST/PUT/PATCH requests
            if (data != null && (method == UnityWebRequest.kHttpVerbPOST || method == UnityWebRequest.kHttpVerbPUT || method == HttpVerbPatch))
            {
                // JsonUtility không hỗ trợ Dictionary<string, object>.
                // Dùng serializer tùy biến để đảm bảo field data là object hợp lệ.
                string jsonData = SimpleJsonSerializer.ToJson(data);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

			// Lấy thời gian từ header phản hồi (nếu server trả về)
			string responseDateHeader = request.GetResponseHeader("Date");

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"HTTP {method} {url} -> {(int)request.responseCode}\nBody: {responseText}");

                    // JSON literal `null` (vd. GET /leaderboard/rank khi không có rank)
                    if (string.Equals(responseText?.Trim(), "null", StringComparison.Ordinal)
                        && !typeof(T).IsValueType
                        && typeof(T) != typeof(string))
                    {
                        onSuccess?.Invoke(new ApiResponse<T>
                        {
                            success = true,
                            message = string.Empty,
                            data = default(T),
                            statusCode = (int)request.responseCode,
                            responseDate = responseDateHeader,
                        });
                        request.Dispose();
                        yield break;
                    }

                    ApiResponse<T> response = null;

                    // Thử parse theo dạng ApiResponse<T>
                    try { response = JsonUtility.FromJson<ApiResponse<T>>(responseText); }
                    catch { response = null; }

                    // Nếu chưa có data hợp lệ, thử parse trực tiếp T
                    bool needsFallback = response == null || response.success == false || EqualityComparer<T>.Default.Equals(response.data, default(T));
                    if (needsFallback)
                    {
                        T parsed = default(T);
                        try { parsed = JsonUtility.FromJson<T>(responseText); }
                        catch (Exception innerEx)
                        {
                            Debug.LogError($"Error fallback parsing body to {typeof(T).Name}: {innerEx.Message}");
                        }

                        if (!EqualityComparer<T>.Default.Equals(parsed, default(T)))
                        {
                            response = new ApiResponse<T>
                            {
                                success = true,
                                message = string.Empty,
                                data = parsed,
                                statusCode = (int)request.responseCode
                            };
                        }
                        else if (response == null)
                        {
                            // tạo vỏ rỗng để không null
                            response = new ApiResponse<T>
                            {
                                success = false,
                                message = string.Empty,
                                data = default(T),
                                statusCode = (int)request.responseCode
                            };
                        }
                    }

					// Đảm bảo gán statusCode và thời gian phản hồi nếu parse được ApiResponse
					if (response != null)
					{
						response.statusCode = (int)request.responseCode;
						response.responseDate = responseDateHeader;
					}

                    onSuccess?.Invoke(response);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error parsing response: {ex.Message}");
                    ErrorResponse errorResponse = new ErrorResponse
                    {
                        success = false,
                        message = "Failed to parse response",
                        error = ex.Message,
                        statusCode = (int)request.responseCode == 0 ? 500 : (int)request.responseCode
                    };
                    onError?.Invoke(errorResponse);
                }
            }
            else
            {
                try
                {
                    string errorText = request.downloadHandler.text;
                    Debug.LogError($"HTTP {method} {url} FAILED -> {(int)request.responseCode} {request.error}\nBody: {errorText}");
					ErrorResponse errorResponse = JsonUtility.FromJson<ErrorResponse>(errorText);
                    errorResponse.statusCode = (int)request.responseCode;
					errorResponse.responseDate = responseDateHeader;
                    onError?.Invoke(errorResponse);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error parsing error response: {ex.Message}");
					ErrorResponse errorResponse = new ErrorResponse
                    {
                        success = false,
                        message = request.error ?? "Unknown error",
                        error = ex.Message,
						statusCode = (int)request.responseCode,
						responseDate = responseDateHeader
                    };
                    onError?.Invoke(errorResponse);
                }
            }

            request.Dispose();
        }
    }
}
