using System;
using ThreadRace.Presentation.Models;
using ThreadRace.Presentation.Animation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThreadRace.Presentation.Views
{
    public sealed class RaceHudView : PhaseViewBase, IRaceHudView
    {
        [SerializeField] private RacerHudRowView[] _racerRows;
        [SerializeField] private RectTransform[] _rankSlots;
        [SerializeField] private TMP_Text _countdownText;
        [SerializeField] private Button _closeButton;

        public event Action CloseRequested;

        public IReadOnlyListShim<RacerHudRowView> RacerRows => new IReadOnlyListShim<RacerHudRowView>(_racerRows);

        public void Render(RaceHudModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (_racerRows == null || _rankSlots == null)
            {
                return;
            }

            SetCountdownText(model.CountdownText);

            for (var i = 0; i < model.Rows.Count; i++)
            {
                var rowModel = model.Rows[i];
                var rowView = FindRow(rowModel.RacerId);
                if (rowView == null)
                {
                    continue;
                }

                var slotIndex = Mathf.Clamp(rowModel.TargetSlotIndex, 0, _rankSlots.Length - 1);
                rowView.Render(rowModel, _rankSlots[slotIndex]);
            }
        }

        public void SetCountdownText(string countdownText)
        {
            if (_countdownText != null)
            {
                _countdownText.text = countdownText ?? string.Empty;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (_closeButton != null)
            {
                ButtonPressFeedback.Install(_closeButton);
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        protected override void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }

            base.OnDestroy();
        }

        private void OnCloseButtonClicked()
        {
            CloseRequested?.Invoke();
        }

        private RacerHudRowView FindRow(string racerId)
        {
            for (var i = 0; i < _racerRows.Length; i++)
            {
                var row = _racerRows[i];
                if (row != null && string.Equals(row.RacerId, racerId, StringComparison.Ordinal))
                {
                    return row;
                }
            }

            return null;
        }
    }

    public readonly struct IReadOnlyListShim<T>
    {
        private readonly T[] _items;

        public IReadOnlyListShim(T[] items)
        {
            _items = items;
        }

        public int Count => _items == null ? 0 : _items.Length;

        public T this[int index] => _items[index];
    }
}
