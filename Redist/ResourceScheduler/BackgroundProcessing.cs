using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KIT.ResourceScheduler
{
    class BackgroundFunction
    {
        Action<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule>
            KITBackgroundUpdate;

        Func<ProtoPartModuleSnapshot, int, bool, bool, bool> BackgroundModuleConfiguration;

    }


    public partial class KITResourceVesselModule
    {
        private static readonly Type[] KITModuleSignature =
        {
            typeof(IResourceManager), typeof(Vessel), typeof(ProtoPartSnapshot), typeof(ProtoPartModuleSnapshot),
            typeof(Part)
        };

        protected static Action<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule> GetFunction(PartModule p)
        {
            var type = p.GetType();
            var methodInfo = type.GetMethod("KITBackgroundUpdate", KITModuleSignature);
            Debug.Log($"[KITResourceVesselModule] MethodInfo for {p} is {methodInfo}");

            // return Action<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule >(methodInfo);

            return null;
        }

        private void FindBackgroundModules()
        {
            var protoPartSnapshots = vessel.protoVessel.protoPartSnapshots;

            foreach (var protoPartSnapshot in protoPartSnapshots)
            {
                // get the part prefab, so we can get the modules on the part
                var prefab = PartLoader.getPartInfoByName(protoPartSnapshot.partName).partPrefab;

                // and get the prefab modules
                var modulePrefabs = prefab.FindModulesImplementing<PartModule>();

                /*
                foreach (var moduleSnapshot in protoPartSnapshot.modules)
                {

                }
                */

                modulePrefabs.ForEach(pm => GetFunction(pm));

            }

        }

        /// <summary>
        /// Entry point for performing background processing on a vessel
        /// </summary>
        private void PerformBackgroundProcessing()
        {
            FindBackgroundModules();
        }
    }
}
