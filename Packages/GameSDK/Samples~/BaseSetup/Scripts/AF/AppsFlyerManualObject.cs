using AppsFlyerSDK;
using SDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

// This class is intended to be used the the AppsFlyerObject.prefab

public class AppsFlyerManualObject : MonoBehaviour, IAppsFlyerConversionData
{

    // These fields are set from the editor so do not modify!
    //******************************//
    public string devKey;
    public string appID;
    public string UWPAppID;
    public string macOSAppID;
    public bool isDebug;
    public bool getConversionData;
    //******************************//

    public event Action<string> ConversionDataSucceeded;

    /// <summary>
    /// AppsFlyer đã trả kết quả conversion (thành công hay lỗi) trong phiên này chưa. Code lấy camp name từ nguồn khác
    /// (vd. server) nên chờ cờ này, để không gọi server trong lúc AF sắp trả campaign.
    /// </summary>
    public static bool HasConversionResult { get; private set; }

    private void Start()
    {
        // Editor tắt Domain Reload thì static giữ nguyên giữa các lần Play.
        HasConversionResult = false;
        // These fields are set from the editor so do not modify!
        //******************************//
        AppsFlyer.setIsDebug(isDebug);
#if UNITY_WSA_10_0 && !UNITY_EDITOR
        AppsFlyer.initSDK(devKey, UWPAppID, getConversionData ? this : null);
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
    AppsFlyer.initSDK(devKey, macOSAppID, getConversionData ? this : null);
#else
        AppsFlyer.initSDK(devKey, appID, getConversionData ? this : null);
#endif
        //******************************/

        AppsFlyer.startSDK();

        if (!PlayerPrefs.HasKey("af_first_open_sent"))
        {
            AppsFlyerManager.TrackFirstOpen();
            PlayerPrefs.SetInt("af_first_open_sent", 1);
            PlayerPrefs.Save();
        }
    }

    // Mark AppsFlyer CallBacks
    public void onConversionDataSuccess(string conversionData)
    {
        AppsFlyer.AFLog("didReceiveConversionData", conversionData);
        Debug.Log("didReceiveConversionData: " + conversionData);
        Dictionary<string, object> conversionDataDictionary = AppsFlyer.CallbackStringToDictionary(conversionData);
        // add deferred deeplink logic here

        try
        {
            var status = "";
            if (conversionDataDictionary.ContainsKey("af_status"))
            {
                status = conversionDataDictionary["af_status"].ToString();
            }
            var campaign = "";
            if (conversionDataDictionary.ContainsKey("campaign"))
            {
                campaign = conversionDataDictionary["campaign"].ToString();
            }
            // AppsFlyer vẫn trả tên camp CŨ cho user đã được attribution trước khi UA đổi tên -> game chuẩn hoá qua hook.
            if (SDKGameHooks.NormalizeCampaign != null) campaign = SDKGameHooks.NormalizeCampaign(campaign) ?? "";
            var campaignID = "";
            if (conversionDataDictionary.ContainsKey("campaign_id"))
            {
                campaignID = conversionDataDictionary["campaign_id"].ToString();
            }
            if (campaign.Length > 0)
            {
                UserPropertyManager.SetUserPropertyFirebase?.Invoke("camp_name", campaign);
            }
            if (campaignID.Length > 0)
            {
                UserPropertyManager.SetUserPropertyFirebase?.Invoke("camp_id", campaignID);
            }

            if (status.Length > 0)
            {
                UserPropertyManager.SetUserPropertyFirebase?.Invoke("user_source", status);
            }
            bool isFirstLaunch = false;
            // save to player pref
            if (campaign.Length > 0)
            {
                isFirstLaunch = string.IsNullOrEmpty(PlayerPrefs.GetString("af_campaign", ""));
                PlayerPrefs.SetString("af_campaign", campaign);
                PlayerPrefs.SetString(AppsFlyerManager.PrefKeyCampSource, AppsFlyerManager.CampSourceAppsFlyer);
                // Tài liệu: af_get_campname mỗi user chỉ 1 lần, dù camp name đến từ AF hay server. Trước đây bắn ở
                // MỌI lần mở app mà AF trả campaign.
                AppsFlyerManager.TrackGetCampnameOnce(AppsFlyerManager.CampSourceAppsFlyer);
            }
            if (campaignID.Length > 0)
            {
                PlayerPrefs.SetString("af_campaignID", campaignID);
            }

            if (status.Length > 0)
            {
                PlayerPrefs.SetString("af_status", status);
            }
            PlayerPrefs.Save();
            // Game lưu campaign vào config riêng và fetch lại remote config khi lần đầu nhận campaign.
            if (campaign.Length > 0 || campaignID.Length > 0)
            {
                SDKGameHooks.OnCampaignReceived?.Invoke(campaign, campaignID, isFirstLaunch);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            HasConversionResult = true;
            ConversionDataSucceeded?.Invoke(conversionData);
        }
    }

    public void onConversionDataFail(string error)
    {
        AppsFlyer.AFLog("didReceiveConversionDataWithError", error);
        HasConversionResult = true;
    }

    public void onAppOpenAttribution(string attributionData)
    {
        AppsFlyer.AFLog("onAppOpenAttribution", attributionData);
        Dictionary<string, object> attributionDataDictionary = AppsFlyer.CallbackStringToDictionary(attributionData);
        // add direct deeplink logic here

    }

    public void onAppOpenAttributionFailure(string error)
    {
        AppsFlyer.AFLog("onAppOpenAttributionFailure", error);
    }
}

internal static class AppsFlyerDeepLinkBridge
{
    // Các khóa phổ biến AppsFlyer trả về cho deeplink
    private static readonly string[] UrlKeys = new[]
    {
        "af_dp",              // đường dẫn đầy đủ
        "af_deeplink",        // boolean hoặc string tùy SDK
        "deep_link_value",    // đôi khi chứa path/action
        "af_sub1"             // fallback tùy setup chiến dịch
    };

    public static string ExtractUrl(IDictionary<string, object> data)
    {
        if (data == null) return null;

        foreach (var key in UrlKeys)
        {
            if (data.TryGetValue(key, out var value) && value != null)
            {
                var s = value.ToString();
                if (!string.IsNullOrEmpty(s) && s.Contains("://"))
                {
                    return s;
                }
            }
        }

        // Trường hợp chiến dịch chỉ gửi action, ta tự build URL theo schema ứng dụng
        if (data.TryGetValue("deep_link_value", out var dlv) && dlv != null)
        {
            var action = dlv.ToString();
            if (!string.IsNullOrEmpty(action))
            {
                var url = $"skewersort://{action}";
                
                // Merge thêm eventId nếu có trong data
                if (data.TryGetValue("eventId", out var eventId) && eventId != null)
                {
                    var eventIdStr = eventId.ToString();
                    if (!string.IsNullOrEmpty(eventIdStr))
                    {
                        url += $"?eventId={eventIdStr}";
                    }
                }
                
                return url;
            }
        }

        return null;
    }
}
