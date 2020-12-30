using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System;

namespace KIT.Reactors
{
    [KSPModule("Magnetic Confinement Fusion Engine")]
    class InterstellarTokamakFusionEngine : InterstellarTokamakFusionReactor { }

    [KSPModule("Magnetic Confinement Fusion Reactor")]
    class InterstellarTokamakFusionReactor : InterstellarFusionReactor
    {
        // persistents
        [KSPField(isPersistant = true)]
        public double storedPlasmaEnergyRatio;

        // configs
        [KSPField]
        public double plasmaBufferSize = 10;
        [KSPField]
        public double minimumHeatingRequirements = 0.1;
        [KSPField]
        public double heatingRequestExponent = 1.5;

        // help variables
        public bool fusion_alert;
        public int jumpstartPowerTime;
        public int fusionAlertFrames;
        public double power_consumed;
        public double heatingPowerRequirements;

        public double HeatingPowerRequirements
        {
            get
            {
                heatingPowerRequirements = CurrentFuelMode == null
                    ? PowerRequirement
                    : PowerRequirement * CurrentFuelMode.NormalisedPowerRequirements;

                heatingPowerRequirements = Math.Max(heatingPowerRequirements * Math.Pow(required_reactor_ratio, heatingRequestExponent), heatingPowerRequirements * minimumHeatingRequirements);

                return heatingPowerRequirements;
            }
        }

        double demandRatio;
        double resourceRatio;

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!isSwappingFuelMode && (!CheatOptions.InfiniteElectricity && demandRatio > 1.01
                                                                          && resourceRatio < 0.25) && IsEnabled && !fusion_alert)
                fusionAlertFrames++;
            else
            {
                fusion_alert = false;
                fusionAlertFrames = 0;
            }

            if (fusionAlertFrames > 2)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_TokomakFusionReator_PostMsg1"), 0.1f, ScreenMessageStyle.UPPER_CENTER);//"Warning: Fusion Reactor plasma heating cannot be guaranteed, reducing power requirements is recommended."
                fusion_alert = true;
            }

            electricPowerMaintenance = PluginHelper.GetFormattedPowerString(power_consumed) + " / " + PluginHelper.GetFormattedPowerString(heatingPowerRequirements);
            
        }

        private double GetPlasmaRatio(double receivedPowerPerSecond, double fusionPowerRequirement)
        {
            if (receivedPowerPerSecond > fusionPowerRequirement)
            {
                storedPlasmaEnergyRatio += ((receivedPowerPerSecond - fusionPowerRequirement) / PowerRequirement);
                receivedPowerPerSecond = fusionPowerRequirement;
            }
            else
            {
                var shortageRatio = (fusionPowerRequirement - receivedPowerPerSecond) / PowerRequirement;
                if (shortageRatio < storedPlasmaEnergyRatio)
                {
                    storedPlasmaEnergyRatio -= (shortageRatio / PowerRequirement);
                    receivedPowerPerSecond = fusionPowerRequirement;
                }
            }

            return Math.Round(fusionPowerRequirement > 0 ? receivedPowerPerSecond / fusionPowerRequirement : 1, 4);
        }

        private bool startingReactor;

        public override void StartReactor()
        {
            base.StartReactor();

            if (HighLogic.LoadedSceneIsEditor) return;

            startingReactor = true;
        }

        public new void KITFixedUpdate(IResourceManager resMan)
        {
            base.OnFixedUpdate();
            if (!IsEnabled)
            {
                plasma_ratio = 0;
                power_consumed = 0;
                return;
            }

            if(startingReactor)
            {
                startingReactor = false;

                var myFusionPowerRequirement = PowerRequirement;
                var availablePower = resMan.ConsumeResource(ResourceName.ElectricCharge, myFusionPowerRequirement);

                // determine if we have received enough power

                plasma_ratio = GetPlasmaRatio(availablePower, myFusionPowerRequirement);
                UnityEngine.Debug.Log("[KSPI]: InterstellarTokamakFusionReactor StartReactor plasma_ratio " + plasma_ratio);
                allowJumpStart = plasma_ratio > 0.99;

                if (allowJumpStart)
                {
                    storedPlasmaEnergyRatio = 1;
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_TokomakFusionReator_PostMsg3"), 5f, ScreenMessageStyle.LOWER_CENTER);//"Starting fusion reaction"
                    jumpstartPowerTime = 10;
                }
                else
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_TokomakFusionReator_PostMsg4"), 5f, ScreenMessageStyle.LOWER_CENTER);//"Not enough power to start fusion reaction"

                return;
            }

            var fusionPowerRequirement = HeatingPowerRequirements;

            var requestedPower = fusionPowerRequirement + ((plasmaBufferSize - storedPlasmaEnergyRatio) * PowerRequirement);

            // consume power from managed power source
            power_consumed = resMan.ConsumeResource(ResourceName.ElectricCharge, requestedPower);

            if (maintenancePowerWasteheatRatio > 0)
                resMan.ProduceResource(ResourceName.WasteHeat, maintenancePowerWasteheatRatio * power_consumed);

            if (isSwappingFuelMode)
            {
                plasma_ratio = 1;
                isSwappingFuelMode = false;
            }
            else if (jumpstartPowerTime > 0)
            {
                plasma_ratio = 1;
                jumpstartPowerTime--;
            }
            else
            {
                plasma_ratio = GetPlasmaRatio(power_consumed, fusionPowerRequirement);
                allowJumpStart = plasma_ratio > 0.99;
            }

            var stats = resMan.ResourceProductionStats(ResourceName.ElectricCharge);

            demandRatio = stats.PreviousDataSupplied() ?
                stats.PreviouslyRequested() / stats.PreviouslySupplied() :
                stats.CurrentlyRequested() / stats.CurrentlyRequested();
            resourceRatio = resMan.ResourceFillFraction(ResourceName.ElectricCharge);

        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state != StartState.Editor)
            {
                if (allowJumpStart)
                {
                    if (startDisabled)
                        allowJumpStart = false;
                    else
                    {
                        storedPlasmaEnergyRatio = plasmaBufferSize;
                        jumpstartPowerTime = 10;
                    }

                    UnityEngine.Debug.Log("[KSPI]: Jumpstart InterstellarTokamakFusionReactor ");
                }
            }

            base.OnStart(state);
        }

        public new bool ProvideResource(IResourceManager resMan, ResourceName resource, double requestedAmount)
        {
            if(!IsEnabled || startingReactor) return false;
            return base.ProvideResource(resMan, resource, requestedAmount);
        }
    }
}
