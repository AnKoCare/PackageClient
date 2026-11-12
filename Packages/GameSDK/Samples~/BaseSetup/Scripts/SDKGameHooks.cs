using System;
using System.Collections.Generic;

namespace SDK
{
    /// <summary>
    /// Điểm nối giữa sample SDK và code riêng của từng game. Sample chỉ gọi qua các hook này nên compile được ở mọi
    /// project; game gán hook lúc khởi động (trước khi Ads / IAP / AppsFlyer chạy). Hook để null thì dùng hành vi mặc định
    /// ghi trong comment của từng hook.
    /// </summary>
    public static class SDKGameHooks
    {
        #region Ads

        /// <summary>Người chơi đã mua gói xoá quảng cáo. Null = false.</summary>
        public static Func<bool> IsRemoveAds;

        /// <summary>Nhận capping inter gốc từ remote (key_inter_capping), trả capping thực dùng (theo level, campaign...). Null = giữ nguyên.</summary>
        public static Func<float, float> ModifyInterCapping;

        /// <summary>Như ModifyInterCapping cho capping sau khi xem reward (key_inter_capping_after_reward). Null = giữ nguyên.</summary>
        public static Func<float, float> ModifyInterCappingAfterReward;

        /// <summary>Còn được show inter trong ngày không (trần inter / ngày). Backfill inter bỏ qua kiểm tra này. Null = true.</summary>
        public static Func<bool> CanShowInterToday;

        /// <summary>Số lần inter đầu tiên bị bỏ qua mỗi lần mở lại game (không áp cho inter đầu tiên từ trước tới nay). Null = 0.</summary>
        public static Func<int> GetSkipFirstInterCountOnReopen;

        /// <summary>Luật riêng của game có cho hiện banner lúc này không (level bắt đầu hiện, campaign...). Null = true.</summary>
        public static Func<bool> IsBannerAllowed;

        /// <summary>Gọi mỗi lần inter hiện xong (đếm inter / ngày, đánh dấu analytics...).</summary>
        public static Action OnInterstitialShown;

        /// <summary>Gọi mỗi lần xem xong một quảng cáo inter hoặc reward (đếm quảng cáo trong level...).</summary>
        public static Action OnAdViewed;

        /// <summary>Doanh thu một impression (USD, ad_format) — vd. value bidding Google Ads.</summary>
        public static Action<double, string> OnAdRevenuePaid;

#if UNITY_AD_MAX
        /// <summary>Doanh thu impression của MAX kèm AdInfo gốc (adUnitId, adInfo) — vd. ABILibsCustomEvent, TikTok.</summary>
        public static Action<string, MaxSdkBase.AdInfo> OnMaxAdRevenuePaid;
#endif

        #endregion

        #region UI

        /// <summary>Hiện thông báo lỗi cho người chơi (không có quảng cáo, store không kết nối...).</summary>
        public static Action<string> ShowError;

        /// <summary>Chặn thao tác trong lúc chờ store xử lý giao dịch.</summary>
        public static Action ShowLoadingBlock;

        public static Action HideLoadingBlock;

        #endregion

        #region IAP

        /// <summary>Product id consumable thêm vào catalog ngoài ShopGlobalConfig (vd. SKU Play Games Rewards).</summary>
        public static Func<IEnumerable<string>> GetExtraConsumableProductIds;

        /// <summary>
        /// Giao dịch đặc biệt (vd. phần thưởng Play Games 0đ): game tự cộng đồ và trả true thì sample bỏ qua tính doanh thu
        /// và bỏ qua giao hàng thường. Null hoặc trả false = giao dịch thường.
        /// </summary>
        public static Func<string, bool> TryHandleSpecialProduct;

        /// <summary>
        /// Giao đồ theo product id khi không có callback trong phiên mua (giao dịch pending / deferred / restore / bị ngắt
        /// được store trả lại ở phiên sau). Null = không giao được, chỉ log.
        /// </summary>
        public static Action<string> GrantProductById;

        #endregion

        #region AppsFlyer campaign

        /// <summary>Chuẩn hoá tên campaign AppsFlyer trả về (đổi tên camp cũ, cắt độ dài...). Null = giữ nguyên.</summary>
        public static Func<string, string> NormalizeCampaign;

        /// <summary>AppsFlyer trả campaign (campaign, campaignId, lần đầu nhận campaign trên máy này) — vd. lưu config, fetch lại remote.</summary>
        public static Action<string, string, bool> OnCampaignReceived;

        #endregion
    }
}
