﻿namespace FNPlugin.Refinery 
{
    class AntimatterGenerator : RefineryActivityBase
    {
        public double ProductionRate { get { return _current_rate; } }

        double _efficiency = 0.01149;

        public double Efficiency { get { return _efficiency;}}

        PartResourceDefinition _antimatterDefinition;

        public AntimatterGenerator(Part part, double efficiencyMultiplier, PartResourceDefinition antimatterDefinition)
        {
            _efficiency *= efficiencyMultiplier;
            _part = part;
            _vessel = part.vessel;
            _antimatterDefinition = antimatterDefinition;

            if (HighLogic.CurrentGame != null && HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                if (PluginHelper.upgradeAvailable("ultraHighEnergyPhysics"))
                    _efficiency /= 100;
                else if (PluginHelper.upgradeAvailable("appliedHighEnergyPhysics"))
                    _efficiency /= 500;
                else if (PluginHelper.upgradeAvailable("highEnergyScience"))
                    _efficiency /= 2000;
                else
                    _efficiency /= 10000;
            }
            else
            {
                _efficiency /= 100;
            }
        }

        public void Produce(double energy_provided_in_megajoules) 
        {
            if (energy_provided_in_megajoules <= 0)
                return;

            double antimatter_units = energy_provided_in_megajoules * 1E6 / GameConstants.lightSpeedSquared / 2000 / _antimatterDefinition.density * _efficiency;

            _current_rate = -_part.RequestResource(_antimatterDefinition.id, -antimatter_units, ResourceFlowMode.STAGE_PRIORITY_FLOW);
        }        
    }
}
