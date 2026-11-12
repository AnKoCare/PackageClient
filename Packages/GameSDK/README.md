# ADS FB AF IAP SDK

Unity SDK tích hợp đầy đủ các tính năng quảng cáo, phân tích và thanh toán trong ứng dụng cho Unity.

Require Dependency: 

NuGetForUnity: https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity

Install MemoryPack, ObservableCollections.R3, ZLinq by Cysharp from NuGet tool

ZLinq: https://github.com/Cysharp/ZLinq.git?path=src/ZLinq.Unity/Assets/ZLinq.Unity

UniTask: https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask

R3: https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity

MemoryPack: https://github.com/Cysharp/MemoryPack.git?path=src/MemoryPack.Unity/Assets/MemoryPack.Unity

TW Utility: https://github.com/AnKoCare/TW-CustomPackage.git?path=Packages/TW.Utility

TW UGUI: https://github.com/AnKoCare/TW-CustomPackage.git?path=Packages/TW.UGUI

TW Reactive: https://github.com/AnKoCare/TW-CustomPackage.git?path=Packages/TW.Reactive

TW ACacheEverything: https://github.com/AnKoCare/TW-CustomPackage.git?path=Packages/TW.CacheEverything

Odin Inspector and Serializer

GoogleMobileAds

appsflyer-unity-plugin

AppLovin-MAX-Unity-Plugin

Add UNITY_AD_ADMOB, UNITY_APPSFLYER, UNITY_AD_MAX, CHEAT_ONLY into Scripting Define Symbols


## SDKGameHooks (sample Base Setup, từ 1.2.4)

Sample không gọi thẳng vào code riêng của game nữa. Game gán các hook trong `SDK.SDKGameHooks` lúc khởi động (trước khi Ads / IAP / AppsFlyer chạy); hook để null thì dùng mặc định.

| Hook | Dùng cho | Mặc định |
|---|---|---|
| `IsRemoveAds` | đã mua gói xoá quảng cáo | false |
| `ModifyInterCapping`, `ModifyInterCappingAfterReward` | chỉnh capping inter theo level / campaign | giữ giá trị remote |
| `CanShowInterToday` | trần số inter mỗi ngày | true |
| `GetSkipFirstInterCountOnReopen` | bỏ qua N inter đầu mỗi lần mở lại game | 0 |
| `IsBannerAllowed` | luật hiện banner của game | true |
| `OnInterstitialShown`, `OnAdViewed` | đếm inter / ngày, đếm quảng cáo trong level | — |
| `OnAdRevenuePaid`, `OnMaxAdRevenuePaid` | value bidding, ABILibsCustomEvent, TikTok | — |
| `ShowError`, `ShowLoadingBlock`, `HideLoadingBlock` | UI khi lỗi quảng cáo / chờ store | — |
| `GetExtraConsumableProductIds`, `TryHandleSpecialProduct` | SKU ngoài ShopGlobalConfig (Play Games Rewards) | — |
| `GrantProductById` | giao đồ cho giao dịch pending / restore không có callback trong phiên | chỉ log |
| `NormalizeCampaign`, `OnCampaignReceived` | chuẩn hoá camp name AppsFlyer, lưu config, fetch lại remote | — |

Ví dụ (AntFood):

```csharp
SDKGameHooks.IsRemoveAds = () => ShopData.Instance.IsRemoveADs.Value;
SDKGameHooks.IsBannerAllowed = BannerPolicy.IsBannerAllowed;
SDKGameHooks.ShowError = msg => UIManager.Instance.ShowError(msg);
SDKGameHooks.GrantProductById = IAPRewardDelivery.GrantByProductId;
```

## Inter ưu tiên trong capping (từ 1.2.5)

`MaxMediationController` (sample) load inter theo thứ tự `interstitialAdsUnit` (unit 1 = floor cao nhất), port từ Skewer:

- Capping (`AdsController.InterCooldown`) còn > `interPriorityFallbackBefore` giây: chỉ thử `interPriorityUnits` unit đầu. Unit nào có ads thì giữ sẵn, hết capping mới show. Cả nhóm no-fill thì chờ `interPriorityRetryDelay` giây rồi thử lại từ unit 1.
- Tới mốc còn `interPriorityFallbackBefore` giây mà chưa có ads: chuyển sang luồng thường, load tiếp unit sau đó.
- Capping ≤ `interPriorityFallbackBefore` (hoặc đã hết): luồng thường, load tuần tự unit 1 → cuối.

Tham số nằm trên `AdsController` (InterPriorityUnits / InterPriorityRetryDelay / InterPriorityFallbackBefore), Remote Config key `inter_priority_config` ghi đè, cùng tên field với `ads_config` của Skewer:

```json
{"interPriorityUnits":2,"interPriorityRetryDelay":20,"interPriorityFallbackBefore":10}
```

`interPriorityUnits = 0` tắt chế độ ưu tiên.
