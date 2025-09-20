using UnityEngine;

namespace FauxLightVolumes
{
    /// <summary>
    /// Attribute for localizing property labels & tooltips in inspectors.
    /// Resides in runtime assembly so that non-Editor scripts can reference it safely.
    /// </summary>
    public class LocalizedLabelAttribute : PropertyAttribute
    {
        public readonly string Key;
        public readonly string TooltipKey;

        /// <param name="key">Translation key. If null, field/property name is used.</param>
        /// <param name="tooltipKey">Tooltip translation key. Defaults to key + ".tooltip" if null.</param>
        public LocalizedLabelAttribute(string key = null, string tooltipKey = null)
        {
            Key = key;
            TooltipKey = tooltipKey;
        }
    }
}
