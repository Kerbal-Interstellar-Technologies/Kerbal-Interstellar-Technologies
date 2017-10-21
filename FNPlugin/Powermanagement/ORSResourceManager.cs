﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin 
{
    public class PowerConsumption
    {
        public double Power_draw { get; set; }
        public double Power_consume { get; set; }
    }

    public class PowerGenerated
    {
        public double currentSupply { get; set; }
        public double averageSupply { get; set; }
        public double currentProvided { get; set; }
        public double maximumSupply { get; set; }
        public double minimumSupply { get; set; }
    }



    public class ORSResourceManager 
    {
        public const string STOCK_RESOURCE_ELECTRICCHARGE = "ElectricCharge";
        public const string FNRESOURCE_MEGAJOULES = "Megajoules";
        public const string FNRESOURCE_CHARGED_PARTICLES = "ChargedParticles";
        public const string FNRESOURCE_THERMALPOWER = "ThermalPower";
        public const string FNRESOURCE_WASTEHEAT = "WasteHeat";

        public const double ONE_THIRD = 1.0 / 3.0;

        public const int FNRESOURCE_FLOWTYPE_SMALLEST_FIRST = 0;
        public const int FNRESOURCE_FLOWTYPE_EVEN = 1;
               
        protected Vessel my_vessel;
        protected Part my_part;
        protected PartModule my_partmodule;

        protected PartResourceDefinition resourceDefinition;
        protected PartResourceDefinition electricResourceDefinition;
        protected PartResourceDefinition megajouleResourceDefinition;
        protected PartResourceDefinition thermalpowerResourceDefinition;
        protected PartResourceDefinition chargedpowerResourceDefinition;

        protected bool producesWasteHeat;

        protected Dictionary<ORSResourceSuppliable, PowerConsumption> power_consumption;
        protected Dictionary<IORSResourceSupplier, PowerGenerated> power_produced;

        protected Dictionary<IORSResourceSupplier, Queue<double>> power_produced_history = new Dictionary<IORSResourceSupplier, Queue<double>>() ;

        protected string resource_name;
        protected double currentPowerSupply = 0;
        protected double stable_supply = 0;

        protected double stored_stable_supply = 0;
        protected double stored_resource_demand = 0;
        protected double stored_current_hp_demand = 0;
        protected double stored_current_demand = 0;
        protected double stored_current_charge_demand = 0;
        protected double stored_supply = 0;
        protected double stored_charge_demand = 0;
        protected double stored_total_power_supplied = 0;

        protected double current_resource_demand = 0;
        protected double high_priority_resource_demand = 0;
        protected double charge_resource_demand = 0;
        protected double total_power_distributed = 0;

        protected int flow_type = 0;
        protected List<KeyValuePair<ORSResourceSuppliable, PowerConsumption>> power_draw_list_archive;
        protected List<KeyValuePair<IORSResourceSupplier, PowerGenerated>> power_supply_list_archive;

        protected bool render_window = false;
        protected Rect windowPosition = new Rect(50, 50, 300, 100);
        protected int windowID = 36549835;
        protected double resource_bar_ratio = 0;

        protected double internl_power_extract_fixed = 0;

        public Rect WindowPosition 
        { 
            get { return windowPosition; }
            set { windowPosition = value; }
        }

        public int WindowID
        {
            get { return windowID; }
            set { windowID = value; }
        }

        public ORSResourceManager(PartModule pm,String resource_name) 
        {
            my_vessel = pm.vessel;
            my_part = pm.part;
            my_partmodule = pm;

            windowID = new System.Random(resource_name.GetHashCode()).Next(int.MinValue, int.MaxValue);

            power_consumption = new Dictionary<ORSResourceSuppliable, PowerConsumption>();
            power_produced = new Dictionary<IORSResourceSupplier, PowerGenerated>();

            this.resource_name = resource_name;

            resourceDefinition = PartResourceLibrary.Instance.GetDefinition(resource_name);
            electricResourceDefinition = PartResourceLibrary.Instance.GetDefinition(ORSResourceManager.STOCK_RESOURCE_ELECTRICCHARGE);
            megajouleResourceDefinition = PartResourceLibrary.Instance.GetDefinition(ORSResourceManager.FNRESOURCE_MEGAJOULES); 
            thermalpowerResourceDefinition = PartResourceLibrary.Instance.GetDefinition(ORSResourceManager.FNRESOURCE_THERMALPOWER);
            chargedpowerResourceDefinition = PartResourceLibrary.Instance.GetDefinition(ORSResourceManager.FNRESOURCE_CHARGED_PARTICLES);

            producesWasteHeat = resourceDefinition.id == thermalpowerResourceDefinition.id || resourceDefinition.id == chargedpowerResourceDefinition.id;

            if (resource_name == FNRESOURCE_WASTEHEAT || resource_name == FNRESOURCE_THERMALPOWER || resource_name == FNRESOURCE_CHARGED_PARTICLES) 
                flow_type = FNRESOURCE_FLOWTYPE_EVEN;
            else 
                flow_type = FNRESOURCE_FLOWTYPE_SMALLEST_FIRST;
        }

        public void powerDrawFixed(ORSResourceSuppliable pm, double power_draw, double power_cosumtion) 
        {
            var timeWarpFixedDeltaTime = TimeWarpFixedDeltaTime;
            var power_draw_per_second = power_draw / timeWarpFixedDeltaTime;
            var power_cosumtion_per_second = power_cosumtion / timeWarpFixedDeltaTime;
            
            PowerConsumption powerConsumption;
            if (!power_consumption.TryGetValue(pm, out powerConsumption))
            {
                powerConsumption = new PowerConsumption();
                power_consumption.Add(pm, powerConsumption);
            }
            powerConsumption.Power_draw += power_draw_per_second;
            powerConsumption.Power_consume += power_cosumtion_per_second;         
        }

        public void powerDrawPerSecond(ORSResourceSuppliable pm, double power_draw, double draw_power_consumption)
        {
            PowerConsumption powerConsumption;
            if (!power_consumption.TryGetValue(pm, out powerConsumption))
            {
                powerConsumption = new PowerConsumption();
                power_consumption.Add(pm, powerConsumption);
            }
            powerConsumption.Power_draw += power_draw;
            powerConsumption.Power_consume += draw_power_consumption;
        }

        public double powerSupplyFixed(IORSResourceSupplier pm, double power) 
        {
            var current_power_supply_per_second = power / TimeWarpFixedDeltaTime;

            currentPowerSupply += current_power_supply_per_second;
            stable_supply += current_power_supply_per_second;

            PowerGenerated powerGenerated;
            if (!power_produced.TryGetValue(pm, out powerGenerated))
            {
                powerGenerated = new PowerGenerated();
                power_produced.Add(pm, powerGenerated);
            }
            powerGenerated.currentSupply += current_power_supply_per_second;
            powerGenerated.currentProvided += current_power_supply_per_second;
            powerGenerated.maximumSupply += current_power_supply_per_second;
            
            return power;
        }

        public double powerSupplyPerSecond(IORSResourceSupplier pm, double power)
        {
            currentPowerSupply += power;
            stable_supply += power;

            PowerGenerated powerGenerated;
            if (!power_produced.TryGetValue(pm, out powerGenerated))
            {
                powerGenerated = new PowerGenerated();
                power_produced.Add(pm, powerGenerated);
            }
            powerGenerated.currentSupply += power;
            powerGenerated.currentProvided += power;
            powerGenerated.maximumSupply += power;

            return power;
        }

        public double powerSupplyFixedWithMax(IORSResourceSupplier pm, double power, double maxpower) 
        {
            var timeWarpFixedDeltaTime = TimeWarpFixedDeltaTime;

            var current_power_supply_per_second = power / timeWarpFixedDeltaTime;
            var maximum_power_supply_per_second = maxpower / timeWarpFixedDeltaTime;

            currentPowerSupply += current_power_supply_per_second;
            stable_supply += maximum_power_supply_per_second;

            PowerGenerated powerGenerated;
            if (!power_produced.TryGetValue(pm, out powerGenerated))
            {
                powerGenerated = new PowerGenerated();
                power_produced.Add(pm, powerGenerated);
            }
            powerGenerated.currentSupply += current_power_supply_per_second;
            powerGenerated.currentProvided += current_power_supply_per_second;
            powerGenerated.maximumSupply += maximum_power_supply_per_second;

            return power;
        }

        public double powerSupplyPerSecondWithMax(IORSResourceSupplier pm, double power, double maxpower)
        {
            currentPowerSupply += power;
            stable_supply += maxpower;

            PowerGenerated powerGenerated;
            if (!power_produced.TryGetValue(pm, out powerGenerated))
            {
                powerGenerated = new PowerGenerated();
                power_produced.Add(pm, powerGenerated);
            }
            powerGenerated.currentSupply += power;
            powerGenerated.maximumSupply += maxpower;

            return power;
        }

        public double managedPowerSupplyPerSecond(IORSResourceSupplier pm, double power)
        {
            return managedPowerSupplyPerSecondWithMinimumRatio(pm, power, 0);
        }

        public double getResourceAvailability()
        {
            double amount;
            double maxAmount;
            my_part.GetConnectedResourceTotals(resourceDefinition.id, out amount, out maxAmount);

            return amount;
        }

        public double getSpareResourceCapacity() 
        {
            double amount;
            double maxAmount;
            my_part.GetConnectedResourceTotals(resourceDefinition.id, out amount, out maxAmount);

            return maxAmount - amount;
        }

        public double getTotalResourceCapacity()
        {
            double amount;
            double maxAmount;
            my_part.GetConnectedResourceTotals(resourceDefinition.id, out amount, out maxAmount);

            return maxAmount;
        }

        public double getNeededPowerSupplyPerSecondWithMinimumRatio(double power, double ratio_min)
        {
            var minimum_power_per_second = power * ratio_min;
            var needed_power_per_second = Math.Min(power, (Math.Max(GetCurrentUnfilledResourceDemand(), minimum_power_per_second)));

            return needed_power_per_second;
        }

        public PowerGenerated managedRequestedPowerSupplyPerSecondMinimumRatio(IORSResourceSupplier pm, double available_power, double maximum_power, double ratio_min)
        {
            var minimum_power_per_second = maximum_power * ratio_min;

            var provided_demand_power_per_second = Math.Min(maximum_power, Math.Max(minimum_power_per_second, Math.Max(available_power, GetCurrentUnfilledResourceDemand())));
            var managed_supply_per_second = Math.Min(maximum_power, Math.Max(minimum_power_per_second, Math.Min(available_power, GetRequiredResourceDemand())));

            currentPowerSupply += managed_supply_per_second;
            stable_supply += maximum_power;

            var addedPower = new PowerGenerated
            {
                currentSupply = managed_supply_per_second,
                currentProvided = provided_demand_power_per_second,
                maximumSupply = maximum_power,
                minimumSupply = minimum_power_per_second
            };

            PowerGenerated powerGenerated;
            if (!power_produced.TryGetValue(pm, out powerGenerated))
            {
                power_produced.Add(pm, addedPower);
            }
            else
            {
                powerGenerated.currentSupply += addedPower.currentSupply;
                powerGenerated.currentProvided += addedPower.currentProvided;
                powerGenerated.maximumSupply += addedPower.maximumSupply;
                powerGenerated.minimumSupply += addedPower.minimumSupply;
            }

            return addedPower; 
        }

        public double managedPowerSupplyPerSecondWithMinimumRatio(IORSResourceSupplier pm, double maximum_power, double ratio_min)
        {
            var minimum_power_per_second = maximum_power * ratio_min;

            var provided_demand_power_per_second = Math.Min(maximum_power, Math.Max(GetCurrentUnfilledResourceDemand(), minimum_power_per_second));
            var required_power_per_second = Math.Max(GetRequiredResourceDemand(), minimum_power_per_second);
            var managed_supply_per_second = Math.Min(maximum_power, required_power_per_second);

            currentPowerSupply += managed_supply_per_second;
            stable_supply += maximum_power;

            PowerGenerated powerGenerated;
            if (!power_produced.TryGetValue(pm, out powerGenerated))
            {
                powerGenerated = new PowerGenerated();
                power_produced.Add(pm, powerGenerated);
            }

            powerGenerated.currentSupply += managed_supply_per_second;
            powerGenerated.currentProvided += provided_demand_power_per_second;
            powerGenerated.maximumSupply += maximum_power;
            powerGenerated.minimumSupply += minimum_power_per_second;

            return provided_demand_power_per_second;
        }

        public double StableResourceSupply { get { return stored_stable_supply; } }
        public double ResourceSupply { get { return stored_supply; } }
        public double ResourceDemand { get {  return stored_resource_demand; } }
        public double CurrentResourceDemand { get { return current_resource_demand; } }
        public double CurrentHighPriorityResourceDemand { get { return stored_current_hp_demand; } }
        public double PowerSupply { get { return currentPowerSupply; } }
        public double CurrentRresourceDemand { get { return current_resource_demand; } }
        public double ResourceBarRatio { get {  return resource_bar_ratio; } }
        public Vessel Vessel { get { return my_vessel; } }
        public PartModule PartModule { get { return my_partmodule; } }

        public double getOverproduction()
        {
            return stored_supply - stored_resource_demand;
        }

        public double getDemandStableSupply()
        {
            return stored_stable_supply > 0 ? stored_resource_demand / stored_stable_supply : 1;
        }

        public double GetCurrentUnfilledResourceDemand()
        {
            return current_resource_demand - currentPowerSupply;
        }

        public double GetRequiredResourceDemand()
        {
            return GetCurrentUnfilledResourceDemand() + getSpareResourceCapacity();
        }

        public void UpdatePartModule(PartModule pm) 
        {
            if (pm != null)
            {
                my_vessel = pm.vessel;
                my_part = pm.part;
                my_partmodule = pm;
            }
            else
            {
                my_partmodule = null;
            }
        }

        public bool IsUpdatedAtLeastOnce { get; set; }

        public long Counter { get; private set; }

        public void update(long counter) 
        {
            var timeWarpFixedDeltaTime = TimeWarpFixedDeltaTime;

            IsUpdatedAtLeastOnce = true;
            Counter = counter;

            stored_supply = currentPowerSupply;
            stored_stable_supply = stable_supply;
            stored_resource_demand = current_resource_demand;
            stored_current_demand = current_resource_demand;
            stored_current_hp_demand = high_priority_resource_demand;
            stored_current_charge_demand = charge_resource_demand;
            stored_charge_demand = charge_resource_demand;
            stored_total_power_supplied = total_power_distributed;

            current_resource_demand = 0;
            high_priority_resource_demand = 0;
            charge_resource_demand = 0;
            total_power_distributed = 0;

            double availableResourceAmount;
            double maxResouceAmount;
            my_part.GetConnectedResourceTotals(resourceDefinition.id, out availableResourceAmount, out maxResouceAmount);

            if (maxResouceAmount > 0 && !double.IsNaN(maxResouceAmount) && !double.IsNaN(availableResourceAmount)) 
                resource_bar_ratio = availableResourceAmount / maxResouceAmount;
            else 
                resource_bar_ratio = 0.0001;

            double missingResourceAmount = maxResouceAmount - availableResourceAmount;
            currentPowerSupply += availableResourceAmount;

            double high_priority_demand_supply_ratio = high_priority_resource_demand > 0
                ? Math.Min((currentPowerSupply - stored_current_charge_demand) / stored_current_hp_demand, 1.0)
                : 1.0;

            double demand_supply_ratio = stored_current_demand > 0
                ? Math.Min((currentPowerSupply - stored_current_charge_demand - stored_current_hp_demand) / stored_current_demand, 1.0)
                : 1.0;        

            //Prioritise supplying stock ElectricCharge resource
            if (resourceDefinition.id == megajouleResourceDefinition.id && stored_stable_supply > 0) 
            {
                double amount;
                double maxAmount;

                my_part.GetConnectedResourceTotals(electricResourceDefinition.id, out amount, out maxAmount);
                double stock_electric_charge_needed = maxAmount - amount;

                double power_supplied = Math.Min(currentPowerSupply * 1000 * timeWarpFixedDeltaTime, stock_electric_charge_needed);
                if (stock_electric_charge_needed > 0)
                {
                    var deltaResourceDemand = stock_electric_charge_needed / 1000 / timeWarpFixedDeltaTime;
                    current_resource_demand += deltaResourceDemand;
                    charge_resource_demand += deltaResourceDemand;
                }

                if (power_supplied > 0)
                {
                    double fixed_provided_electric_charge_in_MW = my_part.RequestResource(ORSResourceManager.STOCK_RESOURCE_ELECTRICCHARGE, -power_supplied) / 1000;
                    var provided_electric_charge_per_second = fixed_provided_electric_charge_in_MW / timeWarpFixedDeltaTime;
                    total_power_distributed += -provided_electric_charge_per_second;
                    currentPowerSupply += provided_electric_charge_per_second;
                }
            }

            power_supply_list_archive = power_produced.OrderByDescending(m => m.Value.maximumSupply).ToList();

            // store current supply and update average
            power_supply_list_archive.ForEach(m =>
                {
                    Queue<double> queue;

                    if (!power_produced_history.TryGetValue(m.Key, out queue))
                    {
                        queue = new Queue<double>(10);
                        power_produced_history.Add(m.Key, queue);
                    }

                    if (queue.Count > 10)
                        queue.Dequeue();
                    queue.Enqueue(m.Value.currentSupply);

                    m.Value.averageSupply = queue.Average();
                });

            List<KeyValuePair<ORSResourceSuppliable, PowerConsumption>> power_draw_items = power_consumption.OrderBy(m => m.Value.Power_draw).ToList();

            power_draw_list_archive = power_draw_items.ToList();
            power_draw_list_archive.Reverse();
            
            // check priority 1 parts like reactors
            foreach (KeyValuePair<ORSResourceSuppliable, PowerConsumption> power_kvp in power_draw_items) 
            {
                ORSResourceSuppliable resourceSuppliable = power_kvp.Key;

                if (resourceSuppliable.getPowerPriority() == 1) 
                {
                    double power = power_kvp.Value.Power_draw;
                    current_resource_demand += power;
                    high_priority_resource_demand += power;

                    if (flow_type == FNRESOURCE_FLOWTYPE_EVEN) 
                        power = power * high_priority_demand_supply_ratio;
                    
                    double power_supplied = Math.Max(Math.Min(currentPowerSupply, power), 0.0);

                    currentPowerSupply -= power_supplied;
                    total_power_distributed += power_supplied;

                    //notify of supply
                    resourceSuppliable.receiveFNResource(power_supplied, this.resource_name);
                }
            }

            // check priority 2 parts like reactors
            foreach (KeyValuePair<ORSResourceSuppliable, PowerConsumption> power_kvp in power_draw_items) 
            {
                ORSResourceSuppliable resourceSuppliable = power_kvp.Key;
                
                if (resourceSuppliable.getPowerPriority() == 2) 
                {
                    double power = power_kvp.Value.Power_draw;
                    current_resource_demand += power;

                    if (flow_type == FNRESOURCE_FLOWTYPE_EVEN) 
                        power = power * demand_supply_ratio;
                    
                    double power_supplied = Math.Max(Math.Min(currentPowerSupply, power), 0.0);

                    currentPowerSupply -= power_supplied;
                    total_power_distributed += power_supplied;

                    //notify of supply
                    resourceSuppliable.receiveFNResource(power_supplied, this.resource_name);
                }
            }

            // check priority 3 parts like engines and nuclear reactors
            foreach (KeyValuePair<ORSResourceSuppliable, PowerConsumption> power_kvp in power_draw_items) 
            {
                ORSResourceSuppliable resourceSuppliable = power_kvp.Key;

                if (resourceSuppliable.getPowerPriority() == 3) 
                {
                    double power = power_kvp.Value.Power_draw;
                    current_resource_demand += power;

                    if (flow_type == FNRESOURCE_FLOWTYPE_EVEN) 
                        power = power * demand_supply_ratio;

                    double power_supplied = Math.Max(Math.Min(currentPowerSupply, power), 0.0);

                    currentPowerSupply -= power_supplied;
                    total_power_distributed += power_supplied;

                    //notify of supply
                    resourceSuppliable.receiveFNResource(power_supplied, this.resource_name);
                }
            }

            // check priority 4 parts like antimatter reactors, engines and transmitters
            foreach (KeyValuePair<ORSResourceSuppliable, PowerConsumption> power_kvp in power_draw_items)
            {
                ORSResourceSuppliable resourceSuppliable = power_kvp.Key;

                if (resourceSuppliable.getPowerPriority() == 4)
                {
                    double power = power_kvp.Value.Power_draw;
                    current_resource_demand += power;

                    if (flow_type == FNRESOURCE_FLOWTYPE_EVEN)
                        power = power * demand_supply_ratio;

                    double power_supplied = Math.Max(Math.Min(currentPowerSupply, power), 0.0);

                    currentPowerSupply -= power_supplied;
                    total_power_distributed += power_supplied;

                    //notify of supply
                    resourceSuppliable.receiveFNResource(power_supplied, this.resource_name);
                }
            }

            // check priority 5 parts and higher
            foreach (KeyValuePair<ORSResourceSuppliable, PowerConsumption> power_kvp in power_draw_items)
            {
                ORSResourceSuppliable resourceSuppliable = power_kvp.Key;

                if (resourceSuppliable.getPowerPriority() >= 5)
                {
                    double power = power_kvp.Value.Power_draw;
                    current_resource_demand += power;

                    if (flow_type == FNRESOURCE_FLOWTYPE_EVEN)
                        power = power * demand_supply_ratio;

                    double power_supplied = Math.Max(Math.Min(currentPowerSupply, power), 0.0);

                    currentPowerSupply -= power_supplied;
                    total_power_distributed += power_supplied;

                    //notify of supply
                    resourceSuppliable.receiveFNResource(power_supplied, this.resource_name);
                }
            }

            // substract avaialble resource amount to get delta resource change
            currentPowerSupply -= Math.Max(availableResourceAmount, 0.0);
            internl_power_extract_fixed = -currentPowerSupply * timeWarpFixedDeltaTime;

            pluginSpecificImpl();

            if (internl_power_extract_fixed > 0) 
                internl_power_extract_fixed = Math.Min(internl_power_extract_fixed, availableResourceAmount);
            else
                internl_power_extract_fixed = Math.Max(internl_power_extract_fixed, -missingResourceAmount);

            my_part.RequestResource(this.resource_name, internl_power_extract_fixed);

            //calculate total input and output
            //var total_current_supplied = power_produced.Sum(m => m.Value.currentSupply);
            //var total_current_provided = power_produced.Sum(m => m.Value.currentProvided);
            //var total_power_consumed = power_consumption.Sum(m => m.Value.Power_consume);
            //var total_power_min_supplied = power_produced.Sum(m => m.Value.minimumSupply);

            ////generate wasteheat from used thermal power + thermal store
            //if (!CheatOptions.IgnoreMaxTemperature && total_current_produced > 0 && 
            //    (resourceDefinition.id == thermalpowerResourceDefinition.id || resourceDefinition.id == chargedpowerResourceDefinition.id))
            //{
            //    var min_supplied_fixed = TimeWarp.fixedDeltaTime * total_power_min_supplied;
            //    var used_or_stored_power_fixed = TimeWarp.fixedDeltaTime * Math.Min(total_power_consumed, total_current_produced) + Math.Max(-actual_stored_power, 0);
            //    var wasteheat_produced_fixed = Math.Max(min_supplied_fixed, used_or_stored_power_fixed);

            //    var effective_wasteheat_ratio = Math.Max(wasteheat_produced_fixed / (total_current_produced * TimeWarp.fixedDeltaTime), 1);

            //    ORSResourceManager manager = ORSResourceOvermanager.getResourceOvermanagerForResource(ORSResourceManager.FNRESOURCE_WASTEHEAT).getManagerForVessel(my_vessel);

            //    foreach (var supplier_key_value in power_produced)
            //    {
            //        if (supplier_key_value.Value.currentSupply > 0)
            //        {
            //            manager.powerSupplyPerSecondWithMax(supplier_key_value.Key, supplier_key_value.Value.currentSupply * effective_wasteheat_ratio, supplier_key_value.Value.maximumSupply * effective_wasteheat_ratio);
            //        }
            //    }
            //}

            currentPowerSupply = 0;
            stable_supply = 0;

            power_produced.Clear();
            power_consumption.Clear();
        }

        protected double TimeWarpFixedDeltaTime
        {
            get { return (double)(decimal)TimeWarp.fixedDeltaTime; }
        }

        protected virtual void pluginSpecificImpl() 
        {

        }

        public void showWindow() 
        {
            render_window = true;
        }

        public void hideWindow() 
        {
            render_window = false;
        }

        public void OnGUI() 
        {
            if (my_vessel == FlightGlobals.ActiveVessel && render_window) 
            {
                string title = resource_name + " Management Display";
                windowPosition = GUILayout.Window(windowID, windowPosition, doWindow, title);
            }
        }

        // overriden by FNResourceManager
        protected virtual void doWindow(int windowID) 
        {
           
        }

        protected string getPowerFormatString(double power) 
        {
            if (Math.Abs(power) >= 1000) 
            {
                if (Math.Abs(power) > 20000) 
                    return (power / 1000).ToString("0.0") + " GW";
                else 
                    return (power / 1000).ToString("0.00") + " GW";
            } 
            else 
            {
                if (Math.Abs(power) > 20) 
                    return power.ToString("0.0") + " MW";
                else 
                {
                    if (Math.Abs(power) >= 1) 
                        return power.ToString("0.00") + " MW";
                    
                    else 
                        return (power * 1000).ToString("0.0") + " KW";
                }
            }
        }
    }
}
