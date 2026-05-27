# ABI Lib Ads Demo

Project Unity mẫu minh họa cách tích hợp **[ABI Ads Unity Bridge](https://github.com/hongphuong0211/ABI-Lib-Ads-Support)** (`com.abi.ads.unity`) vào game thực tế.

| | |
|---|---|
| Demo repo | [ABI-Lib-Ads-Demo](https://github.com/hongphuong0211/ABI-Lib-Ads-Demo) |
| Package ads | [ABI-Lib-Ads-Support](https://github.com/hongphuong0211/ABI-Lib-Ads-Support) v1.7.9 |
| Unity | 2022.3 LTS (project hiện tại: **2022.3.62f2**) |
| Scene demo | `Assets/ABILibsSDK/Scenes/SceneDemo.unity` |
| Script demo | `Assets/ABILibsSDK/Scripts/DemoController.cs` |

---

## 1. Yêu cầu

- **Unity 2022.3+** (khuyến nghị LTS; project này dùng 2022.3 + JDK 11 cho Android).
- **External Dependency Manager (EDM4U)** — resolve Gradle dependency Android.
- **Firebase Analytics** (tuỳ chọn) — Remote Config, forward revenue TROAS/Bamboo (package đã nhúng sẵn ABI-Custom-Event).
- **AppsFlyer Unity SDK** (tuỳ chọn) — attribution trong demo.
- **AppLovin MAX Unity SDK** (bắt buộc nếu `mediation_provider = 1` hoặc `2` Dual).

> Package `com.abi.ads.unity` **đã nhúng** ABI-Custom-Event. **Không** thêm `com.abilibs.custom-events` riêng (trùng type → lỗi build).

---

## 2. Cài package ads vào project

### 2.1. Thêm dependency UPM

Mở `Packages/manifest.json`, thêm:

```json
"com.abi.ads.unity": "https://github.com/hongphuong0211/ABI-Lib-Ads-Support.git#v1.7.9"
```

Project demo này đã khai báo sẵn entry trên. Sau khi Unity resolve, package nằm tại `Packages/com.abi.ads.unity/`.

### 2.2. Dependency khuyến nghị (project host)

| Thành phần | Mục đích |
|------------|----------|
| EDM4U | Resolve `ABIAdsDependencies.xml`, mediation adapters |
| Firebase Unity SDK | Analytics, Remote Config (demo có `FirebaseManager`) |
| AppsFlyer Unity SDK | Attribution (demo có `AppsFlyerManager`) |
| MAX Unity SDK | Mediation MAX / Dual |

---

## 3. Tích hợp trong project hiện tại (checklist)

### Bước 1 — Cấu hình Global & Placement

1. Mở **ABI Ads → Configs → Edit Ads Config** (menu từ package).
2. **Global Config** → lưu `Assets/Resources/Configs/global_config.json`:
   - `mediation_provider`: `0` AdMob, `1` MAX, `2` Dual
   - `admob_app_id`, `max_sdk_key`
   - `variant_dev`: `true` khi dev, **`false`** khi build store
   - `inter_ad_interval`, `skip_interval_placements`, `test_devices`
3. **Placement Config** → lưu `Assets/Resources/Configs/placements.json`:
   - Mỗi placement: `ad_name`, `ads_type`, `ad_ids[]` (AdMob `mediation: 0`, MAX `mediation: 1`)

Project demo dùng các placement mặc định:

| Placement | `ads_type` | Mô tả |
|-----------|------------|--------|
| `main_banner` | `banner` | Banner adaptive |
| `main_interstitial` | `interstitial` | Fullscreen giữa level |
| `main_reward` | `rewarded` | Rewarded video |
| `main_app_open` | `app_open` | App Open khi resume |
| `main_native` | `native` | Native overlay |

### Bước 2 — Mediation networks (Android)

1. **Player Settings → Publishing Settings** — bật:
   - Custom Main Gradle Template
   - Custom Gradle Settings Template
   - Custom Gradle Properties Template
2. **ABI Ads → Configs → Edit Global Config** → tick network AdMob/MAX → **Apply To XML**.
3. **Assets → External Dependency Manager → Android Resolver → Force Resolve**.
4. Kiểm tra `Assets/Plugins/Android/mainTemplate.gradle` vẫn có block **GMA Next-Gen** phía trên `// Android Resolver Dependencies Start`.

Chi tiết Unity 2022 + JDK 11: xem [android-build-unity-2022-jdk11.md](https://github.com/hongphuong0211/ABI-Lib-Ads-Support/blob/v1.7.9/docs/android-build-unity-2022-jdk11.md) trong package.

Project demo có thêm `Assets/ABILibsSDK/Scripts/Editor/AndroidGradleDexFixPostProcessor.cs` — patch D8 pins cho launcher Gradle (JDK 11).

### Bước 3 — Android Application class

Trong `AndroidManifest.xml`, đặt `android:name` của `<application>`:

```xml
android:name="com.abi.ads.modules.unity.ABIUnityAdsApplication"
```

Hoặc kế thừa `AdsMultiDexApplication` nếu app đã có Application class riêng.

### Bước 4 — Bootstrap khởi tạo SDK

Tạo `MonoBehaviour` (hoặc dùng `DemoController`), gán config **TextAsset** trong Inspector:

```csharp
using ABI.Ads.UnityBridge;
using UnityEngine;

public class AdsBootstrap : MonoBehaviour
{
    [SerializeField] private TextAsset globalConfig;     // global_config.json hoặc 2.txt (encrypted)
    [SerializeField] private TextAsset placementsConfig; // placements.json hoặc 1.txt (encrypted)

    private void Awake()
    {
        ABIAds.OnInitialized += OnAdsInitialized;
        ABIAds.Initialize(globalConfig, placementsConfig);
    }

    private void OnAdsInitialized(ABIAdsEvent e)
    {
        if (!e.ready) return;
        PreloadAll();
    }

    private void PreloadAll()
    {
        ABIAds.Load("main_interstitial");
        ABIAds.Load("main_app_open");
        ABIAds.LoadRewarded("main_reward");
        // Banner/Native: ShowBanner / ShowNative tự request khi show
    }

    private void OnDestroy()
    {
        ABIAds.OnInitialized -= OnAdsInitialized;
    }
}
```

**Demo hiện tại** (`DemoController.cs`) gọi tương tự trong `Start()` và đăng ký callback theo placement.

Luôn đăng ký `OnInitialized` / `RegisterPlacement` **trước** `Initialize()`.

### Bước 5 — Gắn scene demo (tuỳ chọn)

1. Mở `Assets/ABILibsSDK/Scenes/SceneDemo.unity`.
2. Component `DemoController` đã gắn sẵn UI test từng format.
3. Kéo `global_config.json` và `placements.json` vào Inspector nếu chưa gán.

### Bước 6 — Build Android / iOS

- **Android**: Custom Gradle templates + Force Resolve (mục 2).
- **iOS**: Export → copy `Podfile.template` từ package → `pod install` → mở `.xcworkspace`.
- Gọi `ABIAds.SetCurrentViewController()` trên iOS khi đổi scene.

---

## 4. Từng ad format — setup & API

Mapping `ads_type` trong `placements.json` → API Unity (`ABIAds`):

| `ads_type` | API chính | Preload | Show |
|------------|-----------|---------|------|
| `interstitial` | `Load`, `Show`, `LoadAndShow` | `Load(placement)` | `Show(placement)` |
| `app_open` | `Load`, `Show` | Khi app pause | Khi app resume |
| `rewarded` | `LoadRewarded`, `ShowRewarded` | `LoadRewarded(placement)` | `ShowRewarded(placement)` |
| `banner` | `ShowBanner`, `HideBanner`, `DestroyBanner` | Không cần `Load` riêng | `ShowBanner(placement, "bottom")` |
| `mrec` | Giống banner | — | `ShowBanner(placement, position)` |
| `native` | `ShowNative`, `SetNativePlaceholderBounds`, `HideNative`, `DestroyNative` | **Không** `Load` + `ShowNative` | `ShowNative(...)` tự load |

### 4.1. Banner

```csharp
// ShowBanner tự request ad; không cần Load trước
ABIAds.ShowBanner("main_banner", "bottom");
ABIAds.HideBanner();
ABIAds.DestroyBanner();
```

**Demo:** nút Load Banner gọi `ABIAds.Load` (tuỳ chọn preload pool); Show/Hide map trực tiếp API trên.

**Callback:** `banner_requested`, `impression`, `clicked`, `revenue`, `banner_hidden`, `banner_destroyed`, `failed`, `display_failed`.

### 4.2. Interstitial

```csharp
ABIAds.Load("main_interstitial");

if (ABIAds.IsPlacementReady("main_interstitial"))
    ABIAds.Show("main_interstitial");
else
    ABIAds.LoadAndShow("main_interstitial");

// Timeout loading (ms)
ABIAds.Show("main_interstitial", timeoutLoadingAds: 3000);
```

**Callback:** `loaded`, `failed`, `impression`, `clicked`, `closed`, `display_failed`, `revenue`.

### 4.3. Rewarded

```csharp
ABIAds.RegisterPlacement("main_reward", new ABIAdsPlacementCallbacks
{
    OnRewardGranted = e => GrantReward(e.rewardType, e.rewardAmount),
    OnFailed = e => Debug.Log($"Reward failed: {e.error}"),
    OnClosed = e => ResumeGameplay()
});

ABIAds.LoadRewarded("main_reward");
ABIAds.ShowRewarded("main_reward", timeoutLoadingAds: 3000);
```

Trao thưởng tại **`reward_granted`**. Dùng **`reward_completed`** nếu cần đợi ad đóng hẳn.

**Callback:** `loaded`, `failed`, `reward_granted`, `reward_completed`, `closed`, `revenue`, …

### 4.4. App Open

```csharp
void OnApplicationPause(bool paused)
{
    if (!ABIAds.IsReady()) return;

    if (paused)
        ABIAds.Load("main_app_open");
    else if (ABIAds.IsPlacementReady("main_app_open"))
        ABIAds.Show("main_app_open");
}
```

**Callback:** giống interstitial (`loaded`, `closed`, `revenue`, …).

### 4.5. Native

```csharp
// Neo vùng placeholder (tọa độ chuẩn hoá 0..1) trước khi show
ABIAds.SetNativePlaceholderBounds(minX: 0f, minY: 0.6f, maxX: 1f, maxY: 1f);

// ShowNative TỰ LOAD — không gọi Load() rồi OnLoaded → ShowNative()
ABIAds.ShowNative(
    placement: "main_native",
    templateName: "ads_layout_native_language",
    size: NativeSize.Medium,       // Small | Medium | FreeSize
    position: NativePosition.Bottom, // Top | Center | Bottom
    duration: 0                    // giây; < 0 ẩn nút close
);

ABIAds.ShowNativeFullScreen("main_native_full", countDownSec: 3);

ABIAds.HideNative();
ABIAds.DestroyNative();
```

**Demo:** field `nativeTemplateName` (InputField) truyền tên layout Android/iOS.

**Callback:** `native_requested`, `loaded`, `impression`, `clicked`, `native_hidden`, `native_destroyed`, `revenue`, …

---

## 5. Callback trong project demo

`DemoController` đăng ký callback toàn cục và theo placement:

```csharp
ABIAds.EventReceived += OnABIAdsEvent;
ABIAds.OnInitialized += OnABIAdsInitialized;

var cb = ABIAds.RegisterPlacement("main_interstitial");
cb.OnLoaded = e => Log($"{e.placement} loaded");
cb.OnRevenue = e => Log($"revenue={e.revenue} {e.currency}");
// OnFailed, OnClosed, OnRewardGranted, ...
```

Event global: `bridge_ready`, `initialized`, `view_controller_updated` (iOS), `config_applied`.

Hằng số: `ABIAdsEventNames.Loaded`, `ABIAdsEventNames.RewardGranted`, …

---

## 6. Cấu trúc thư mục project demo

```
Assets/
├── ABILibsSDK/
│   ├── Scenes/SceneDemo.unity      # Scene test UI
│   └── Scripts/
│       ├── DemoController.cs       # Demo ABI Ads API
│       ├── SDKInitializer.cs       # Firebase / AppsFlyer / MAX (legacy wrapper)
│       ├── FirebaseManager.cs
│       ├── AppsFlyerManager.cs
│       └── Editor/
│           └── AndroidGradleDexFixPostProcessor.cs
├── Resources/Configs/
│   ├── global_config.json
│   └── placements.json
└── Plugins/Android/
    ├── mainTemplate.gradle
    └── gradleTemplate.properties
Packages/
└── com.abi.ads.unity/              # ABI Ads Unity Bridge (UPM git)
```

---

## 7. Clone & chạy demo

```bash
git clone https://github.com/hongphuong0211/ABI-Lib-Ads-Demo.git
```

1. Mở project bằng **Unity 2022.3 LTS**.
2. Đợi Unity import package `com.abi.ads.unity` từ git.
3. Chạy **Android Resolver → Force Resolve** (lần đầu).
4. Mở `SceneDemo`, Play Mode — test từng nút ads trên UI.
5. Thay ad unit test bằng ID thật trong **ABI Ads → Configs** trước khi build store.

---

## 8. Tài liệu package đầy đủ

- README package: [ABI-Lib-Ads-Support README](https://github.com/hongphuong0211/ABI-Lib-Ads-Support/blob/v1.7.9/README.md)
- Native layout templates: [native-template-files.md](https://github.com/hongphuong0211/ABI-Lib-Ads-Support/blob/v1.7.9/docs/native-template-files.md)
- Android Unity 6: [android-build-notes.md](https://github.com/hongphuong0211/ABI-Lib-Ads-Support/blob/v1.7.9/docs/android-build-notes.md)
- Android Unity 2022 + JDK 11: [android-build-unity-2022-jdk11.md](https://github.com/hongphuong0211/ABI-Lib-Ads-Support/blob/v1.7.9/docs/android-build-unity-2022-jdk11.md)

---

*Cập nhật: 2026-05-27 — demo `com.abi.ads.unity` v1.7.9; editor config load fix; native template doc.*
