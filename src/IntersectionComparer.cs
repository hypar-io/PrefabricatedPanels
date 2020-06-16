using System.Collections.Generic;
using Elements.Geometry;

namespace PrefabricatedPanels
{
    internal class IntersectionComparer : IComparer<Vector3>
    {
        private Vector3 _origin;
        public IntersectionComparer(Vector3 origin)
        {
            this._origin = origin;
        }

        public int Compare(Vector3 x, Vector3 y)
        {
            var a = x.DistanceTo(_origin);
            var b = y.DistanceTo(_origin);

            if (a < b)
            {
                return -1;
            }
            else if (a > b)
            {
                return 1;
            }
            return 0;
        }
    }
}