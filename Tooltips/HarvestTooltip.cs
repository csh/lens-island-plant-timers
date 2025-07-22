using UnityEngine;

namespace PlantTimers.Tooltips
{
    [DisallowMultipleComponent]
    public class HarvestTooltip : TooltipBase
    {
        private Plant _plant;

        protected override Color LabelColour => Color.cyan;

        public void SetPlant(Plant plant)
        {
            _plant = plant;
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

            var gameManager = Singleton<GameManager>.Instance;
            if (!gameManager) return null;

            var currentGameTime = TimeData.currentTime;
            var totalRemainingTime = TimeData.zero;

            for (var i = _plant.stageNum; i < _plant.growthStages.Length; i++)
            {
                var stage = _plant.growthStages[i];
                var duration = AdjustDurationForModifiers(stage);

                totalRemainingTime += duration;

                if (_plant.stageNum != i) continue;
                var stageEndTime = stage.startTime + duration;
                if (currentGameTime >= stageEndTime) continue;
                totalRemainingTime = stageEndTime - currentGameTime;
                break;
            }

            return FormatTimeForTooltip(totalRemainingTime);
        }

        private static TimeData AdjustDurationForModifiers(PlantStage stage)
        {
            var duration = stage.duration;

            if (stage.enriched)
            {
                duration = new TimeData(
                    duration.year / 2,
                    duration.month / 2,
                    duration.day / 2,
                    duration.hour / 2
                );
            }

            if (stage.fastGrowthActive)
            {
                duration.timeInDays *= stage.fastGrowthTimeMultiplier;
            }

            return duration;
        }

        private static string FormatTimeForTooltip(TimeData remainingTime)
        {
            var gameToRealTimeFactor = 1440f / TimeData.DayLengthInMinutes;
            var realWorldSeconds = remainingTime.timeInDays * 24 * 60 * 60 / gameToRealTimeFactor;

            var hours = Mathf.FloorToInt(realWorldSeconds / 3600);
            var minutes = Mathf.FloorToInt((realWorldSeconds % 3600) / 60);
            var seconds = Mathf.FloorToInt(realWorldSeconds % 60);

            if (hours == 0 && minutes == 0 && seconds == 0) return null;
            
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