using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using KIT.Resources;
using KIT.ResourceScheduler;
using UnityEngine;

namespace KIT.ResourceScheduler
{
    class BackgroundFunction
    {
        // Fixed Update equiv
        public Action<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part>
            KITBackgroundUpdate;

        public Func<ProtoPartModuleSnapshot, ModuleConfigurationFlags> BackgroundModuleConfiguration;

        // Variable supplier equiv
        public Func<ProtoPartModuleSnapshot, ResourceName[]> ResourcesProvided;

        public Func<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part, ResourceName
                , double>
            BackgroundProvideResource;

        public static readonly Type[] KITBackgroundUpdateSignature =
        {
            typeof(IResourceManager), typeof(Vessel), typeof(ProtoPartSnapshot), typeof(ProtoPartModuleSnapshot),
            typeof(Part)
        };

        public static readonly Type[] BackgroundModuleConfigurationSignature =
        {
            typeof(ProtoPartModuleSnapshot), typeof(ModuleConfigurationFlags)
        };

        public static readonly Type[] ResourcesProvidedSignature =
        {
            typeof(ProtoPartModuleSnapshot), typeof(ResourceName[])
        };

        public static readonly Type[] BackgroundProvideResourceSignature =
        {
            typeof(IResourceManager), typeof(Vessel), typeof(ProtoPartSnapshot), typeof(ProtoPartModuleSnapshot),
            typeof(Part), typeof(ResourceName), typeof(double)
        };


        public bool VariableSupplier => BackgroundProvideResource != null && ResourcesProvided != null;

        private static readonly Dictionary<Type, BackgroundFunction> _preCached =
            new Dictionary<Type, BackgroundFunction>();

        public BackgroundFunction(Func<ProtoPartModuleSnapshot, ModuleConfigurationFlags> backgroundModuleConfiguration,
            Action<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part>
                kitBackgroundUpdate)
        {
            KITBackgroundUpdate = kitBackgroundUpdate;
            BackgroundModuleConfiguration = backgroundModuleConfiguration;
        }

        public BackgroundFunction(Func<ProtoPartModuleSnapshot, ModuleConfigurationFlags> backgroundModuleConfiguration,
            Action<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part>
                kitBackgroundUpdate, Func<ProtoPartModuleSnapshot, ResourceName[]> resourcesProvided,
            Func<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part, ResourceName,
                double> backgroundProvideResource) : this(backgroundModuleConfiguration, kitBackgroundUpdate)
        {
            KITBackgroundUpdate = kitBackgroundUpdate;
            BackgroundModuleConfiguration = backgroundModuleConfiguration;
            ResourcesProvided = resourcesProvided;
            BackgroundProvideResource = backgroundProvideResource;

        }

        public static BackgroundFunction Instance(PartModule partModule)
        {
            var type = partModule.GetType();

            if (_preCached.TryGetValue(type, out var retValue))
            {
                return retValue;
            }

            var methodInfo = type.GetMethod("KITBackgroundUpdate", KITBackgroundUpdateSignature);
            if (methodInfo == null)
            {
                Debug.Log($"[BackgroundFunction] Can not find KITBackgroundUpdate in {partModule.ClassName}");
                return null;
            }

            // var backgroundFunction = new BackgroundFunction();

            #region Standard KIT Module support

            var kitBackgroundUpdate =
                (Action<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part>)
                Delegate.CreateDelegate(
                    typeof(Action<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part
                    >), methodInfo);


            methodInfo = type.GetMethod("BackgroundModuleConfiguration", BackgroundModuleConfigurationSignature);
            if (methodInfo == null)
            {
                Debug.Log($"[BackgroundFunction] Can not find BackgroundModuleConfiguration in {partModule.ClassName}");
                return null;
            }

            var backgroundModuleConfiguration =
                (Func<ProtoPartModuleSnapshot, ModuleConfigurationFlags>)Delegate.CreateDelegate(
                    typeof(Func<ProtoPartModuleSnapshot, ResourceName[]>), methodInfo);

            #endregion

            if (!(partModule is IKITVariableSupplier))
            {
                var result = new BackgroundFunction(backgroundModuleConfiguration, kitBackgroundUpdate);
                _preCached[type] = result;
                return result;
            }

            methodInfo = type.GetMethod("ResourcesProvided", ResourcesProvidedSignature);
            if (methodInfo == null)
            {
                Debug.Log(
                    $"[BackgroundFunction] Can not find ResourcesProvided in {partModule.ClassName} - disabling module");
                return null;
            }

            var resourcesProvided =
                (Func<ProtoPartModuleSnapshot, ResourceName[]>)Delegate.CreateDelegate(
                    typeof(Func<ProtoPartModuleSnapshot, ResourceName[]>), methodInfo);

            methodInfo = type.GetMethod("BackgroundProvideResource", BackgroundProvideResourceSignature);
            if (methodInfo == null)
            {
                Debug.Log(
                    $"[BackgroundFunction] Can not find BackgroundProvideResource in {partModule.ClassName} - disabling module");
                return null;
            }

            var backgroundProvideResource =
                (Func<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part,
                    ResourceName, double>)
                Delegate.CreateDelegate(
                    typeof(Func<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part,
                        ResourceName, double>),
                    methodInfo);


            // Debug.Log($"[KITResourceVesselModule] MethodInfo for {partModule} is {methodInfo}");

            return new BackgroundFunction(backgroundModuleConfiguration, kitBackgroundUpdate, resourcesProvided,
                backgroundProvideResource);

        }



    }


    public partial class KITResourceVesselModule : VesselModule
    {
        private void FindBackgroundModules()
        {
            var protoPartSnapshots = vessel.protoVessel.protoPartSnapshots;

            foreach (var protoPartSnapshot in protoPartSnapshots)
            {
                // get the part prefab, so we can get the modules on the part
                var prefab = PartLoader.getPartInfoByName(protoPartSnapshot.partName).partPrefab;

                // and get the prefab modules
                var modulePrefabs = prefab.FindModulesImplementing<IKITModule>();

                // 

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
