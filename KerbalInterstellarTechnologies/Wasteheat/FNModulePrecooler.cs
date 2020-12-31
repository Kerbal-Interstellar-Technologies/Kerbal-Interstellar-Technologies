﻿using System.Collections.Generic;
using System.Linq;
using KIT.Resources;
using KSP.Localization;
using UnityEngine;

namespace KIT.Wasteheat
{
    class FNModulePrecooler : PartModule
    {
        [KSPField(isPersistant = true)]
        public bool functional;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Preecooler_Area")]//Area
        public double area = 0.01;
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_Preecooler_Status")]//Precooler status
        public string statusStr;
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_Preecooler_Intake")]//Intake
        public string attachedIntakeName;

        AtmosphericIntake attachedIntake;
        List<AtmosphericIntake> radialAttachedIntakes = new List<AtmosphericIntake>();

        public override void OnStart(StartState state)
        {
            Debug.Log("[KSPI]: FNModulePrecooler - Onstart start search for Air Intake module to cool");

            // first check if part itself has an air intake
            attachedIntake = part.FindModulesImplementing<AtmosphericIntake>().FirstOrDefault();

            if (attachedIntake != null)
                Debug.Log("[KSPI]: FNModulePrecooler - Found Airintake on self");

            if (state == StartState.Editor) return;

            if (attachedIntake == null)
            {
                // then look to connect radial attached children
                radialAttachedIntakes = part.children
                    .Where(p => p.attachMode == AttachModes.SRF_ATTACH)
                    .SelectMany(p => p.FindModulesImplementing<AtmosphericIntake>()).ToList();

                Debug.Log(radialAttachedIntakes.Count > 0
                    ? "[KSPI]: FNModulePrecooler - Found Airintake in children"
                    : "[KSPI]: FNModulePrecooler - Did not find Airintake in children");
            }

            // third look for stack attachable air intake
            if (attachedIntake == null && (radialAttachedIntakes == null || radialAttachedIntakes.Count == 0))
            {
                Debug.Log("[KSPI]: FNModulePrecooler - looking at attached nodes");

                foreach (var attachNode in part.attachNodes.Where(a => a.attachedPart != null))
                {
                    var attachedPart = attachNode.attachedPart;

                    // skip any parts that contain a precooler
                    if (attachedPart.FindModulesImplementing<FNModulePrecooler>().Any())
                    {
                        Debug.Log("[KSPI]: FNModulePrecooler - skipping Module Implementing FNModulePrecooler");
                        continue;
                    }

                    attachedIntake = attachedPart.FindModulesImplementing<AtmosphericIntake>().FirstOrDefault();

                    if (attachedIntake == null) continue;

                    Debug.Log("[KSPI]: FNModulePrecooler - found Airintake in attached part with name " + attachedIntake.name);
                    break;
                }

                if (attachedIntake == null)
                {
                    Debug.Log("[KSPI]: FNModulePrecooler - looking at deeper attached nodes");

                    // look for stack attacked parts one part further
                    foreach (var attachNode in part.attachNodes.Where(a => a.attachedPart != null))
                    {
                        // then look to connect radial attached children
                        radialAttachedIntakes = attachNode.attachedPart.children
                            .Where(p => p.attachMode == AttachModes.SRF_ATTACH)
                            .SelectMany(p => p.FindModulesImplementing<AtmosphericIntake>()).ToList();

                        if (radialAttachedIntakes.Count > 0)
                        {
                            Debug.Log("[KSPI]: FNModulePrecooler - Found " + radialAttachedIntakes.Count + " Airintake(s) in children in deeper node");
                            break;
                        }

                        if (attachNode.attachedPart.FindModulesImplementing<FNModulePrecooler>().Any()) continue;

                        foreach (var subAttachNode in attachNode.attachedPart.attachNodes.Where(a => a.attachedPart != null))
                        {
                            Debug.Log("[KSPI]: FNModulePrecooler - look for Air intakes in part " + subAttachNode.attachedPart.name);

                            attachedIntake = subAttachNode.attachedPart.FindModulesImplementing<AtmosphericIntake>().FirstOrDefault();

                            if (attachedIntake != null)
                            {
                                Debug.Log("[KSPI]: FNModulePrecooler - found Airintake in deeper attached part with name " + attachedIntake.name);
                                break;
                            }

                            // then look to connect radial attached children
                            radialAttachedIntakes = subAttachNode.attachedPart.children
                                .Where(p => p.attachMode == AttachModes.SRF_ATTACH)
                                .SelectMany(p => p.FindModulesImplementing<AtmosphericIntake>()).ToList();

                            if (radialAttachedIntakes.Count <= 0) continue;

                            Debug.Log("[KSPI]: FNModulePrecooler - Found " + radialAttachedIntakes.Count + " Airintake(s) in children in even deeper node");
                            break;
                        }
                        if (attachedIntake != null)
                            break;
                    }
                }
            }

            if (attachedIntake != null)
                attachedIntakeName = attachedIntake.name;
            else
            {
                if (radialAttachedIntakes == null )
                    attachedIntakeName = "Null found";
                else if (radialAttachedIntakes.Count > 1)
                    attachedIntakeName = radialAttachedIntakes.Count + " radial intakes found";
                else if (radialAttachedIntakes.Count > 0)
                    attachedIntakeName = radialAttachedIntakes.First().name;
                else
                    attachedIntakeName = "Not found";
            }
        }

        public override void OnUpdate()
        {
            statusStr = functional ? Localizer.Format("#LOC_KSPIE_Preecooler_Active") : Localizer.Format("#LOC_KSPIE_Preecooler_Offline");//"Active.""Offline."
        }

        public void FixedUpdate() // FixedUpdate is also called while not staged
        {
            functional = ((attachedIntake != null && attachedIntake.intakeOpen) || radialAttachedIntakes.Any(i => i.intakeOpen));
        }
    }
}
