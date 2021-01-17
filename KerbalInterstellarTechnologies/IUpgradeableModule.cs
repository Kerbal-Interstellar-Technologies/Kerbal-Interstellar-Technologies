using System;

namespace KIT
{
    public interface IUpgradeableModule
    {
        String UpgradeTechnology { get; }
        void upgradePartModule();
    }

    public static class UpgradeableModuleExtensions
    {
        public static bool HasTechsRequiredToUpgrade(this IUpgradeableModule upgModule)
        {
            return PluginHelper.UpgradeAvailable(upgModule.UpgradeTechnology);
        }
    }
}
