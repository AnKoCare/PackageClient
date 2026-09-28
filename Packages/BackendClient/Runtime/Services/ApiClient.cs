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
        IEnumerator GetRaw(string endpoint, Action<string, int, string> onSuccess, Action<ErrorResponse> onError);
        IEnumerator Post<T>(string endpoint, object data, Action<ApiResponse<T>> onSuccess, Action<ErrorResponse> onError, IReadOnlyDictionary<string, string> extraHeaders = null);
        IEnumerator Put<T>(string endpoint, object data, Action<ApiResponse<T>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator Patch<T>(string endpoint, object data, Action<ApiResponse<T>> onSuccess, Action<ErrorResponse> onError);
        IEnumerator Delete<T>(string endpoint, Action<ApiResponse<T>> onSuccess, Action<ErrorResponse> onError);
    }

    public class ApiClient : IApiClient
    {
        private const string HttpVerbPatch = "PATCH";
        /// <summary>
        /// Console Editor xử lý chuỗi rất chậm với payload lớn (vd. leaderboard top 1000 ~250KB),
        /// nên chỉ log phần đầu body.
        /// </summary>
        private const int MaxLoggedBodyLength = 2000;

        private string authToken;
        private readonly string baseUrl;

        /// <summary>
        /// Phiên bản app gắn vào mọi request (header <c>X-App-Version</c>). Server dùng nó
        /// để không trả bản cloud save do một bản game mới hơn ghi — bản cũ đọc vào là
        /// hỏng dữ liệu. Đọc một lần vì <c>Application.version</c> chỉ gọi được trên
        /// main thread.
        /// </summary>
        private static string appVersion;

        private static string AppVersion
        {
            get
            {
                if (string.IsNullOrEmpty(appVersion))
                {
                    try { appVersion = Application.version; }
                    catch { appVersion = string.Empty; }
                }
                return appVersion;
            }
        }

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

#if UNITY_EDITOR
        private static string TruncateForLog(string body)
        {
            if (string.IsNullOrEmpty(body) || body.Length <= MaxLoggedBodyLength)
                return body;

            return body.Substring(0, MaxLoggedBodyLength) +
                   $"… (+{body.Length - MaxLoggedBodyLength} ký tự)";
        }
#endif

        public IEnumerator Get<T>(string endpoint, Action<ApiResponse<T>> onSuccess, Action<ErrorResponse> onError)
        {
            yield return SendRequest<T>(UnityWebRequest.kHttpVerbGET, endpoint, null, onSuccess, onError, null);
        }

        public IEnumerator GetRaw(string endpoint, Action<string, int, string> onSuccess, Action<ErrorResponse> onError)
        {
            yield return SendRawRequest(UnityWebRequest.kHttpVerbGET, endpoint, null, onSuccess, onError);
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
            request.SetRequestHeader(ApiConstants.APP_VERSION_HEADER, AppVersion);

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
#if UNITY_EDITOR
                    Debug.Log($"HTTP {method} {url} -> {(int)request.responseCode}\nBody: {TruncateForLog(responseText)}");
#endif

                    // Không có dữ liệu: JSON literal `null` (vd. GET /leaderboard/rank khi
                    // chưa có rank) hoặc body RỖNG — Nest gửi body rỗng cho mọi handler trả
                    // về null/undefined. Cả hai đều là câu trả lời hợp lệ "không có gì", phải
                    // báo success + data null; để rơi xuống nhánh parse thì JsonUtility ném
                    // lỗi với chuỗi rỗng và caller hiểu nhầm thành request hỏng.
                    string bodyText = responseText?.Trim();
                    if ((string.IsNullOrEmpty(bodyText) || string.Equals(bodyText, "null", StringComparison.Ordinal))
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
#if UNITY_EDITOR
                            Debug.LogError($"Error fallback parsing body to {typeof(T).Name}: {innerEx.Message}");
#endif
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
#if UNITY_EDITOR
                    Debug.LogError($"Error parsing response: {ex.Message}");
#endif
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
#if UNITY_EDITOR
                    Debug.LogError($"HTTP {method} {url} FAILED -> {(int)request.responseCode} {request.error}\nBody: {errorText}");
#endif
					ErrorResponse errorResponse = JsonUtility.FromJson<ErrorResponse>(errorText);
                    errorResponse.statusCode = (int)request.responseCode;
					errorResponse.responseDate = responseDateHeader;
                    onError?.Invoke(errorResponse);
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogError($"Error parsing error response: {ex.Message}");
#endif
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

        private IEnumerator SendRawRequest(string method, string endpoint, object data, Action<string, int, string> onSuccess, Action<ErrorResponse> onError)
        {
            string url = baseUrl + endpoint;
            UnityWebRequest request = new UnityWebRequest(url, method);

            request.SetRequestHeader(ApiConstants.CONTENT_TYPE_HEADER, ApiConstants.CONTENT_TYPE_JSON);
            request.SetRequestHeader("Accept", ApiConstants.CONTENT_TYPE_JSON);
            request.SetRequestHeader(ApiConstants.APP_VERSION_HEADER, AppVersion);

            if (!string.IsNullOrEmpty(authToken))
            {
                request.SetRequestHeader(ApiConstants.AUTHORIZATION_HEADER, ApiConstants.BEARER_PREFIX + authToken);
            }

            if (data != null && (method == UnityWebRequest.kHttpVerbPOST || method == UnityWebRequest.kHttpVerbPUT || method == HttpVerbPatch))
            {
                string jsonData = SimpleJsonSerializer.ToJson(data);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();

            string responseDateHeader = request.GetResponseHeader("Date");

            if (request.result == UnityWebRequest.Result.Success)
            {
#if UNITY_EDITOR
                Debug.Log($"HTTP {method} {url} -> {(int)request.responseCode}\nBody: {TruncateForLog(request.downloadHandler.text)}");
#endif
                onSuccess?.Invoke(request.downloadHandler.text, (int)request.responseCode, responseDateHeader);
            }
            else
            {
                try
                {
                    string errorText = request.downloadHandler.text;
#if UNITY_EDITOR
                    Debug.LogError($"HTTP {method} {url} FAILED -> {(int)request.responseCode} {request.error}\nBody: {errorText}");
#endif
                    ErrorResponse errorResponse = JsonUtility.FromJson<ErrorResponse>(errorText);
                    errorResponse.statusCode = (int)request.responseCode;
                    errorResponse.responseDate = responseDateHeader;
                    onError?.Invoke(errorResponse);
                }
                catch (Exception ex)
                {
                    onError?.Invoke(new ErrorResponse
                    {
                        success = false,
                        message = request.error ?? "Unknown error",
                        error = ex.Message,
                        statusCode = (int)request.responseCode,
                        responseDate = responseDateHeader
                    });
                }
            }

            request.Dispose();
        }
    }
}
