using UnityEngine;

namespace TorbuTils
{
    namespace Anime
    {
        public class Vector3Controller : AnimController<Vector3> { protected override Vector3 GetActualValue(Vector3 startValue, Vector3 endValue, float relative) => Vector3.LerpUnclamped(startValue, endValue, relative); }
    }
}
