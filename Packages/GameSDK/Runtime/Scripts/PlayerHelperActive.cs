using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TW.Utility.Extension;
using UnityEngine;
using UnityEngine.UI;

namespace SDK
{
    public class PlayerHelperActive : MonoBehaviour
    {
        private Text _playerIdLabel;
        private GameObject _overlayRoot;

        private int FullPlayerId => SystemInfo.deviceUniqueIdentifier.GetHashCode();

        private void Awake()
        {
            _overlayRoot = new GameObject("PlayerIdCheckOverlay");
            _overlayRoot.transform.SetParent(transform, false);

            var canvas = _overlayRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;

            var scaler = _overlayRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var textGo = new GameObject("PlayerIdText");
            textGo.transform.SetParent(_overlayRoot.transform, false);

            _playerIdLabel = textGo.AddComponent<Text>();
            // Unity 6+ / newer runtimes: only LegacyRuntime.ttf is valid for GetBuiltinResource (Arial.ttf throws).
            _playerIdLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_playerIdLabel.font == null)
                _playerIdLabel.font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Liberation Sans", "Segoe UI" }, 16);
            _playerIdLabel.fontSize = 14;
            _playerIdLabel.color = Color.white;
            _playerIdLabel.alignment = TextAnchor.UpperRight;
            _playerIdLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            _playerIdLabel.verticalOverflow = VerticalWrapMode.Overflow;
            _playerIdLabel.raycastTarget = false;

            var outline = textGo.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);

            var rt = textGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.one;
            rt.anchorMax = Vector2.one;
            rt.pivot = Vector2.one;
            rt.anchoredPosition = new Vector2(-5f, -5f);
            rt.sizeDelta = new Vector2(1200f, 80f);
        }

        private void OnDestroy()
        {
            if (_overlayRoot != null)
                Destroy(_overlayRoot);
        }

        private void Start()
        {
            _playerIdLabel.text = $"Player ID: {FullPlayerId}";
            TryActiveHelper().Forget();
        }

        private async UniTask TryActiveHelper()
        {
            List<Dictionary<string, string>> dataTable =
                await ABakingSheet.GetDataTable("1onpRBqfzEDcZd4oi_Yv63Nz-r6PFTJ2KQE1NVhOYBmY", "HelperActive");
            for (int i = 0; i < dataTable.Count; i++)
            {
                if (dataTable[i]["PlayerID"] != FullPlayerId.ToString()) continue;
                // CheatObject.HelperIsActive = true;
                _playerIdLabel.text = $"Player ID: {FullPlayerId} (Helper Active)";
                break;
            }
        }
    }
}

