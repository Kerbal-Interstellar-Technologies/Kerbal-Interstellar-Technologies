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
    #region Background Function helper class
    class BackgroundFunctionInfo
    {
        // Fixed Update equiv
        public Action<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part>
            KITBackgroundUpdate;

        public Func<ProtoPartModuleSnapshot, ModuleConfigurationFlags> BackgroundModuleConfiguration;

        public Func<ProtoPartSnapshot, ProtoPartModuleSnapshot, string> KITPartName;

        // Variable supplier equiv
        public Func<ProtoPartModuleSnapshot, ResourceName[]> ResourcesProvided;

        public Func<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part, ResourceName
                , double, bool>
            BackgroundProvideResource;

        public static readonly Type[] KITBackgroundUpdateSignature =
        {
            typeof(IResourceManager), typeof(Vessel), typeof(ProtoPartSnapshot), typeof(ProtoPartModuleSnapshot),
            typeof(Part)
        };

        public static readonly Type[] KITPartNameSignature =
        {
            typeof(ProtoPartSnapshot), typeof(ProtoPartModuleSnapshot), typeof(string)
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
            typeof(Part), typeof(ResourceName), typeof(double), typeof(bool)
        };


        public bool VariableSupplier => BackgroundProvideResource != null && ResourcesProvided != null;

        private static readonly Dictionary<Type, BackgroundFunctionInfo> PreCached =
            new Dictionary<Type, BackgroundFunctionInfo>();

        public BackgroundFunctionInfo(Func<ProtoPartSnapshot, ProtoPartModuleSnapshot, string> kitPartName, Func<ProtoPartModuleSnapshot, ModuleConfigurationFlags> backgroundModuleConfiguration,
            Action<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part>
                kitBackgroundUpdate)
        {
            KITPartName = kitPartName;
            KITBackgroundUpdate = kitBackgroundUpdate;
            BackgroundModuleConfiguration = backgroundModuleConfiguration;
        }

        public BackgroundFunctionInfo(
            Func<ProtoPartSnapshot, ProtoPartModuleSnapshot, string> kitPartName,
            Func<ProtoPartModuleSnapshot, ModuleConfigurationFlags> backgroundModuleConfiguration,
            Action<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part> kitBackgroundUpdate,
            Func<ProtoPartModuleSnapshot, ResourceName[]> resourcesProvided,
            Func<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part, ResourceName,
                double, bool> backgroundProvideResource)
        {
            KITPartName = kitPartName;
            KITBackgroundUpdate = kitBackgroundUpdate;
            BackgroundModuleConfiguration = backgroundModuleConfiguration;
            ResourcesProvided = resourcesProvided;
            BackgroundProvideResource = backgroundProvideResource;
        }

        public static BackgroundFunctionInfo Instance(IKITModule kitModule)
        {
            var type = kitModule.GetType();

            if (PreCached.TryGetValue(type, out var retValue))
            {
                return retValue;
            }

            #region Standard KIT Module support

            var methodInfo = type.GetMethod("KITPartName", KITPartNameSignature);
            if (methodInfo == null)
            {
                Debug.Log($"[BackgroundFunctionInfo] Can not find KITPartName(ProtoPartModuleSnapshot)");
                return null;
            }

            var kitPartName =
                (Func<ProtoPartSnapshot, ProtoPartModuleSnapshot, string>)Delegate.CreateDelegate(
                    typeof(Func<ProtoPartSnapshot, ProtoPartModuleSnapshot, string>), methodInfo);

            methodInfo = type.GetMethod("KITBackgroundUpdate", KITBackgroundUpdateSignature);
            if (methodInfo == null)
            {
                Debug.Log($"[BackgroundFunctionInfo] Can not find KITBackgroundUpdate in {kitModule.KITPartName()}");
                return null;
            }

            var kitBackgroundUpdate =
                (Action<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part>)
                Delegate.CreateDelegate(
                    typeof(Action<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part
                    >), methodInfo);


            methodInfo = type.GetMethod("BackgroundModuleConfiguration", BackgroundModuleConfigurationSignature);
            if (methodInfo == null)
            {
                Debug.Log($"[BackgroundFunctionInfo] Can not find BackgroundModuleConfiguration in {kitModule.KITPartName()}");
                return null;
            }

            var backgroundModuleConfiguration =
                (Func<ProtoPartModuleSnapshot, ModuleConfigurationFlags>)Delegate.CreateDelegate(
                    typeof(Func<ProtoPartModuleSnapshot, ResourceName[]>), methodInfo);

            if (!(kitModule is IKITVariableSupplier))
            {
                var result = new BackgroundFunctionInfo(kitPartName, backgroundModuleConfiguration, kitBackgroundUpdate);
                PreCached[type] = result;
                return result;
            }

            methodInfo = type.GetMethod("ResourcesProvided", ResourcesProvidedSignature);
            if (methodInfo == null)
            {
                Debug.Log(
                    $"[BackgroundFunctionInfo] Can not find ResourcesProvided in {kitModule.KITPartName()} - disabling module");
                return null;
            }

            #endregion

            #region Suppliable resources

            var resourcesProvided =
                (Func<ProtoPartModuleSnapshot, ResourceName[]>)Delegate.CreateDelegate(
                    typeof(Func<ProtoPartModuleSnapshot, ResourceName[]>), methodInfo);

            methodInfo = type.GetMethod("BackgroundProvideResource", BackgroundProvideResourceSignature);
            if (methodInfo == null)
            {
                Debug.Log(
                    $"[BackgroundFunctionInfo] Can not find BackgroundProvideResource in {kitModule.KITPartName()} - disabling module");
                return null;
            }

            var backgroundProvideResource =
                (Func<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part,
                    ResourceName, double, bool>)
                Delegate.CreateDelegate(
                    typeof(Func<IResourceManager, Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part,
                        ResourceName, double, bool>),
                    methodInfo);


            #endregion

            return new BackgroundFunctionInfo(kitPartName, backgroundModuleConfiguration, kitBackgroundUpdate, resourcesProvided,
                backgroundProvideResource);

        }
    }

    #endregion

    class BackgroundModule : IKITModule
    {
        private protected BackgroundFunctionInfo FunctionInfo;
        private protected Vessel Vessel;
        private protected ProtoPartSnapshot ProtoPartSnapshot;
        private protected ProtoPartModuleSnapshot ProtoPartModuleSnapshot;
        private protected Part Part;
        private protected PartModule PartModule;

        public BackgroundModule(BackgroundFunctionInfo functionInfo, Vessel vessel, ProtoPartSnapshot protoPartSnapshot, ProtoPartModuleSnapshot protoPartModuleSnapshot, Part part, PartModule partModule)
        {
            FunctionInfo = functionInfo;
            Vessel = vessel;
            ProtoPartSnapshot = protoPartSnapshot;
            ProtoPartModuleSnapshot = protoPartModuleSnapshot;
            Part = part;
            PartModule = partModule;
        }

        public ModuleConfigurationFlags ModuleConfiguration()
        {
            return FunctionInfo.BackgroundModuleConfiguration(ProtoPartModuleSnapshot);
        }

        public void KITFixedUpdate(IResourceManager resMan)
        {
            FunctionInfo.KITBackgroundUpdate(resMan, Vessel, ProtoPartSnapshot,
                ProtoPartModuleSnapshot, PartModule, Part);
        }

        public string KITPartName()
        {
            return FunctionInfo.KITPartName(ProtoPartSnapshot, ProtoPartModuleSnapshot);
        }
    }

    class BackgroundSuppliableModule : BackgroundModule, IKITVariableSupplier
    {
        public BackgroundSuppliableModule(BackgroundFunctionInfo functionInfo, Vessel vessel, ProtoPartSnapshot protoPartSnapshot, ProtoPartModuleSnapshot protoPartModuleSnapshot, Part part, PartModule partModule) : base(functionInfo, vessel, protoPartSnapshot, protoPartModuleSnapshot, part, partModule)
        {
        }

        public ResourceName[] ResourcesProvided()
        {
            return FunctionInfo.ResourcesProvided(ProtoPartModuleSnapshot);
        }

        public bool ProvideResource(IResourceManager resMan, ResourceName resource, double requestedAmount)
        {
            return FunctionInfo.BackgroundProvideResource(resMan, Vessel, ProtoPartSnapshot,
                ProtoPartModuleSnapshot, PartModule, Part, resource, requestedAmount);
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
                var protoModuleSnapshots = protoPartSnapshot.modules;

                var backgroundModules = protoModuleSnapshots.Zip(modulePrefabs, (snapshot, module) =>
                {
                    var backgroundFunctionInfo = BackgroundFunctionInfo.Instance(module);
                    var pm = module as PartModule;
                    
                    System.Diagnostics.Debug.Assert(pm != null, nameof(pm) + " != null");
                    
                    if (snapshot.moduleName != pm.moduleName)
                    {
                        Debug.Log($"[FindBackgroundModule] Uh oh, order of the prefab modules does not match the proto module order :(");
                        throw new Exception(
                            "[FindBackgroundModule] Uh oh, order of the prefab modules does not match the proto module order :(");
                    }

                    return new BackgroundModule(backgroundFunctionInfo, vessel, protoPartSnapshot, snapshot, prefab,
                        pm);
                });

                Debug.Log($"got {backgroundModules}");
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
