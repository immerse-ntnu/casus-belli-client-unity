using UnityEngine;

namespace TorbuTils
{
    namespace Anime
    {
        public class QuaternionController : AnimController<Quaternion> { protected override Quaternion GetActualValue(Quaternion startValue, Quaternion endValue, float relative) => Quaternion.LerpUnclamped(startValue, endValue, relative); }
    }
}
