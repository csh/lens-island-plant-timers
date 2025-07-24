using UnityEngine;

namespace PlantTimers.Tooltips
{
    [DisallowMultipleComponent]
    public class HarvestTooltip : TooltipBase
    {
        private Plant _plant;

        public void SetPlant(Plant plant)
        {
            _plant = plant;
        }

        private void Start()
        {
            LabelColour = PlantTimerPlugin.HarvestLabelColour;
        }

        internal override bool ShouldBeVisible()
        {
            if (!_plant) return false;
            if (_plant.farm is null) return false;
            if (_plant.farm.isDry) return false;
            return _plant.IsGrown() == false;
        }

        protected override string GetTooltip()
        {
            if (!_plant || _plant.isDead || _plant.IsGrown())
            {
                return null;
            }

            var stageRemainingTime = _plant.currentStage.endTime - TimeData.currentTime;
            return FormatTimeForTooltip(stageRemainingTime);
        }

        private static string FormatTimeForTooltip(TimeData remainingTime)
        {
            var gameToRealTimeFactor = 1440f / TimeData.DayLengthInMinutes;
            var realWorldSeconds = remainingTime.timeInDays * 24 * 60 * 60 / gameToRealTimeFactor;

            var hours = Mathf.FloorToInt(realWorldSeconds / 3600);
            var minutes = Mathf.FloorToInt((realWorldSeconds % 3600) / 60);
            var seconds = Mathf.FloorToInt(realWorldSeconds % 60);
            
            if (hours > 0)
            {
                return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            }

            return minutes > 0
                ? $"{minutes:D2}:{seconds:D2}"
                : $"{seconds}s";
        }
    }
}