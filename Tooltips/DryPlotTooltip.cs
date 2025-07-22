using UnityEngine;

namespace PlantTimers.Tooltips
{
    [DisallowMultipleComponent]
    public class DryPlotTooltip : TooltipBase
    {
        private FarmPlot _plot;

        public void SetFarmPlot(FarmPlot plot)
        {
            _plot = plot;
        }

        internal override bool ShouldBeVisible()
        {
            return _plot && _plot.isDry && _plot.HasGrowingPlants();
        }

        protected override string GetTooltip()
        {
            if (_plot && _plot.isDry) 
                return "Needs Water!";
            return null;
        }
    }
}