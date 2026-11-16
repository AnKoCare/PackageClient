using System;
using System.Collections;
using System.Collections.Generic;
using GameBackendModule.Models;

namespace GameBackendModule.Services
{
    public interface IIapService
    {
        /// <summary>
        /// POST /iap/android/verify — Bearer JWT. Server hỏi Google Play xem purchaseToken có
        /// thật không. Lỗi mạng / timeout / 401 đi vào <paramref name="onError"/>: client coi
        /// như UNVERIFIED và áp chính sách của mình.
        /// </summary>
        IEnumerator VerifyAndroidPurchase(
            AndroidPurchaseVerifyRequest request,
            Action<ApiResponse<AndroidPurchaseVerifyResponse>> onSuccess,
            Action<ErrorResponse> onError,
            int timeoutSeconds = IapService.DefaultTimeoutSeconds);
    }

    public class IapService : IIapService
    {
        /// <summary>Server tự timeout Google sau 8 giây; client chờ thêm một chút.</summary>
        public const int DefaultTimeoutSeconds = 10;

        private readonly IApiClient apiClient;

        public IapService(IApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public IEnumerator VerifyAndroidPurchase(
            AndroidPurchaseVerifyRequest request,
            Action<ApiResponse<AndroidPurchaseVerifyResponse>> onSuccess,
            Action<ErrorResponse> onError,
            int timeoutSeconds = DefaultTimeoutSeconds)
        {
            if (request == null
                || string.IsNullOrEmpty(request.packageName)
                || string.IsNullOrEmpty(request.productId)
                || string.IsNullOrEmpty(request.purchaseToken))
            {
                onError?.Invoke(new ErrorResponse
                {
                    success = false,
                    message = "packageName, productId và purchaseToken là bắt buộc",
                    statusCode = 400,
                });
                yield break;
            }

            yield return apiClient.PostWithTimeout<AndroidPurchaseVerifyResponse>(
                ApiConstants.IAP_ANDROID_VERIFY_ENDPOINT,
                BuildBody(request),
                timeoutSeconds,
                onSuccess,
                onError);
        }

        /// <summary>Chỉ gửi field có giá trị: server dùng whitelist, field null/rỗng bỏ đi cho gọn.</summary>
        private static Dictionary<string, object> BuildBody(AndroidPurchaseVerifyRequest r)
        {
            var body = new Dictionary<string, object>
            {
                { "packageName", r.packageName },
                { "productId", r.productId },
                { "purchaseToken", r.purchaseToken },
            };
            AddIfSet(body, "orderId", r.orderId);
            if (r.price.HasValue && r.price.Value >= 0) body["price"] = r.price.Value;
            if (!string.IsNullOrEmpty(r.currency) && r.currency.Length == 3) body["currency"] = r.currency.ToUpperInvariant();
            AddIfSet(body, "appVersion", r.appVersion);
            AddIfSet(body, "integrityToken", r.integrityToken);
            AddIfSet(body, "requestHash", r.requestHash);
            return body;
        }

        private static void AddIfSet(Dictionary<string, object> body, string key, string value)
        {
            if (!string.IsNullOrEmpty(value)) body[key] = value;
        }
    }
}
