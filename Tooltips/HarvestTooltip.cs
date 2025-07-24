using System;
using UnityEngine;

namespace PlantTimers.Tooltips
{
    [DisallowMultipleComponent]
    public class HarvestTooltip : TooltipBase
    {
        private Plant _plant;

        private TimeData TimeLeftToGrow
        {
            get
            {
                if (_plant.fullGrown)
                {
                    return TimeData.zero;
                }

                var currentTime = TimeData.currentTime;
                var totalRemainingDays = 0.0f;

                var current = _plant.currentStage;
                var currentStageEndTime = current.endTime;

                if (currentStageEndTime.timeInDays > currentTime.timeInDays)
                {
                    totalRemainingDays += currentStageEndTime.timeInDays - currentTime.timeInDays;
                }

                /*
                 * There are two stages indicative of a fully grown plant.
                 * 
                 * - The _Sparkle stage providing the harvestable VFX lasting for half a game day.
                 * - The actual final growth stage.
                 *
                 * Failing to account for the Sparkle stage will yield dayDuration/2 leftover on
                 * the timer when a plant reaches a harvestable form.
                 */
                for (var i = _plant.stageNum + 1; i < Math.Max(0, _plant.growthStages.Length - 2); i++)
                {
                    var futureStage = _plant.growthStages[i];
                    var stageDuration = futureStage.duration;

                    if (current.enriched)
                    {
                        stageDuration = new TimeData(
                            stageDuration.year / 2,
                            stageDuration.month / 2,
                            stageDuration.day / 2,
                            stageDuration.hour / 2
                        );
                    }

                    if (current.fastGrowthActive)
                    {
                        stageDuration.timeInDays *= futureStage.fastGrowthTimeMultiplier;
                    }

                    totalRemainingDays += stageDuration.timeInDays;
                }

                var timeLeft = new TimeData
                {
                    timeInDays = totalRemainingDays
                };
                return timeLeft;
            }
        }

        public void SetPlant(Plant plant)
        {
            _plant = plant;
        }

        private void Start()
        {
            LabelColour = PlantTimerPlugin.HarvestLabelColour;
            Canvas.transform.localPosition = new Vector3(0, _plant.zoneType == PlantZoneType.Lattice ? 2.8f : VerticalOffset, 0);
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

            return FormatTimeForTooltip(TimeLeftToGrow);
        }

        private static string FormatTimeForTooltip(TimeData remainingTime)
        {
            var gameToRealTimeFactor = 1440f / TimeData.DayLengthInMinutes;
            var realWorldSeconds = remainingTime.timeInDays * 24 * 60 * 60 / gameToRealTimeFactor;

            var timeSpan = TimeSpan.FromSeconds(realWorldSeconds);
            
            if (timeSpan.Hours > 0)
                return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (timeSpan.Minutes > 0)
                return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";

            return $"{timeSpan.Seconds}s";
        }
    }
}