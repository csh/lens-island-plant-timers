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
            return _plant.farm.isDry == false;
        }

        protected override string GetTooltip()
        {
            if (!_plant || _plant.isDead || _plant.IsGrown())
            {
                return null;
            }

            // Convert plant maturity time to real seconds
            var deltaGameDays = (_plant.TimeUntilMaturity - TimeData.currentTime).timeInDays;
            var totalGameMinutes = deltaGameDays * 24f * 60f; 
            var totalRealSeconds = totalGameMinutes * 60f / TimeData.TimeFactor;

            // Calculate time components
            var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(totalRealSeconds));
            var hours = totalSeconds / 3600;
            var minutes = (totalSeconds % 3600) / 60;
            var seconds = totalSeconds % 60;

            // Format time string based on remaining time
            return hours > 0 
                ? $"{hours:00}:{minutes:00}:{seconds:00}"  // HH:MM:SS
                : minutes > 0
                    ? $"{minutes:00}:{seconds:00}"         // MM:SS
                    : $"{seconds}s";                    // SSs
        }
    }
}