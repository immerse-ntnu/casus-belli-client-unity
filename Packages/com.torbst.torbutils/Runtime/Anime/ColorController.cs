using UnityEngine;

namespace TorbuTils
{
    namespace Anime
    {
        public class ColorController : AnimController<Color> { protected override Color GetActualValue(Color startValue, Color endValue, float relative) => Color.LerpUnclamped(startValue, endValue, relative); }
    }
}
