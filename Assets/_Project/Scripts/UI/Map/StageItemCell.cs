using NATMP.Utilities.GamePlaySystem;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using GamesTan.UI;
using NATMP;

namespace NATMP.UI.Map
{
    public class StageItemCell : MonoBehaviour, IScrollCell
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI _stageIndexText;
        [SerializeField] private Image _imgStage;
        [SerializeField] private Image _imgTutorialBadge;
        [SerializeField] private Image _imgLock;
        [SerializeField] private Image _imgLineV;
        [SerializeField] private Image _imgLineH;
        [SerializeField] private Image[] _imgsStar;

        [Header("Sprites")]
        [SerializeField] private Sprite _unlockedStageSprite;
        [SerializeField] private Sprite _lockedStageSprite;

        private Button _launchButton;

        public void SetEmpty()
        {
            gameObject.SetActive(true);
            if (_launchButton != null)
            {
                _launchButton.onClick.RemoveAllListeners();
                _launchButton.interactable = false;
            }
            if (_stageIndexText != null)
                _stageIndexText.gameObject.SetActive(false);
            if (_imgStage != null)
                _imgStage.gameObject.SetActive(false);
            if (_imgTutorialBadge != null)
                _imgTutorialBadge.gameObject.SetActive(false);
            if (_imgLock != null)
                _imgLock.gameObject.SetActive(false);
            if (_imgLineV != null)
                _imgLineV.gameObject.SetActive(false);
            if (_imgLineH != null)
                _imgLineH.gameObject.SetActive(false);
            if (_imgsStar != null)
            {
                for (int i = 0; i < _imgsStar.Length; i++)
                {
                    if (_imgsStar[i] != null)
                        _imgsStar[i].gameObject.SetActive(false);
                }
            }
        }

        public void Bind(StageData stage)
        {
            gameObject.SetActive(true);
            EnsureLaunchButton();

            if (_stageIndexText != null)
                _stageIndexText.gameObject.SetActive(!stage.HasTutorialLabel);
            if (_imgStage != null)
                _imgStage.gameObject.SetActive(true);

            if (_stageIndexText != null)
                _stageIndexText.text = stage.StageIndex.ToString();

            if (_imgTutorialBadge != null)
                _imgTutorialBadge.gameObject.SetActive(stage.HasTutorialLabel);

            bool isUnlocked = stage.IsUnlocked;

            if (_imgStage != null)
                _imgStage.sprite = isUnlocked ? _unlockedStageSprite : _lockedStageSprite;
            if (_imgLock != null)
                _imgLock.gameObject.SetActive(!isUnlocked);
            if (_imgLineV != null)
                _imgLineV.gameObject.SetActive(IsEnableVerticalLine(stage));
            if (_imgLineH != null)
            {
                _imgLineH.gameObject.SetActive(!IsDisableHorizontalLine(stage));
            }

            ApplyStars(isUnlocked ? stage.StarCount : 0);

            if (_launchButton != null)
            {
                _launchButton.onClick.RemoveAllListeners();
                if (isUnlocked)
                    _launchButton.onClick.AddListener(() => LaunchUnlockedStage(stage));
                _launchButton.interactable = isUnlocked;
            }
        }

        private void EnsureLaunchButton()
        {
            if (_imgStage == null)
                return;
            if (_launchButton != null)
                return;
            _launchButton = _imgStage.GetComponent<Button>();
            if (_launchButton == null)
            {
                _launchButton = _imgStage.gameObject.AddComponent<Button>();
                _launchButton.targetGraphic = _imgStage;
            }
        }

        private static void LaunchUnlockedStage(StageData stage)
        {
            if (stage == null || !stage.IsUnlocked)
                return;
            var gc = GameController.Instance;
            if (gc == null)
                return;
            gc.PendingGameplayStageIndex = stage.StageIndex;
            SceneManager.LoadScene(ProjectScenes.Gameplay);
        }

        private bool IsEnableVerticalLine(StageData stage)
        {
            return (stage.StageIndex % 4) == 0;
        }
        private bool IsDisableHorizontalLine(StageData stage)
        {
            int m = stage.StageIndex % 8;
            return m == 4 || m == 5;
        }

        private void ApplyStars(int starCount)
        {
            if (_imgsStar == null || _imgsStar.Length == 0) return;

            int clamped = Mathf.Clamp(starCount, 0, 3);
            for (int i = 0; i < _imgsStar.Length; i++)
            {
                if (_imgsStar[i] == null) continue;
                _imgsStar[i].gameObject.SetActive(i < clamped);
            }
        }
    }
}
