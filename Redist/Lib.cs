using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KIT
{
    public static class Lib
    {
        public static double GetDouble(ProtoPartModuleSnapshot partModuleSnapshot, string name, double defaultValue = 0.0)
        {
            double output = 0;
            var s = partModuleSnapshot.moduleValues.TryGetValue(name, ref output);
            if (!s) return defaultValue;

            if (double.IsNaN(output) || double.IsInfinity(output))
            {
                Debug.Log($"[Lib.GetDouble] got unexpected Infinity or NaN for {partModuleSnapshot}, for name {name}, returning default value");
                output = defaultValue;
            }

            return output;
        }
    }
}
