using System;
using UnityEngine;

namespace GameBackendModule.Models
{
    /// <summary>
    /// Body của <c>POST /api/v1/iap/android/verify</c>. Trường nào để null / rỗng thì
    /// không gửi lên (server từ chối field lạ nhưng chấp nhận field vắng).
    /// </summary>
    public class AndroidPurchaseVerifyRequest
    {
        /// <summary>Bắt buộc. Thường là <c>Application.identifier</c>.</summary>
        public string packageName;

        /// <summary>Bắt buộc. SKU Google Play.</summary>
        public string productId;

        /// <summary>Bắt buộc. Lấy từ receipt bằng <see cref="GooglePlayReceipt.TryParse"/>.</summary>
        public string purchaseToken;

        public string orderId;

        /// <summary>Giá store hiển thị (<c>product.metadata.localizedPrice</c>). Null = không gửi.</summary>
        public decimal? price;

        /// <summary>ISO 4217, vd "USD" (<c>product.metadata.isoCurrencyCode</c>).</summary>
        public string currency;

        public string appVersion;

        /// <summary>Play Integrity token — server nhận nhưng chưa dùng.</summary>
        public string integrityToken;

        public string requestHash;
    }

    /// <summary>Kết quả xác thực một đơn Google Play.</summary>
    [Serializable]
    public class AndroidPurchaseVerifyResponse
    {
        /// <summary>
        /// <see cref="IapVerifyStatus"/>: VALID / INVALID_TOKEN / PENDING / UNVERIFIED.
        /// </summary>
        public string status;

        /// <summary>
        /// Lý do chi tiết. VALID: PURCHASED / TEST_PURCHASE / PROMO / REWARDED.
        /// INVALID_TOKEN: NOT_FOUND / CANCELED / TOKEN_REUSED / PRODUCT_MISMATCH /
        /// ORDER_ID_MISMATCH / PACKAGE_MISMATCH. PENDING: PAYMENT_PENDING.
        /// UNVERIFIED: GOOGLE_UNAVAILABLE / GOOGLE_CONFIG_ERROR.
        /// </summary>
        public string reason;

        /// <summary>Server cho cộng quà (chỉ khi VALID). UNVERIFIED do client tự quyết.</summary>
        public bool grantEligible;

        /// <summary>Được báo doanh thu (đơn mua thật đã thanh toán).</summary>
        public bool revenueEligible;

        /// <summary>PURCHASE / TEST / PROMO / REWARDED, rỗng khi không VALID.</summary>
        public string purchaseKind;

        /// <summary>Mã vùng thanh toán Google trả về, vd "DE".</summary>
        public string regionCode;

        /// <summary>True nếu server trả lại kết quả đã lưu của cùng token.</summary>
        public bool fromCache;

        public bool IsValid => status == IapVerifyStatus.Valid;
        public bool IsInvalidToken => status == IapVerifyStatus.InvalidToken;
        public bool IsPending => status == IapVerifyStatus.Pending;
        public bool IsUnverified => status == IapVerifyStatus.Unverified;
    }

    public static class IapVerifyStatus
    {
        public const string Valid = "VALID";
        public const string InvalidToken = "INVALID_TOKEN";
        public const string Pending = "PENDING";
        public const string Unverified = "UNVERIFIED";
    }

    /// <summary>
    /// Đọc receipt Unity IAP của Google Play:
    /// <c>{"Store":"GooglePlay","TransactionID":"…","Payload":"{\"json\":\"{…purchaseToken…}\",\"signature\":\"…\"}"}</c>.
    /// Receipt của FakeStore (<c>"Store":"fake"</c>) không có purchaseToken → trả false.
    /// </summary>
    public static class GooglePlayReceipt
    {
        // Các field dưới do JsonUtility gán.
#pragma warning disable 0649
        [Serializable]
        private class UnityReceipt
        {
            public string Store;
            public string TransactionID;
            public string Payload;
        }

        [Serializable]
        private class GooglePayload
        {
            public string json;
        }

        [Serializable]
        private class GooglePurchaseJson
        {
            public string orderId;
            public string packageName;
            public string productId;
            public string purchaseToken;
        }
#pragma warning restore 0649

        public static bool TryParse(
            string unityReceipt,
            out string purchaseToken,
            out string orderId,
            out string productId,
            out string packageName)
        {
            purchaseToken = orderId = productId = packageName = null;
            if (string.IsNullOrEmpty(unityReceipt)) return false;
            try
            {
                var wrapper = JsonUtility.FromJson<UnityReceipt>(unityReceipt);
                if (wrapper == null || wrapper.Store != "GooglePlay" || string.IsNullOrEmpty(wrapper.Payload)) return false;

                var payload = JsonUtility.FromJson<GooglePayload>(wrapper.Payload);
                if (payload == null || string.IsNullOrEmpty(payload.json)) return false;

                var purchase = JsonUtility.FromJson<GooglePurchaseJson>(payload.json);
                if (purchase == null || string.IsNullOrEmpty(purchase.purchaseToken)) return false;

                purchaseToken = purchase.purchaseToken;
                orderId = string.IsNullOrEmpty(purchase.orderId) ? null : purchase.orderId;
                productId = purchase.productId;
                packageName = purchase.packageName;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
