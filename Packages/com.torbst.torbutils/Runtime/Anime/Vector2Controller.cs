using UnityEngine;

namespace TorbuTils
{
    namespace Anime
    {
        public class Vector2Controller : AnimController<Vector2> { protected override Vector2 GetActualValue(Vector2 startValue, Vector2 endValue, float relative) => Vector2.LerpUnclamped(startValue, endValue, relative); }
    }
}
