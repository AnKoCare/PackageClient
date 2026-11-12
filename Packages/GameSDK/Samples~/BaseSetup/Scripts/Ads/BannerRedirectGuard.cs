using System;
using System.Collections.Generic;
using System.Diagnostics;
using Firebase.Analytics;

namespace SDK
{
    /// <summary>
    /// Phat hien banner tu redirect / tu bung full man hinh ma nguoi choi khong cham vao (malvertising).
    /// MAX khong co API chan redirect, nen chi doan theo dau hieu: app bi day xuong background hoac banner
    /// tu expand ngay sau khi banner vua nap creative moi, va khong co click nao di kem.
    /// Moi click / expand / nghi ngo deu log kem network + creative ID de block dung creative tren dashboard.
    /// </summary>
    public class BannerRedirectGuard
    {
        private const string EVENT_BANNER_CLICK = "ad_banner_click";
        private const string EVENT_BANNER_EXPAND = "ad_banner_expand";
        private const string EVENT_BANNER_REDIRECT_SUSPECT = "ad_banner_redirect_suspect";
        private const int MAX_PARAM_LENGTH = 100;

        // Callback click cua MAX duoc xep hang cho main thread, ma main thread dung khi app pause
        // -> click that thuong chi toi sau khi resume. Doi them chut roi moi ket luan.
        private const double EVALUATE_DELAY_AFTER_RESUME = 1d;
        private const double EXPECTED_LEAVE_DURATION = 15d;

        private sealed class CreativeInfo
        {
            public readonly string Network;
            public readonly string NetworkPlacement;
            public readonly string CreativeId;
            public readonly string DspName;

            public CreativeInfo(string network, string networkPlacement, string creativeId, string dspName)
            {
                Network = network;
                NetworkPlacement = networkPlacement;
                CreativeId = creativeId;
                DspName = dspName;
            }
        }

        // Dung clock rieng thay vi Time.realtimeSinceStartup: callback MAX co the chay ngoai main thread.
        private static double Now => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

        private static double s_ExpectedLeaveUntil = double.NegativeInfinity;

        private readonly double m_SuspectWindowSeconds;

        private CreativeInfo m_Creative;
        private double m_CreativeLoadedAt;
        private double m_LastClickAt = double.NegativeInfinity;

        private bool m_HasPending;
        private string m_PendingTrigger;
        private double m_PendingAt;
        private double m_PendingSecSinceLoad;
        private CreativeInfo m_PendingCreative;
        private double m_EvaluateAt;

        public BannerRedirectGuard(float suspectWindowSeconds)
        {
            m_SuspectWindowSeconds = suspectWindowSeconds;
        }

        /// <summary>
        /// Game tu mo thu gi do lam app roi foreground (IAP, mo link...) -> khong tinh la redirect.
        /// </summary>
        public static void MarkExpectedAppLeave()
        {
            s_ExpectedLeaveUntil = Now + EXPECTED_LEAVE_DURATION;
        }

        public void OnCreativeLoaded(string network, string networkPlacement, string creativeId, string dspName)
        {
            m_Creative = new CreativeInfo(network, networkPlacement, creativeId, dspName);
            m_CreativeLoadedAt = Now;
        }

        public void OnBannerClicked()
        {
            double now = Now;
            m_LastClickAt = now;
            LogBannerEvent(EVENT_BANNER_CLICK, m_Creative, SecondsSinceLoad(now), null);
        }

        /// <param name="canSuspect">Banner dang hien va khong co fullscreen ad nao dang chay.</param>
        public void OnBannerExpanded(bool canSuspect)
        {
            double now = Now;
            LogBannerEvent(EVENT_BANNER_EXPAND, m_Creative, SecondsSinceLoad(now), null);
            TryOpenCandidate("expand", now, canSuspect);
        }

        /// <param name="canSuspect">Banner dang hien va khong co fullscreen ad nao dang chay.</param>
        public void OnAppPaused(bool canSuspect)
        {
            TryOpenCandidate("pause", Now, canSuspect);
            // Chi ket luan sau khi app quay lai, khong phu thuoc thu tu Update / OnApplicationPause luc resume
            if (m_HasPending)
            {
                m_EvaluateAt = double.PositiveInfinity;
            }
        }

        public void OnAppResumed()
        {
            if (m_HasPending)
            {
                m_EvaluateAt = Now + EVALUATE_DELAY_AFTER_RESUME;
            }
        }

        /// <summary>
        /// Goi moi frame. Tra ve true khi vua ket luan banner tu redirect.
        /// </summary>
        public bool Tick()
        {
            if (!m_HasPending || Now < m_EvaluateAt) return false;
            m_HasPending = false;

            // Co click ngay truoc hoac trong luc app vang mat -> nguoi choi tu bam vao banner.
            if (m_LastClickAt >= m_PendingAt - m_SuspectWindowSeconds) return false;

            SDKDebugLogger.Log($"Banner nghi auto-redirect - trigger: {m_PendingTrigger}, network: {m_PendingCreative?.Network}, " +
                               $"placement: {m_PendingCreative?.NetworkPlacement}, creative: {m_PendingCreative?.CreativeId}, " +
                               $"dsp: {m_PendingCreative?.DspName}, sec since load: {m_PendingSecSinceLoad:F1}");
            LogBannerEvent(EVENT_BANNER_REDIRECT_SUSPECT, m_PendingCreative, m_PendingSecSinceLoad, m_PendingTrigger);
            return true;
        }

        private void TryOpenCandidate(string trigger, double now, bool canSuspect)
        {
            if (!canSuspect || m_Creative == null) return;
            if (now < s_ExpectedLeaveUntil) return;

            if (m_HasPending)
            {
                // Expand mo activity rieng: pause truoc, callback expand toi sau resume -> cung mot su co.
                if (!m_PendingTrigger.Contains(trigger))
                {
                    m_PendingTrigger += "+" + trigger;
                }
                return;
            }

            // Chi nghi ngo khi creative vua moi load: auto-redirect chay ngay luc render. Noi rong cua so
            // se bat nham ca nguoi choi bam Home dung luc banner refresh.
            double secSinceLoad = now - m_CreativeLoadedAt;
            if (secSinceLoad > m_SuspectWindowSeconds) return;

            m_HasPending = true;
            m_PendingTrigger = trigger;
            m_PendingAt = now;
            m_PendingSecSinceLoad = secSinceLoad;
            m_PendingCreative = m_Creative;
            m_EvaluateAt = now + EVALUATE_DELAY_AFTER_RESUME;
        }

        private double SecondsSinceLoad(double now)
        {
            return m_Creative == null ? -1d : now - m_CreativeLoadedAt;
        }

        private static void LogBannerEvent(string eventName, CreativeInfo creative, double secSinceLoad, string trigger)
        {
            if (FirebaseManager.Instance == null) return;

            List<Parameter> parameters = new List<Parameter>
            {
                new Parameter("ad_source", Clamp(creative?.Network)),
                new Parameter("ad_network_placement", Clamp(creative?.NetworkPlacement)),
                new Parameter("ad_creative_id", Clamp(creative?.CreativeId)),
                new Parameter("ad_dsp", Clamp(creative?.DspName)),
                new Parameter("sec_since_load", Math.Round(secSinceLoad, 1))
            };
            if (trigger != null)
            {
                parameters.Add(new Parameter("trigger", trigger));
            }

            FirebaseManager.Instance.LogFirebaseEvent(eventName, parameters.ToArray());
        }

        private static string Clamp(string value)
        {
            if (string.IsNullOrEmpty(value)) return "unknown";
            return value.Length > MAX_PARAM_LENGTH ? value.Substring(0, MAX_PARAM_LENGTH) : value;
        }
    }
}
