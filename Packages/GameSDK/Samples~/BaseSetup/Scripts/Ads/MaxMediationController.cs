using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using SDK.Analytics;
using UnityEngine;
using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace SDK {
    public class MaxMediationController : AdsMediationController
    {
#if UNITY_AD_MAX
        private const float LOAD_AD_TIMEOUT_SECONDS = 30f;
        private const float LOAD_AD_FIRST_DELAY_SECONDS = 1f;

        public MaxAdSetup m_MaxAdConfig;
        [FoldoutGroup("Rewarded")] [ShowInInspector] private Dictionary<string, bool> m_RewardedReadyMap = new Dictionary<string, bool>();
        [FoldoutGroup("Rewarded")] [ShowInInspector] private List<string> m_RewardedOrder = new List<string>();
        [FoldoutGroup("Rewarded")] [ShowInInspector] private int m_CurrentLoadingIndexReward = 0;
        [FoldoutGroup("Rewarded")] [ShowInInspector] private bool isWaitLoadRewardedUntilAvailable = false;
        [FoldoutGroup("Rewarded")] [ShowInInspector] private double revenueRewarded = 0;
        private CancellationTokenSource m_RewardedLoadTimeoutCts;
        private string m_CurrentLoadingRewardedAdUnitId;

        [FoldoutGroup("Interstitial")] [ShowInInspector] private Dictionary<string, bool> m_InterstitialReadyMap = new Dictionary<string, bool>();
        [FoldoutGroup("Interstitial")] [ShowInInspector] private List<string> m_InterstitialOrder = new List<string>();
        [FoldoutGroup("Interstitial")] [ShowInInspector] private int m_CurrentLoadingIndexInterstitial = 0;
        [FoldoutGroup("Interstitial")] [ShowInInspector] private bool isWaitLoadInterstitialUntilAvailable = false;
        [FoldoutGroup("Interstitial")] [ShowInInspector] private double revenueInterstitial = 0;
        private CancellationTokenSource m_InterstitialLoadTimeoutCts;
        private string m_CurrentLoadingInterstitialAdUnitId;

        // Uu tien load N unit inter dau trong cua so capping (port tu Skewer, 2026-11-12):
        //  * con capping > fallbackBefore giay -> chi thu N unit dau (unit floor cao); unit nao co ads thi giu san, het
        //    capping moi show; ca N unit no-fill -> doi retryDelay giay roi thu lai tu unit 1;
        //  * den moc con fallbackBefore giay ma van chua co ads -> roi xuong luong thuong, load tiep unit N+1..het;
        //  * con capping <= fallbackBefore giay (hoac het capping) -> luong thuong, load tuan tu unit 1 -> het.
        // Tham so lay tu AdsController (InterPriorityUnits / RetryDelay / FallbackBefore); day la gia tri du phong.
        private const int INTER_PRIORITY_UNITS_DEFAULT = 2;
        private const float INTER_PRIORITY_RETRY_DELAY_DEFAULT = 20f;
        private const float INTER_PRIORITY_FALLBACK_BEFORE_DEFAULT = 10f;
        [FoldoutGroup("Interstitial")] [ShowInInspector] private bool m_IsInterstitialPriorityMode = false;
        private CancellationTokenSource m_InterstitialPriorityRetryCts;
        private CancellationTokenSource m_InterstitialPriorityDeadlineCts;

        public override void Init()
        {
            if (IsInited) return;
            base.Init();
            SDKDebugLogger.Log("unity-script: MyAppStart Start called");
            MaxSdk.SetExtraParameter("disable_all_logs", "true");

            // Disable MAX SDK retry mechanism - chi ap cho interstitial/rewarded (set theo tung ad unit
            // trong BuildInterstitialOrder/BuildRewardedOrder). Truoc day set global khien banner cung
            // mat co che tu retry cua MAX, nen khi vong retry tu viet bi ket thi banner khong con duong cuu.

            // Disable back-to-back ad loading
            // MaxSdk.SetExtraParameter("disable_back_to_back_loading", "true");

            // Initialize InMobi Publisher Signals Manager
            // ABIInMobiPublisherSignalsManager.Initialize();
            // ABIInMobiPublisherSignalsManager.SendAllPublisherSignals();

            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                // AppLovin SDK is initialized, configure and start loading ads.
                SDKDebugLogger.Log("MAX SDK Initialized");
                AdsManager.Instance.InitAdsType(AdsMediationType.MAX);
                //MaxSdk.ShowMediationDebugger();
            };
            // Khong tu set consent o day: MAX Terms and Privacy Policy Flow dang bat nen
            // MAX lay lua chon that cua nguoi choi tu Google UMP. Ep SetHasUserConsent(true)
            // se bao "da dong y" cho ca nguoi o EU vua bam tu choi.
            MaxSdk.InitializeSdk();
            MaxSdk.SetVerboseLogging(false);
        }

        private void OnAdRevenuePaidEvent(AdsType adsType, string adUnitId, MaxSdkBase.AdInfo impressionData)
        {
            double revenue = impressionData.Revenue;
            ImpressionData impression = new ImpressionData
            {
                ad_mediation = AdsMediationType.MAX,
                ad_source = impressionData.NetworkName,
                ad_unit_name = impressionData.AdUnitIdentifier,
                ad_format = impressionData.AdFormat,
                ad_revenue = revenue,
                ad_currency = "USD",
                ad_type = impressionData.AdFormat
            };
            AdRevenuePaidCallback?.Invoke(impression);

            // Tracker riêng của game cần AdInfo gốc của MAX (vd. ABILibsCustomEvent TROAS / Bamboo, TikTok)
            SDKGameHooks.OnMaxAdRevenuePaid?.Invoke(adUnitId, impressionData);



            // AnalyticsManager.LogAdsRevenue(impression, adsType);

            // Track ad impression for InMobi signals
            // ABIInMobiPublisherSignalsManager.TrackAdImpression(adsType, revenue);
        }
        #region Interstitial
        public override void InitInterstitialAd(Action adClosedCallback, Action adLoadSuccessCallback, Action adLoadFailedCallback, Action adShowSuccessCallback, Action adShowFailCallback)
        {
            base.InitInterstitialAd(adClosedCallback, adLoadSuccessCallback, adLoadFailedCallback, adShowSuccessCallback, adShowFailCallback);
            SDKDebugLogger.Log("Init MAX Interstitial");
            // Attach callbacks
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialFailedToDisplayEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialDismissedEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += (adUnitID, adInfo) => { OnAdRevenuePaidEvent(AdsType.INTERSTITIAL, adUnitID, adInfo); };
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialAdShowSucceededEvent;

            PreloadAllInterstitialAds();
        }


        private void BuildInterstitialOrder()
        {
            m_InterstitialOrder.Clear();
            m_InterstitialReadyMap.Clear();
            if (m_MaxAdConfig.interstitialAdsUnit != null && m_MaxAdConfig.interstitialAdsUnit.Count > 0)
            {
                for (int i = 0; i < m_MaxAdConfig.interstitialAdsUnit.Count && i < 5; i++)
                {
                    string id = m_MaxAdConfig.interstitialAdsUnit[i];
                    if (!string.IsNullOrEmpty(id))
                    {
                        m_InterstitialOrder.Add(id);
                        m_InterstitialReadyMap.Add(id, false);
                        MaxSdk.SetInterstitialExtraParameter(id, "disable_auto_retries", "true");
                    }
                }
            }

            if (m_InterstitialOrder.Count == 0 && !string.IsNullOrEmpty(m_MaxAdConfig.InterstitialAdUnitID))
            {
                m_InterstitialOrder.Add(m_MaxAdConfig.InterstitialAdUnitID);
                m_InterstitialReadyMap.Add(m_MaxAdConfig.InterstitialAdUnitID, false);
                MaxSdk.SetInterstitialExtraParameter(m_MaxAdConfig.InterstitialAdUnitID, "disable_auto_retries", "true");
            }
        }

        private void PreloadAllInterstitialAds()
        {
            BuildInterstitialOrder();

            LoadInterstitialFirst();
        }

        private void LoadInterstitialFirst(bool isSkipDelay = false)
        {
            if (!isSkipDelay)
            {
                DelayLoadInterstitialFirst().Forget();
                return;
            }

            if (isWaitLoadInterstitialUntilAvailable) return;

            // Bug fix: tranh ArgumentOutOfRangeException khi order rong
            if (m_InterstitialOrder == null || m_InterstitialOrder.Count == 0)
            {
                BuildInterstitialOrder();
            }
            if (m_InterstitialOrder.Count == 0)
            {
                InterLog("Khong co interstitial ad unit nao duoc cau hinh -> khong load");
                isWaitLoadInterstitialUntilAvailable = false;
                return;
            }

            isWaitLoadInterstitialUntilAvailable = true;

            SDKDebugLogger.Log($"LoadInterstitialFirst");

            // Moi lan quay ve index 0 danh gia lai: con capping > nguong thi chi thu N unit dau
            CancelInterstitialPriorityRetry();
            EvaluateInterstitialPriorityMode();

            m_CurrentLoadingIndexInterstitial = 0;

            string adUnitId = m_InterstitialOrder[m_CurrentLoadingIndexInterstitial];

            // Bug4 fix: dung MaxSdk lam nguon su that thay vi map noi bo
            bool isReady = MaxSdk.IsInterstitialReady(adUnitId);
            m_InterstitialReadyMap[adUnitId] = isReady;

            if (!isReady)
            {
                m_CurrentLoadingInterstitialAdUnitId = adUnitId;
                InterLog($"LOAD {InterUnitLabel(0)}");
                MaxSdk.LoadInterstitial(adUnitId);
                StartInterstitialLoadTimeout();
            }
            else
            {
                m_CurrentLoadingInterstitialAdUnitId = null;
                InterLog($"{InterUnitLabel(0)} da co ads san -> khong load them, cho show");
                isWaitLoadInterstitialUntilAvailable = false;
                StopInterstitialPriority();
            }
        }

        private async UniTaskVoid DelayLoadInterstitialFirst()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(LOAD_AD_FIRST_DELAY_SECONDS));
            LoadInterstitialFirst(true);
        }

        private void LoadInterstitialAtIndex(int index)
        {
            SDKDebugLogger.Log($"LoadInterstitialAtIndex: {index}");
            if (index < 0 || index >= m_InterstitialOrder.Count)
            {
                SDKDebugLogger.Log($"LoadInterstitialAtIndex aborted: index {index} out of range (count {m_InterstitialOrder.Count})");
                isWaitLoadInterstitialUntilAvailable = false;
                return;
            }
            string adUnitId = m_InterstitialOrder[index];
            bool isReady = MaxSdk.IsInterstitialReady(adUnitId);
            m_InterstitialReadyMap[adUnitId] = isReady;

            if (!m_InterstitialReadyMap[adUnitId])
            {
                m_CurrentLoadingIndexInterstitial = index;
                m_CurrentLoadingInterstitialAdUnitId = adUnitId;
                InterLog($"LOAD {InterUnitLabel(index)}");
                MaxSdk.LoadInterstitial(adUnitId);
                StartInterstitialLoadTimeout();
            }
            else
            {
                m_CurrentLoadingInterstitialAdUnitId = null;
                isWaitLoadInterstitialUntilAvailable = false;
                InterLog($"{InterUnitLabel(index)} da co ads san -> khong load them, cho show");
                StopInterstitialPriority();
            }
        }

        private void StartInterstitialLoadTimeout()
        {
            CancelInterstitialLoadTimeout();
            m_InterstitialLoadTimeoutCts = new CancellationTokenSource();
            InterstitialLoadTimeoutAsync(m_InterstitialLoadTimeoutCts.Token).Forget();
        }

        private void CancelInterstitialLoadTimeout()
        {
            if (m_InterstitialLoadTimeoutCts == null) return;
            m_InterstitialLoadTimeoutCts.Cancel();
            m_InterstitialLoadTimeoutCts.Dispose();
            m_InterstitialLoadTimeoutCts = null;
        }

        private async UniTaskVoid InterstitialLoadTimeoutAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(LOAD_AD_TIMEOUT_SECONDS), cancellationToken: token);
                InterLog($"TIMEOUT {LOAD_AD_TIMEOUT_SECONDS:F0}s {InterUnitLabel(m_CurrentLoadingIndexInterstitial)} khong tra loi -> coi nhu no-fill");
                // Bug1 fix: timeout thi di tiep index ke tiep, khong reset ve 0
                m_CurrentLoadingInterstitialAdUnitId = null;
                ContinueInterstitialAfterNoFill();
            }
            catch (OperationCanceledException)
            {
            }
        }

        #region Interstitial priority (capping window)

        // Log luong inter, chi hien khi bat SDK_DEBUG_ENABLED trong SDKDebugLogger (release dang tat).
        private void InterLog(string message)
        {
            string mode = m_IsInterstitialPriorityMode ? "PRIORITY" : "NORMAL";
            SDKDebugLogger.Log($"[INTER] {message} | capping={GetInterCappingRemaining():F0}s mode={mode}");
        }

        private string InterUnitLabel(int index)
        {
            if (index < 0 || index >= m_InterstitialOrder.Count) return $"unit ?/{m_InterstitialOrder.Count}";
            return $"unit {index + 1}/{m_InterstitialOrder.Count} ({m_InterstitialOrder[index]})";
        }

        private string InterUnitLabel(string adUnitId)
        {
            return InterUnitLabel(m_InterstitialOrder.IndexOf(adUnitId));
        }

        // Capping inter thuc te cua game nam o AdsController.InterCooldown (giay, giam theo Time.deltaTime; dat lai khi show
        // inter, cong them InterCappingAfterReward khi xem xong rewarded).
        private float GetInterCappingRemaining()
        {
            AdsController controller = AdsController.Instance;
            if (controller == null) return 0f;
            return Mathf.Max(0f, controller.InterCooldown);
        }

        private void GetInterPriorityConfig(out int units, out float retryDelay, out float fallbackBefore)
        {
            // Tham so o AdsController (gia tri trong scene, ghi de boi Remote Config key inter_priority_config).
            AdsController controller = AdsController.Instance;
            units = controller != null ? controller.InterPriorityUnits : INTER_PRIORITY_UNITS_DEFAULT;
            retryDelay = controller != null ? controller.InterPriorityRetryDelay : INTER_PRIORITY_RETRY_DELAY_DEFAULT;
            fallbackBefore = controller != null ? controller.InterPriorityFallbackBefore : INTER_PRIORITY_FALLBACK_BEFORE_DEFAULT;

            units = Mathf.Clamp(units, 0, m_InterstitialOrder.Count);
            retryDelay = Mathf.Max(1f, retryDelay);
            fallbackBefore = Mathf.Max(0f, fallbackBefore);
        }

        // Goi moi lan pipeline quay ve index 0. Con capping > nguong fallback -> chi thu N unit dau.
        private void EvaluateInterstitialPriorityMode()
        {
            GetInterPriorityConfig(out int units, out _, out float fallbackBefore);
            float remaining = GetInterCappingRemaining();
            m_IsInterstitialPriorityMode = units > 0 && remaining > fallbackBefore;

            if (m_IsInterstitialPriorityMode)
            {
                InterLog($"PRIORITY ON: capping con {remaining:F0}s > {fallbackBefore:F0}s -> chi thu {units} unit dau, moc fallback sau {remaining - fallbackBefore:F0}s");
                StartInterstitialPriorityDeadline(remaining - fallbackBefore);
            }
            else
            {
                CancelInterstitialPriorityDeadline();
                if (units <= 0)
                {
                    InterLog("NORMAL: interPriorityUnits = 0 (tat uu tien) -> load tuan tu tu unit 1");
                }
                else
                {
                    InterLog($"NORMAL: capping con {remaining:F0}s <= {fallbackBefore:F0}s -> load tuan tu tu unit 1");
                }
            }
        }

        // Da co inter san sang -> khong can uu tien nua, huy moi timer cua che do nay.
        private void StopInterstitialPriority()
        {
            m_IsInterstitialPriorityMode = false;
            CancelInterstitialPriorityDeadline();
            CancelInterstitialPriorityRetry();
        }

        // Goi sau khi unit dang load bao no-fill (fail callback hoac timeout). Quyet dinh buoc ke tiep.
        private void ContinueInterstitialAfterNoFill()
        {
            int nextIndex = m_CurrentLoadingIndexInterstitial + 1;

            if (m_IsInterstitialPriorityMode)
            {
                GetInterPriorityConfig(out int units, out float retryDelay, out float fallbackBefore);
                if (nextIndex >= units)
                {
                    float remaining = GetInterCappingRemaining();
                    if (remaining > fallbackBefore)
                    {
                        InterLog($"{units} unit dau deu no-fill, capping con {remaining:F0}s -> DOI {retryDelay:F0}s roi thu lai tu unit 1");
                        StartInterstitialPriorityRetry(retryDelay);
                        return;
                    }

                    m_IsInterstitialPriorityMode = false;
                    CancelInterstitialPriorityDeadline();
                    InterLog($"PRIORITY OFF: capping con {remaining:F0}s <= {fallbackBefore:F0}s -> FALLBACK load {InterUnitLabel(nextIndex)}");
                }
            }

            LoadInterstitialFromIndexOrRestart(nextIndex);
        }

        private void LoadInterstitialFromIndexOrRestart(int index)
        {
            if (index < m_InterstitialOrder.Count)
            {
                SDKDebugLogger.Log($"Loading next interstitial ad at index {index}");
                LoadInterstitialAtIndex(index);
            }
            else
            {
                InterLog($"Ca {m_InterstitialOrder.Count} unit deu no-fill -> doi {LOAD_AD_FIRST_DELAY_SECONDS:F0}s roi quay lai unit 1");
                isWaitLoadInterstitialUntilAvailable = false;
                LoadInterstitialFirst(); // restart co delay, tranh vong lap request lien tuc
            }
        }

        private void StartInterstitialPriorityRetry(float delaySeconds)
        {
            CancelInterstitialPriorityRetry();
            // Giu isWaitLoadInterstitialUntilAvailable = true trong luc doi de khong co pipeline thu hai chay song song
            m_CurrentLoadingInterstitialAdUnitId = null;
            m_InterstitialPriorityRetryCts = new CancellationTokenSource();
            InterstitialPriorityRetryAsync(delaySeconds, m_InterstitialPriorityRetryCts.Token).Forget();
        }

        private void CancelInterstitialPriorityRetry()
        {
            if (m_InterstitialPriorityRetryCts == null) return;
            m_InterstitialPriorityRetryCts.Cancel();
            m_InterstitialPriorityRetryCts.Dispose();
            m_InterstitialPriorityRetryCts = null;
        }

        private async UniTaskVoid InterstitialPriorityRetryAsync(float delaySeconds, CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (m_InterstitialPriorityRetryCts != null && m_InterstitialPriorityRetryCts.Token == token)
            {
                m_InterstitialPriorityRetryCts.Dispose();
                m_InterstitialPriorityRetryCts = null;
            }

            InterLog("Het thoi gian doi -> thu lai tu unit 1");
            isWaitLoadInterstitialUntilAvailable = false;
            LoadInterstitialFirst(true);
        }

        private void StartInterstitialPriorityDeadline(float seconds)
        {
            CancelInterstitialPriorityDeadline();
            m_InterstitialPriorityDeadlineCts = new CancellationTokenSource();
            InterstitialPriorityDeadlineAsync(Mathf.Max(0f, seconds), m_InterstitialPriorityDeadlineCts.Token).Forget();
        }

        private void CancelInterstitialPriorityDeadline()
        {
            if (m_InterstitialPriorityDeadlineCts == null) return;
            m_InterstitialPriorityDeadlineCts.Cancel();
            m_InterstitialPriorityDeadlineCts.Dispose();
            m_InterstitialPriorityDeadlineCts = null;
        }

        // Den moc "con fallbackBefore giay capping": neu van chua co ads thi roi xuong luong thuong tu unit N+1.
        private async UniTaskVoid InterstitialPriorityDeadlineAsync(float seconds, CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!m_IsInterstitialPriorityMode) return;
            if (IsInterstitialLoaded())
            {
                StopInterstitialPriority();
                return;
            }

            GetInterPriorityConfig(out int units, out _, out float fallbackBefore);
            float remaining = GetInterCappingRemaining();
            if (remaining > fallbackBefore)
            {
                // Rewarded vua cong them capping -> dat lai moc, van uu tien
                InterLog($"Capping duoc cong them (rewarded), con {remaining:F0}s -> dat lai moc fallback sau {remaining - fallbackBefore:F0}s, van uu tien");
                StartInterstitialPriorityDeadline(remaining - fallbackBefore);
                return;
            }

            m_IsInterstitialPriorityMode = false;
            if (m_InterstitialPriorityRetryCts != null)
            {
                // Dang doi giua cac vong uu tien -> cat ngan, load unit ke tiep ngay
                CancelInterstitialPriorityRetry();
                InterLog($"PRIORITY OFF (den moc): capping con {remaining:F0}s -> cat cho, FALLBACK load {InterUnitLabel(units)}");
                LoadInterstitialFromIndexOrRestart(units);
            }
            else
            {
                // Dang co request in-flight: callback/timeout sap toi se di tiep theo luong thuong
                InterLog($"PRIORITY OFF (den moc): capping con {remaining:F0}s, {InterUnitLabel(m_CurrentLoadingIndexInterstitial)} dang cho ket qua -> sau do di tiep luong thuong");
            }
        }

        #endregion

        private void ShowInterstitialReady()
        {
            SDKDebugLogger.Log("ShowInterstitialReady");
            for (int i = 0; i < m_InterstitialOrder.Count; i++)
            {
                string adUnitId = m_InterstitialOrder[i];
                if (MaxSdk.IsInterstitialReady(adUnitId))
                {
                    InterLog($"SHOW {InterUnitLabel(i)} placement={m_InterstitialPlacement}");
                    m_InterstitialReadyMap[adUnitId] = false;
                    MaxSdk.ShowInterstitial(adUnitId, m_InterstitialPlacement);
                    return;
                }
            }

            InterLog("KHONG co inter nao san de show -> bao fail, load lai tu unit 1");
            m_InterstitialAdShowFailCallback?.Invoke();
            LoadInterstitialFirst(true);
        }


        public override void RequestInterstitialAd() // not used
        {
            base.RequestInterstitialAd();
            SDKDebugLogger.Log("Request MAX Interstitial");
            if (m_InterstitialOrder == null || m_InterstitialOrder.Count == 0)
            {
                BuildInterstitialOrder();
            }

            LoadInterstitialFirst(true);
        }
        public override void ShowInterstitialAd(Action successCallback, Action failedCallback, string m_InterstitialPlacement = "interstitial")
        {
            base.ShowInterstitialAd(successCallback, failedCallback, m_InterstitialPlacement);
            SDKDebugLogger.Log("Show MAX Interstitial");
            ShowInterstitialReady();
        }
        public override bool IsInterstitialLoaded()
        {
            if (m_InterstitialOrder == null || m_InterstitialOrder.Count == 0)
            {
                BuildInterstitialOrder();
            }
            foreach (string adUnitId in m_InterstitialOrder)
            {
                if (MaxSdk.IsInterstitialReady(adUnitId))
                {
                    return true;
                }
            }
            return false;
        }

        private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            m_InterstitialReadyMap[adUnitId] = true;
            revenueInterstitial = adInfo.Revenue;

            EventManager.AddEventNextFrame(m_InterstitialAdLoadSuccessCallback);

            // Bug3 fix: callback stale (cua request cu) - khong duoc cancel timeout / dong pipeline hien tai
            if (adUnitId != m_CurrentLoadingInterstitialAdUnitId)
            {
                InterLog($"LOADED (stale) {InterUnitLabel(adUnitId)} network={adInfo.NetworkName} - request hien tai la {InterUnitLabel(m_CurrentLoadingInterstitialAdUnitId)}, giu ads nhung khong doi pipeline");
                return;
            }

            InterLog($"LOADED {InterUnitLabel(adUnitId)} network={adInfo.NetworkName} revenue={adInfo.Revenue} -> dung load, cho show");

            CancelInterstitialLoadTimeout();
            m_CurrentLoadingInterstitialAdUnitId = null;
            isWaitLoadInterstitialUntilAvailable = false;
            StopInterstitialPriority();
        }

        private void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            m_InterstitialReadyMap[adUnitId] = false;
            EventManager.AddEventNextFrame(m_InterstitialAdLoadFailCallback);

            // Bug3 fix: callback stale (cua request cu) - khong duoc cancel timeout / dong pipeline hien tai
            if (adUnitId != m_CurrentLoadingInterstitialAdUnitId)
            {
                InterLog($"NO-FILL (stale) {InterUnitLabel(adUnitId)} code={errorInfo.Code} - request hien tai la {InterUnitLabel(m_CurrentLoadingInterstitialAdUnitId)}, bo qua");
                return;
            }

            InterLog($"NO-FILL {InterUnitLabel(adUnitId)} code={errorInfo.Code} msg={errorInfo.Message}");

            CancelInterstitialLoadTimeout();
            m_CurrentLoadingInterstitialAdUnitId = null;
            ContinueInterstitialAfterNoFill();
        }

        private void InterstitialFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            InterLog($"SHOW FAIL {InterUnitLabel(adUnitId)} code={errorInfo.Code} msg={errorInfo.Message} -> load lai sau {LOAD_AD_FIRST_DELAY_SECONDS:F0}s");
            EventManager.AddEventNextFrame(m_InterstitialAdShowFailCallback);
            LoadInterstitialFirst();
        }

        private void OnInterstitialDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            InterLog($"Inter DONG {InterUnitLabel(adUnitId)} -> capping bat dau, load lai sau {LOAD_AD_FIRST_DELAY_SECONDS:F0}s");
            EventManager.AddEventNextFrame(m_InterstitialAdCloseCallback);
            LoadInterstitialFirst();
        }

        private void OnInterstitialAdShowSucceededEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SDKDebugLogger.Log("unity-script: I got InterstitialAdShowSuccess");

            // Send InMobi display signals (dir_r, dir_nn, dir_type)
            // ABIInMobiPublisherSignalsManager.OnAdDisplayed(adUnitId, adInfo);

            // Send updated publisher signals
            // ABIInMobiPublisherSignalsManager.SendAllPublisherSignals();

            EventManager.AddEventNextFrame(m_InterstitialAdShowSuccessCallback);
        }
        #endregion

        #region Rewards Video

        public override void InitRewardVideoAd(Action videoClosed, Action videoLoadSuccess, Action videoLoadFailed, Action videoStart) {
            base.InitRewardVideoAd(videoClosed, videoLoadSuccess, videoLoadFailed, videoStart);

            SDKDebugLogger.Log("Init MAX RewardedVideoAd");
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += Rewarded_OnAdStartedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += Rewarded_OnAdShowFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += Rewarded_OnAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += Rewarded_OnAdRewardedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += Rewarded_OnAdClosedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += Rewarded_OnAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += Rewarded_OnAdLoadedFailEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += (adUnitID, adInfo) => { OnAdRevenuePaidEvent(AdsType.REWARDED, adUnitID, adInfo);};
            PreloadAllRewardedAds();
        }

        private void BuildRewardedOrder()
        {
            m_RewardedOrder.Clear();
            m_RewardedReadyMap.Clear();
            if (m_MaxAdConfig.rewardAdsUnit != null && m_MaxAdConfig.rewardAdsUnit.Count > 0)
            {
                for (int i = 0; i < m_MaxAdConfig.rewardAdsUnit.Count && i < 5; i++)
                {
                    string id = m_MaxAdConfig.rewardAdsUnit[i];
                    if (!string.IsNullOrEmpty(id))
                    {
                        m_RewardedOrder.Add(id);
                        m_RewardedReadyMap.Add(id, false);
                        MaxSdk.SetRewardedAdExtraParameter(id, "disable_auto_retries", "true");
                    }
                }
            }

            if (m_RewardedOrder.Count == 0 && !string.IsNullOrEmpty(m_MaxAdConfig.RewardedAdUnitID))
            {
                m_RewardedOrder.Add(m_MaxAdConfig.RewardedAdUnitID);
                m_RewardedReadyMap.Add(m_MaxAdConfig.RewardedAdUnitID, false);
                MaxSdk.SetRewardedAdExtraParameter(m_MaxAdConfig.RewardedAdUnitID, "disable_auto_retries", "true");
            }
        }

        private void PreloadAllRewardedAds()
        {
            BuildRewardedOrder();

            LoadRewardedFirst();
        }

        private void LoadRewardedFirst(bool isSkipDelay = false)
        {
            if (!isSkipDelay)
            {
                DelayLoadRewardedFirst().Forget();
                return;
            }

            if (isWaitLoadRewardedUntilAvailable) return;

            // Bug fix: tranh ArgumentOutOfRangeException khi order rong
            if (m_RewardedOrder == null || m_RewardedOrder.Count == 0)
            {
                BuildRewardedOrder();
            }
            if (m_RewardedOrder.Count == 0)
            {
                SDKDebugLogger.Log("LoadRewardedFirst aborted: no rewarded ad units configured");
                isWaitLoadRewardedUntilAvailable = false;
                return;
            }

            isWaitLoadRewardedUntilAvailable = true;

            SDKDebugLogger.Log($"LoadRewardedFirst");

            m_CurrentLoadingIndexReward = 0;

            string adUnitId = m_RewardedOrder[m_CurrentLoadingIndexReward];

            // Bug4 fix: dung MaxSdk lam nguon su that thay vi map noi bo
            bool isReady = MaxSdk.IsRewardedAdReady(adUnitId);
            m_RewardedReadyMap[adUnitId] = isReady;

            if (!isReady)
            {
                m_CurrentLoadingRewardedAdUnitId = adUnitId;
                SDKDebugLogger.Log($"Start loading rewarded ad at index 0: {adUnitId}");
                MaxSdk.LoadRewardedAd(adUnitId);
                StartRewardedLoadTimeout();
            }
            else
            {
                m_CurrentLoadingRewardedAdUnitId = null;
                SDKDebugLogger.Log($"Rewarded ad at index 0 already ready: {adUnitId}");
                isWaitLoadRewardedUntilAvailable = false;
            }
        }

        private async UniTaskVoid DelayLoadRewardedFirst()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(LOAD_AD_FIRST_DELAY_SECONDS));
            LoadRewardedFirst(true);
        }

        private void LoadRewardedAtIndex(int index)
        {
            SDKDebugLogger.Log($"LoadRewardedAtIndex: {index}");
            if (index < 0 || index >= m_RewardedOrder.Count)
            {
                SDKDebugLogger.Log($"LoadRewardedAtIndex aborted: index {index} out of range (count {m_RewardedOrder.Count})");
                isWaitLoadRewardedUntilAvailable = false;
                return;
            }
            string adUnitId = m_RewardedOrder[index];
            bool isReady = MaxSdk.IsRewardedAdReady(adUnitId);
            m_RewardedReadyMap[adUnitId] = isReady;

            if (!m_RewardedReadyMap[adUnitId])
            {
                m_CurrentLoadingIndexReward = index;
                m_CurrentLoadingRewardedAdUnitId = adUnitId;
                SDKDebugLogger.Log($"Start loading rewarded ad at index {index}: {adUnitId}");
                MaxSdk.LoadRewardedAd(adUnitId);
                StartRewardedLoadTimeout();
            }
            else
            {
                m_CurrentLoadingRewardedAdUnitId = null;
                isWaitLoadRewardedUntilAvailable = false;
                SDKDebugLogger.Log($"Rewarded ad at index {index} already ready: {adUnitId}");
            }
        }

        private void StartRewardedLoadTimeout()
        {
            CancelRewardedLoadTimeout();
            m_RewardedLoadTimeoutCts = new CancellationTokenSource();
            RewardedLoadTimeoutAsync(m_RewardedLoadTimeoutCts.Token).Forget();
        }

        private void CancelRewardedLoadTimeout()
        {
            if (m_RewardedLoadTimeoutCts == null) return;
            m_RewardedLoadTimeoutCts.Cancel();
            m_RewardedLoadTimeoutCts.Dispose();
            m_RewardedLoadTimeoutCts = null;
        }

        private async UniTaskVoid RewardedLoadTimeoutAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(LOAD_AD_TIMEOUT_SECONDS), cancellationToken: token);
                SDKDebugLogger.Log($"Rewarded load timeout after {LOAD_AD_TIMEOUT_SECONDS}s at index {m_CurrentLoadingIndexReward} - moving to next ad unit");
                // Bug1 fix: timeout thi di tiep index ke tiep, khong reset ve 0
                m_CurrentLoadingRewardedAdUnitId = null;
                int nextIndex = m_CurrentLoadingIndexReward + 1;
                if (nextIndex < m_RewardedOrder.Count)
                {
                    LoadRewardedAtIndex(nextIndex);
                }
                else
                {
                    isWaitLoadRewardedUntilAvailable = false;
                    LoadRewardedFirst(); // restart tu index 0 sau delay
                }
            }
            catch (OperationCanceledException)
            {
            }
        }


        public override void RequestRewardVideoAd() // not used
        {
            base.RequestRewardVideoAd();
            SDKDebugLogger.Log("Request MAX RewardedVideoAd (sequential loading)");
            if (m_RewardedOrder == null || m_RewardedOrder.Count == 0)
            {
                BuildRewardedOrder();
            }

            LoadRewardedFirst(true);
        }

        public override void ShowRewardVideoAd(Action successCallback, Action failedCallback)
        {
            if (RewardReadyToShow() && IsInterstitialLoaded() && revenueInterstitial > revenueRewarded) {
                SDKDebugLogger.Log("[Check Ads] Show Interstitial because revenue interstitial is greater than revenue rewarded");
                ShowInterstitialAd(successCallback, failedCallback, "rewardreplace");
                return;
            }
            base.ShowRewardVideoAd(successCallback, failedCallback);
            ShowRewardReady();
        }
        public override bool IsRewardVideoLoaded() {
            if (m_RewardedOrder == null || m_RewardedOrder.Count == 0)
            {
                BuildRewardedOrder();
            }
            foreach (string adUnitId in m_RewardedOrder)
            {
                if (MaxSdk.IsRewardedAdReady(adUnitId))
                {
                    return true;
                }
            }
            return false;
        }

        /************* RewardedVideo Delegates *************/
        private void Rewarded_OnAdLoadedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            SDKDebugLogger.Log("RewardedVideoAd MAX Loaded Success: " + adUnitID);
            m_RewardedReadyMap[adUnitID] = true;
            revenueRewarded = adInfo.Revenue;
            SDKDebugLogger.Log("Revenue Rewarded: " + revenueRewarded);

            EventManager.AddEventNextFrame(m_RewardedVideoLoadSuccessCallback);

            // Bug3 fix: callback stale (cua request cu) - khong duoc cancel timeout / dong pipeline hien tai
            if (adUnitID != m_CurrentLoadingRewardedAdUnitId)
            {
                SDKDebugLogger.Log($"Stale rewarded loaded callback: {adUnitID}, current loading: {m_CurrentLoadingRewardedAdUnitId ?? "none"}");
                return;
            }

            CancelRewardedLoadTimeout();
            m_CurrentLoadingRewardedAdUnitId = null;
            isWaitLoadRewardedUntilAvailable = false;
        }
        private void Rewarded_OnAdLoadedFailEvent(string adUnitID, MaxSdkBase.ErrorInfo adError) {
            SDKDebugLogger.Log("RewardedVideoAd MAX Loaded Fail: " + adUnitID);
            m_RewardedReadyMap[adUnitID] = false;
            EventManager.AddEventNextFrame(m_RewardedVideoLoadFailedCallback);

            // Bug3 fix: callback stale (cua request cu) - khong duoc cancel timeout / dong pipeline hien tai
            if (adUnitID != m_CurrentLoadingRewardedAdUnitId)
            {
                SDKDebugLogger.Log($"Stale rewarded fail callback: {adUnitID}, current loading: {m_CurrentLoadingRewardedAdUnitId ?? "none"}");
                return;
            }

            CancelRewardedLoadTimeout();
            m_CurrentLoadingRewardedAdUnitId = null;

            int nextIndex = m_CurrentLoadingIndexReward + 1;
            if (nextIndex < m_RewardedOrder.Count)
            {
                SDKDebugLogger.Log($"Loading next rewarded ad at index {nextIndex}");
                LoadRewardedAtIndex(nextIndex);
            }
            else
            {
                SDKDebugLogger.Log("All rewarded ads failed to load - restart after delay");
                isWaitLoadRewardedUntilAvailable = false;
                LoadRewardedFirst(); // restart co delay, tranh vong lap request lien tuc
            }
        }

        private void Rewarded_OnAdRewardedEvent(string adUnitID, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
#if !UNITY_EDITOR
        SDKDebugLogger.Log("unity-script: I got RewardedVideoAdRewardedEvent");
#endif
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    {
                        if (m_RewardedVideoEarnSuccessCallback != null)
                        {
                            SDKDebugLogger.Log("Watch video Success Callback!");
                            EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
                            m_RewardedVideoEarnSuccessCallback = null;
                        }

                        break;
                    }
                case RuntimePlatform.IPhonePlayer:
                    {
                        if (m_RewardedVideoEarnSuccessCallback != null)
                        {
                            SDKDebugLogger.Log("Watch video Success Callback!");
                            EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
                            m_RewardedVideoEarnSuccessCallback = null;
                        }
                        break;
                    }
#if UNITY_EDITOR
                // Editor: Application.platform là OSXEditor/WindowsEditor nên không rơi vào 2 case trên,
                // success callback không bao giờ được gọi -> xem ad stub xong không nhận thưởng.
                default:
                    {
                        if (m_RewardedVideoEarnSuccessCallback != null)
                        {
                            EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
                            m_RewardedVideoEarnSuccessCallback = null;
                        }
                        break;
                    }
#endif
            }

            LoadRewardedFirst();
        }

        private void Rewarded_OnAdClosedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            SDKDebugLogger.Log("unity-script: I got RewardedVideoAdClosedEvent");
            // if (m_RewardedVideoEarnSuccessCallback != null)
            // {
            //     EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
            //     m_RewardedVideoEarnSuccessCallback = null;
            // }
            EventManager.AddEventNextFrame(() =>
            {
                m_RewardedVideoCloseCallback?.Invoke();
            });
            // m_RewardedVideoCloseCallback?.Invoke(m_IsWatchSuccess);

            LoadRewardedFirst();
        }

        private void Rewarded_OnAdStartedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            SDKDebugLogger.Log("unity-script: I got RewardedVideoAdStartedEvent");

            // Send InMobi display signals (dir_r, dir_nn, dir_type)
            // ABIInMobiPublisherSignalsManager.OnAdDisplayed(adUnitID, adInfo);

            // Send updated publisher signals
            // ABIInMobiPublisherSignalsManager.SendAllPublisherSignals();

            EventManager.AddEventNextFrame(m_RewardedVideoShowStartCallback);

        }

        private void RewardedVideoAdEndedEvent() {
            SDKDebugLogger.Log("unity-script: I got RewardedVideoAdEndedEvent");
        }

        private void Rewarded_OnAdShowFailedEvent(string adUnitID, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            SDKDebugLogger.Log("unity-script: I got RewardedVideoAdShowFailedEvent, code :  " + errorInfo.Code + ", description : " + errorInfo.Message);
            EventManager.AddEventNextFrame(m_RewardedVideoShowFailCallback);

            LoadRewardedFirst();
        }

        private void Rewarded_OnAdClickedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            SDKDebugLogger.Log("unity-script: I got RewardedVideoAdClickedEvent");

            // Track ad click for CTR calculation
            // ABIInMobiPublisherSignalsManager.TrackAdClick(AdsType.REWARDED);
        }
        #endregion

        private bool RewardReadyToShow()
        {
            for (int i = 0; i < m_RewardedOrder.Count; i++)
            {
                string adUnitId = m_RewardedOrder[i];
                if (MaxSdk.IsRewardedAdReady(adUnitId))
                {
                    return true;
                }
            }
            return false;
        }

        private void ShowRewardReady()
        {
            SDKDebugLogger.Log("ShowRewardReady");
            for (int i = 0; i < m_RewardedOrder.Count; i++)
            {
                string adUnitId = m_RewardedOrder[i];
                if (MaxSdk.IsRewardedAdReady(adUnitId))
                {
                    SDKDebugLogger.Log("[Check Ads] Show MAX Rewarded with AdUnit: " + adUnitId);
                    m_RewardedReadyMap[adUnitId] = false;
                    MaxSdk.ShowRewardedAd(adUnitId);
                    return;
                }
            }

            // if (GameDefaultConfig.EnableInterstitialFallback)
            // {
            // FallbackToInterstitial();
            // }
            // else
            // {
            EventManager.AddEventNextFrame(m_RewardedVideoShowFailCallback);
            LoadRewardedFirst(true);
            SDKGameHooks.ShowError?.Invoke("Ad unavailable, please try again later.");
            // }
        }

        private void FallbackToInterstitial()
        {
            // SDKDebugLogger.Log("[Check Ads] FallbackToInterstitial");
            // EventAdsManager.ShowInterstitialAds?.Invoke(
            //     () => { m_RewardedVideoEarnSuccessCallback?.Invoke(); },
            //     () => { m_RewardedVideoShowFailCallback?.Invoke(); },
            //     () => { m_RewardedVideoCloseCallback?.Invoke(); },
            //     false,
            //     false,
            //     "ads_fallback_to_interstitial",
            //     new AnalyticsParameter("placement", "rewarded_to_interstitial")
            // );

            // LoadRewardedFirst(true);
        }

        #region Banner

        public MaxSdkBase.BannerPosition m_BannerPosition;
        private bool m_IsBannerLoaded;
        private float maxBannerHeightPercentage = 0.15f;
        public override void InitBannerAds(
            Action bannerLoadedSuccessCallback,
            Action bannerAdLoadedFailCallback,
            Action bannerAdsCollapsedCallback,
            Action bannerAdsExpandedCallback,
            Action bannerAdsDisplayed,
            Action bannerAdsDisplayedFailedCallback,
            Action bannerAdsClickedCallback)
        {
            base.InitBannerAds(
                bannerLoadedSuccessCallback,
                bannerAdLoadedFailCallback,
                bannerAdsCollapsedCallback,
                bannerAdsExpandedCallback,
                bannerAdsDisplayed,
                bannerAdsDisplayedFailedCallback,
                bannerAdsClickedCallback);
            SDKDebugLogger.Log("Banner MAX Init ID = " + m_MaxAdConfig.BannerAdUnitID);
            // Chua co ad unit banner: MAX SDK nem IllegalArgumentException "No Ad Unit ID specified" tren main thread
            // (crash app) khi CreateBanner/LoadAd voi ID rong -> bo qua toan bo banner.
            if (string.IsNullOrEmpty(m_MaxAdConfig.BannerAdUnitID))
            {
                SDKDebugLogger.Log("Banner MAX: chua cau hinh ad unit -> bo qua");
                return;
            }
            MaxSdk.CreateBanner(m_MaxAdConfig.BannerAdUnitID, m_BannerPosition);
            //MaxSdk.SetBannerBackgroundColor(m_MaxAdConfig.BannerAdUnitID, Color.black);

            MaxSdk.SetBannerExtraParameter(m_MaxAdConfig.BannerAdUnitID, "adaptive_banner", "true");

            MaxSdkCallbacks.Banner.OnAdLoadedEvent += BannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += BannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += BannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += (adUnitID, adInfo) => { OnAdRevenuePaidEvent(AdsType.BANNER, adUnitID, adInfo); };
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerAdCollapsedEvent;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerAdExpandedEvent;
        }

        public override void RequestBannerAds()
        {
            base.RequestBannerAds();
            if (string.IsNullOrEmpty(m_MaxAdConfig.BannerAdUnitID)) return;
            MaxSdk.LoadBanner(m_MaxAdConfig.BannerAdUnitID);
        }


        public override void ShowBannerAds()
        {
            if (string.IsNullOrEmpty(m_MaxAdConfig.BannerAdUnitID)) return;

            // Khong chan theo m_IsBannerLoaded nua: MaxSdk.ShowBanner goi duoc ca khi chua co fill,
            // banner view se tu hien khi ad ve va auto-refresh cua MAX duoc bat lai.
            // Truoc day return im lang o day khien lenh show bi mat han, va khi banner load lai
            // thanh cong thi khong con ai goi ShowBanner nua -> banner bien mat vinh vien.
            if (!CheckBannerHeight())
            {
                return;
            }

            base.ShowBannerAds();
            SDKDebugLogger.Log("MAX Mediation Banner Call Show");
            MaxSdk.ShowBanner(m_MaxAdConfig.BannerAdUnitID);

            // MAX khong co event "banner displayed" -> tu ban callback de AdsManager biet banner dang hien
            EventManager.AddEventNextFrame(m_BannerAdsDisplayedCallback);
        }
        public override void HideBannerAds()
        {
            base.HideBannerAds();
            if (string.IsNullOrEmpty(m_MaxAdConfig.BannerAdUnitID)) return;
            SDKDebugLogger.Log("MAX Mediation Banner Call Hide");
            MaxSdk.HideBanner(m_MaxAdConfig.BannerAdUnitID);
        }

        public override void DestroyBannerAds()
        {
            base.DestroyBannerAds();
            if (string.IsNullOrEmpty(m_MaxAdConfig.BannerAdUnitID)) return;
            SDKDebugLogger.Log("MAX Mediation Banner Call Destroy");
            MaxSdk.DestroyBanner(m_MaxAdConfig.BannerAdUnitID);
            m_IsBannerLoaded = false;
        }

        public override bool IsBannerLoaded()
        {
            return m_IsBannerLoaded;
        }

        private void BannerAdLoadedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            SDKDebugLogger.Log($"MAX Mediation Banner Loaded Success - AdFormat: {adInfo.AdFormat}, Network: {adInfo.NetworkName}, " +
                               $"Placement: {adInfo.NetworkPlacement}, Creative: {adInfo.CreativeIdentifier}, DSP: {adInfo.DspName}");

            // Moi lan auto-refresh la mot creative moi -> ghi lai de log click/expand/redirect dung creative
            AdsManager.Instance?.BannerRedirectGuard?.OnCreativeLoaded(adInfo.NetworkName, adInfo.NetworkPlacement,
                adInfo.CreativeIdentifier, adInfo.DspName);

            // Dat co truoc khi ban callback: AdsManager.OnBannerLoadedSuccess se goi lai ShowBannerAds
            m_IsBannerLoaded = true;

            CheckBannerHeight();

            EventManager.AddEventNextFrame(m_BannerAdLoadedSuccessCallback);
        }
        private void BannerAdLoadFailedEvent(string adUnitID, MaxSdkBase.ErrorInfo errorInfo)
        {
            SDKDebugLogger.Log("MAX Mediation Banner Loaded Fail");
            EventManager.AddEventNextFrame(m_BannerAdLoadedFailCallback);
            // m_BannerAdLoadedFailCallback?.Invoke();
            m_IsBannerLoaded = false;
        }
        private void BannerAdClickedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            SDKDebugLogger.Log("MAX Mediation Banner Clicked");
            // MaxAnalyticsManager.TrackAdClicked("banner");

            // Track ad click for CTR calculation
            // ABIInMobiPublisherSignalsManager.TrackAdClick(AdsType.BANNER);

            EventManager.AddEventNextFrame(m_BannerAdsClickedCallback);
            // m_BannerAdsClickedCallback?.Invoke();
        }
        private void OnBannerAdCollapsedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            SDKDebugLogger.Log("MAX Mediation Banner Collapsed");
            EventManager.AddEventNextFrame(m_BannerAdsCollapsedCallback);
            // m_BannerAdsCollapsedCallback?.Invoke();
        }
        private void OnBannerAdExpandedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            SDKDebugLogger.Log("MAX Mediation Banner Expanded");
            EventManager.AddEventNextFrame(m_BannerAdsExpandedCallback);
            // m_BannerAdsExpandedCallback?.Invoke();
        }

        private bool CheckBannerHeight()
        {
            float bannerHeight = MaxSdkUtils.GetAdaptiveBannerHeight();
            float screenHeight = Screen.height;
            float safeAreaHeight = Screen.safeArea.height;

            float maxBannerHeightInPixels = screenHeight - safeAreaHeight;

            float maxAllowedHeight;
            if (maxBannerHeightInPixels > 0 && maxBannerHeightInPixels > screenHeight * maxBannerHeightPercentage)
            {
                // Dùng ngưỡng pixel cố định
                maxAllowedHeight = maxBannerHeightInPixels;
            }
            else
            {
                // Dùng % màn hình
                maxAllowedHeight = screenHeight * maxBannerHeightPercentage;
            }

            SDKDebugLogger.Log($"Banner Height: {bannerHeight}px, Screen Height: {screenHeight}px, Safe Area Height: {safeAreaHeight}px, Max Allowed: {maxAllowedHeight}px ({(maxAllowedHeight/screenHeight)*100:F1}%)");

            if (bannerHeight > maxAllowedHeight)
            {
                // Chi an banner, KHONG dat m_IsBannerLoaded = false va KHONG ban load-fail callback:
                // ad van load thanh cong, no chi qua cao de hien. Bao "load fail" o day lam hong
                // state machine retry ben AdsManager va khoa banner vinh vien.
                SDKDebugLogger.Log($"Banner quá cao ({bannerHeight}px > {maxAllowedHeight}px)! Chặn banner để không che nội dung game.");
                MaxSdk.HideBanner(m_MaxAdConfig.BannerAdUnitID);
                return false;
            }

            SDKDebugLogger.Log($"Banner hợp lệ ({bannerHeight}px <= {maxAllowedHeight}px). Cho phép hiển thị.");
            return true;
        }
        #endregion

        #region MREC
        private bool m_IsMRecLoaded = false;
        public override void InitRMecAds(Action adLoadedCallback, Action adLoadFailedCallback, Action adClickedCallback, Action adExpandedCallback, Action adCollapsedCallback)
        {
            base.InitRMecAds(adLoadedCallback, adLoadFailedCallback, adClickedCallback, adExpandedCallback, adCollapsedCallback);
            SDKDebugLogger.Log("MAX Start Init MREC");
            MaxSdkCallbacks.MRec.OnAdLoadedEvent      += OnMRecAdLoadedEvent;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent  += OnMRecAdLoadFailedEvent;
            MaxSdkCallbacks.MRec.OnAdClickedEvent     += OnMRecAdClickedEvent;
            MaxSdkCallbacks.MRec.OnAdExpandedEvent    += OnMRecAdExpandedEvent;
            MaxSdkCallbacks.MRec.OnAdCollapsedEvent   += OnMRecAdCollapsedEvent;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += (adUnitID, adInfo) => { OnAdRevenuePaidEvent(AdsType.MREC, adUnitID, adInfo);};
            // ID rong -> MAX crash ("No Ad Unit ID specified"), xem InitBannerAds.
            if (string.IsNullOrEmpty(m_MaxAdConfig.MrecAdUnitID)) return;
            MaxSdk.CreateMRec(m_MaxAdConfig.MrecAdUnitID, MaxSdkBase.AdViewPosition.BottomCenter);
        }
        public override bool IsMRecLoaded()
        {
            return m_IsMRecLoaded;
        }
        private void OnMRecAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            SDKDebugLogger.Log("MAX Mediation MREC Loaded Success");
            m_IsMRecLoaded = true;
            m_MRecAdLoadedCallback?.Invoke();
        }
        private void OnMRecAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo error)
        {
            SDKDebugLogger.Log("MAX Mediation MREC Loaded Fail");
            m_IsMRecLoaded = false;
            m_MRecAdLoadFailCallback?.Invoke();
        }
        private void OnMRecAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            m_MRecAdClickedCallback?.Invoke();
        }
        private void OnMRecAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            m_MRecAdExpandedCallback?.Invoke();
        }
        private void OnMRecAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            m_MRecAdCollapsedCallback?.Invoke();
        }
        public override void ShowMRecAds()
        {
            base.ShowMRecAds();
            if (string.IsNullOrEmpty(m_MaxAdConfig.MrecAdUnitID)) return;
            MaxSdk.ShowMRec(m_MaxAdConfig.MrecAdUnitID);
        }
        public override void HideMRecAds()
        {
            base.HideMRecAds();
            if (string.IsNullOrEmpty(m_MaxAdConfig.MrecAdUnitID)) return;
            MaxSdk.HideMRec(m_MaxAdConfig.MrecAdUnitID);
        }
        #endregion

        #region App Open Ads

        public override void InitAppOpenAds(Action adLoadedCallback, Action adLoadFailedCallback,
            Action adClosedCallback, Action adDisplayedCallback, Action adFailedToDisplayCallback)
        {
            base.InitAppOpenAds(adLoadedCallback, adLoadFailedCallback,
                adClosedCallback, adDisplayedCallback, adFailedToDisplayCallback);

            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenAdLoadedEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenAdLoadFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent += OnAppOpenAdClickedEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += (adUnitID, adInfo) => { OnAdRevenuePaidEvent(AdsType.APP_OPEN, adUnitID, adInfo);};
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenAdHiddenEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenAdDisplayedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenAdDisplayFailedEvent;
            RequestAppOpenAds();
        }
        public override void ShowAppOpenAds()
        {
            base.ShowAppOpenAds();
            if (string.IsNullOrEmpty(m_MaxAdConfig.AppOpenAdUnitID)) return;
            MaxSdk.ShowAppOpenAd(m_MaxAdConfig.AppOpenAdUnitID);
        }
        public override void RequestAppOpenAds()
        {
            if (string.IsNullOrEmpty(m_MaxAdConfig.AppOpenAdUnitID)) return;
            MaxSdk.LoadAppOpenAd(m_MaxAdConfig.AppOpenAdUnitID);
        }
        public override bool IsAppOpenAdsLoaded()
        {
            if (string.IsNullOrEmpty(m_MaxAdConfig.AppOpenAdUnitID)) return false;
            return MaxSdk.IsAppOpenAdReady(m_MaxAdConfig.AppOpenAdUnitID);
        }
        private void OnAppOpenAdLoadedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            SDKDebugLogger.Log("MAX Mediation App Open Ads Loaded Success");
            m_AppOpenAdLoadedCallback?.Invoke();
        }
        private void OnAppOpenAdLoadFailedEvent(string adUnitID, MaxSdkBase.ErrorInfo errorInfo)
        {
            SDKDebugLogger.Log("MAX Mediation App Open Ads Loaded Fail");
            m_AppOpenAdLoadFailedCallback?.Invoke();
        }
        private void OnAppOpenAdClickedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
        }
        private void OnAppOpenAdDisplayedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            SDKDebugLogger.Log("MAX Mediation App Open Ads Displayed");
            m_AppOpenAdDisplayedCallback?.Invoke();
        }
        private void OnAppOpenAdDisplayFailedEvent(string adUnitID, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            SDKDebugLogger.Log("MAX Mediation App Open Ads Displayed Fail");
            m_AppOpenAdFailedToDisplayCallback?.Invoke();
        }
        private void OnAppOpenAdHiddenEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            SDKDebugLogger.Log("MAX Mediation App Open Ads Hidden");
            m_AppOpenAdClosedCallback?.Invoke();
        }
        #endregion
#endif
        public override AdsMediationType GetAdsMediationType()
        {
            return AdsMediationType.MAX;
        }
    }
    #if !UNITY_AD_MAX
    public enum BannerPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        Centered,
        CenterLeft,
        CenterRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }
    #endif
}
