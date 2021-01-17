// This code is released under the [unlicense](http://unlicense.org/).
using KSP.UI.Screens;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace KIT
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class RDColoredUpgradeIcon : MonoBehaviour
    {
        RDNode _selectedNode;

        readonly Color _greenColor = new Color(0.717f, 0.819f, 0.561f);  // light green RGB(183,209,143)

        FieldInfo _fieldInfoRdPartList;

        public void Update()
        {
            // Do nothing if there is no PartList
            if (RDController.Instance == null || RDController.Instance.partList == null) return;

            // In case the node is deselected
            if (RDController.Instance.node_selected == null)
                _selectedNode = null;

            // Do nothing if the tooltip hasn't changed since last update
            if (_selectedNode == RDController.Instance.node_selected)
                return;

            // Get the the selected node and part list ui object
            _selectedNode = RDController.Instance.node_selected;

            // retrieve fieldInfo type
            if (_fieldInfoRdPartList == null)
                _fieldInfoRdPartList = typeof(RDPartList).GetField("partListItems", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);

            Debug.Assert(_fieldInfoRdPartList != null, nameof(_fieldInfoRdPartList) + " != null");

            var items = (List<RDPartListItem>)_fieldInfoRdPartList.GetValue(RDController.Instance.partList);

            var upgradedTemplateItems = items.Where(p => !p.isPart && p.upgrade != null).ToList();

            foreach (RDPartListItem item in upgradedTemplateItems)
            {
                //Debug.Log("[KSPI]: RDColoredUpgradeIcon upgrade name " + item.upgrade.name + " upgrade techRequired: " + item.upgrade.techRequired);

                item.gameObject.GetComponent<UnityEngine.UI.Image>().color = _greenColor;
            }
        }
    }
}
