using System;
using Cysharp.Threading.Tasks;
using Firebase.RemoteConfig;
// using Manager;
using R3;
using SDK;
using Sirenix.OdinInspector;
using TW.Utility.DesignPattern;
using UnityEngine;
using UnityEngine.Events;
#pragma warning disable CS0162 // Unreachable code detected

public class AdsController : Singleton<AdsController>
{
    public static Action EventShowBanner { get; set; }
    public static Action EventHideBanner { get; set; }
    
    [field: SerializeField] public float InterCapping {get; private set;}
    [field: SerializeField] public float InterCappingAfterReward {get; private set;}
    [field: SerializeField] public float InterCooldown {get; private set;}
    [field: SerializeField] public bool InterReady { get; private set; } = true;
    [field: SerializeField] public static bool InterJustShowed { get; set; } = false;

    // Load inter uu tien trong capping (MaxMediationController): con capping > InterPriorityFallbackBefore giay thi chi
    // thu InterPriorityUnits unit dau, ca nhom no-fill thi doi InterPriorityRetryDelay giay roi thu lai. 0 unit = tat.
    // Remote Config key inter_priority_config ghi de gia tri nay; ten field giong ads_config cua Skewer:
    // {"interPriorityUnits":2,"interPriorityRetryDelay":20,"interPriorityFallbackBefore":10}.
    [field: SerializeField] public int InterPriorityUnits { get; private set; } = 2;
    [field: SerializeField] public float InterPriorityRetryDelay { get; private set; } = 20f;
    [field: SerializeField] public float InterPriorityFallbackBefore { get; private set; } = 10f;

    [Serializable]
    private class InterPriorityConfig
    {
        public int interPriorityUnits = 2;
        public float interPriorityRetryDelay = 20f;
        public float interPriorityFallbackBefore = 10f;
    }
    private const string PrefKeyHasShownFirstInterEver = "ads_has_shown_first_inter_ever";
    private bool HasShownFirstInterEver
    {
        get => PlayerPrefs.GetInt(PrefKeyHasShownFirstInterEver, 0) == 1;
        set { PlayerPrefs.SetInt(PrefKeyHasShownFirstInterEver, value ? 1 : 0); PlayerPrefs.Save(); }
    }
    private bool _firstInterAlreadyShownAtLaunch;
    private int _interSkippedThisReopen;
    // Giá trị remote gốc, giữ riêng để luật riêng của game (SDKGameHooks) chồng lên mà không mất gốc.
    private float _baseInterCapping;
    private float _baseInterCappingAfterReward;

    protected override void Awake()
    {
        base.Awake();
        AddEvent();
        Initialize();
    }
    private void OnDestroy()
    {
        RemoveEvent();
    }

    public void Initialize()
    {
        // Chốt trạng thái đầu lần mở game: nếu inter đầu tiên đã show từ lần mở trước
        // thì lần mở này sẽ áp dụng skip-first-N.
        _firstInterAlreadyShownAtLaunch = HasShownFirstInterEver;
        _interSkippedThisReopen = 0;
        // Giữ giá trị authored trong scene làm gốc cho tới khi GetRemoteConfig() kéo được giá trị remote,
        // để ad show sớm (trước khi remote config về) không bị đặt capping-after-reward về 0.
        _baseInterCapping = InterCapping;
        _baseInterCappingAfterReward = InterCappingAfterReward;

        EventAdsManager.ShowInterstitialAds += ShowAdsInter;
        EventAdsManager.ShowRewardAds += ShowAdsReward;
        EventAdsManager.ShowBannerAds += ShowBanner;
    }

    public void GetRemoteConfig()
    {
        _baseInterCapping = (float)FirebaseManager.Instance.GetConfigValue(Keys.key_inter_capping).DoubleValue;
        _baseInterCappingAfterReward = (float)FirebaseManager.Instance.GetConfigValue(Keys.key_inter_capping_after_reward).DoubleValue;
        RefreshInterCapping();
        LoadInterPriorityConfig();
    }

    private void LoadInterPriorityConfig()
    {
        string json = FirebaseManager.Instance.GetConfigValue(Keys.key_inter_priority_config).StringValue;
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            InterPriorityConfig config = JsonUtility.FromJson<InterPriorityConfig>(json);
            if (config == null) return;
            InterPriorityUnits = Mathf.Max(0, config.interPriorityUnits);
            InterPriorityRetryDelay = Mathf.Max(1f, config.interPriorityRetryDelay);
            InterPriorityFallbackBefore = Mathf.Max(0f, config.interPriorityFallbackBefore);
            SDKDebugLogger.Log($"[INTER] inter_priority_config: units={InterPriorityUnits} retry={InterPriorityRetryDelay}s fallbackBefore={InterPriorityFallbackBefore}s");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"inter_priority_config JSON loi, giu gia tri mac dinh: {e.Message}");
        }
    }

    private void RefreshInterCapping()
    {
        // Luật riêng của game (theo level, campaign, đã mua IAP...) chồng lên giá trị remote gốc qua SDKGameHooks.
        // Tính lại ở mỗi lần show thay vì chốt một lần lúc boot, để dữ liệu về muộn (campaign...) vẫn kịp áp.
        InterCapping = SDKGameHooks.ModifyInterCapping?.Invoke(_baseInterCapping) ?? _baseInterCapping;
        InterCappingAfterReward = SDKGameHooks.ModifyInterCappingAfterReward?.Invoke(_baseInterCappingAfterReward)
                                  ?? _baseInterCappingAfterReward;
    }

    /// <summary>Trần số lần show inter trong ngày do game quy định (SDKGameHooks.CanShowInterToday).</summary>
    private static bool IsUnderDailyInterLimit()
    {
        return SDKGameHooks.CanShowInterToday?.Invoke() ?? true;
    }

    private static bool IsRemoveAds()
    {
        return SDKGameHooks.IsRemoveAds?.Invoke() ?? false;
    }

    private void ClearEvent()
    {
        EventShowBanner = null;
        EventHideBanner = null;
    }
    
    public void AddEvent()
    {
        ClearEvent();
        EventShowBanner += ShowBanner;
        EventHideBanner += HideBanner;
    }
    public void RemoveEvent()
    {
        EventShowBanner -= ShowBanner;
        EventHideBanner -= HideBanner;
    }

    public bool CheckCanShowAds()
    {
        return true;
    }

    public void ShowBanner(bool isShow)
    {
        if (isShow)
        {
            ShowBanner();
        }
        else
        {
            HideBanner();
        }
    }
    
    [Button]
    public void ShowAdsReward(Action successAction, Action failAction, Action closeAction, bool isSkipCapping = false, string eventName = "", params AnalyticsParameter[] parameters)
    {
        RefreshInterCapping();
        AppsFlyerManager.TrackRewarded_ClickShowButton();
        EventTrackingManager.TrackEventFirebase?.Invoke("ads_reward_clicked", parameters);
        AdsManager.Instance.ShowRewardVideo(
        () =>
            {
                successAction?.Invoke();
                InterCooldown += InterCappingAfterReward;
                InterCooldown = Mathf.Clamp(InterCooldown, 0, InterCapping);
                TrackLevelAdsView();
                EventTrackingManager.TrackEventFirebase?.Invoke(eventName, parameters);
                AppsFlyerManager.TrackRewarded_Displayed();
            },
            () =>
            {
                closeAction?.Invoke();
            },
            () =>
            {
                FailToShowdAds(failAction);
            }
        );
    }

    private void FailToShowdAds(Action failAction = null)
    {
        failAction?.Invoke();

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.Log("Fail to show reward ads because of no internet connection");
            // ActivityBlockContext.Events.CallNotification?.Invoke(528);
        }
        else
        {
            Debug.Log("Fail to show reward ads because of other reason");
            // ActivityBlockContext.Events.CallNotification?.Invoke(535);
        }
    }
    
    
    [Button]
    public void ShowAdsInter(Action successAction, Action failAction, Action closeAction, Action skipAction, bool isSkipCapping = false, bool isBackfillInter = false, string eventName = "", params AnalyticsParameter[] parameters)
    {
        RefreshInterCapping();
        if (IsRemoveAds() && !isBackfillInter)
        {
            successAction?.Invoke();
            skipAction?.Invoke();
            return;
        }

        // Trần inter/ngày là trần cứng nên isSkipCapping không vượt được; chỉ backfill đi xuyên,
        // giống cách nó đã đi xuyên check remove-ads ở trên.
        if (!isBackfillInter && !IsUnderDailyInterLimit())
        {
            successAction?.Invoke();
            skipAction?.Invoke();
            return;
        }

        if (!InterReady && !isSkipCapping)
        {
            successAction?.Invoke();
            skipAction?.Invoke();
            return;
        }

        // Skip N lần show inter đầu mỗi khi mở lại game (chỉ áp dụng cho inter capping thường,
        // không áp cho backfill/skip-capping vốn là show ép). Lần inter đầu tiên (ever) không bao giờ bị skip.
        if (!isSkipCapping && !isBackfillInter && _firstInterAlreadyShownAtLaunch)
        {
            int skipFirstCount = SDKGameHooks.GetSkipFirstInterCountOnReopen?.Invoke() ?? 0;
            if (_interSkippedThisReopen < skipFirstCount)
            {
                _interSkippedThisReopen++;
                // Bỏ qua lần này, không set cooldown để lần eligible kế tiếp vẫn được xét.
                successAction?.Invoke();
                skipAction?.Invoke();
                return;
            }
        }

        AppsFlyerManager.TrackInterstitial_ClickShowButton();
        AdsManager.Instance.ShowInterstitial(
            () =>
            {
                InterCooldown = InterCapping;
                if (!HasShownFirstInterEver) HasShownFirstInterEver = true;
                // Đếm tập trung ở đây (trần inter / ngày, analytics...) thay vì rải ở từng màn gọi inter.
                SDKGameHooks.OnInterstitialShown?.Invoke();
                successAction?.Invoke();
                InterCooldown = Mathf.Clamp(InterCooldown, 0, InterCapping);
                TrackLevelAdsView();
                EventTrackingManager.TrackEventFirebase?.Invoke(eventName, parameters);
                AppsFlyerManager.TrackInterstitial_Displayed();
            },
            () =>
            {
                closeAction?.Invoke();
            },
            () =>
            {
                FailToShowdAds(failAction);
            }
        );
        InterCooldown = InterCapping;
        InterJustShowed = true;
    }
    [Button]
    public void ShowBanner()
    {
        // Chặn tập trung ở đây để phủ mọi chỗ gọi ShowBanner. Hide chứ không chỉ return, để banner đang hiện biến mất
        // ngay khi user vừa mua gói xoá quảng cáo giữa session hoặc luật riêng của game (SDKGameHooks.IsBannerAllowed) chặn.
        if (IsRemoveAds() || !(SDKGameHooks.IsBannerAllowed?.Invoke() ?? true))
        {
            AdsManager.Instance.HideBannerAds();
            return;
        }
        AdsManager.Instance.ShowBannerAds();
    }
    [Button]
    public void HideBanner()
    {
        AdsManager.Instance.HideBannerAds();
    }

    private void Update()
    {
        if (InterCooldown > 0)
        {
            InterCooldown -= Time.deltaTime;
        }   
        InterReady = InterCooldown <= 0;
    }

    private static void TrackLevelAdsView()
    {
        SDKGameHooks.OnAdViewed?.Invoke();
    }
}
