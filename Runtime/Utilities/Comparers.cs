using System.Collections.Generic;
using UnityEngine;

namespace mactinite.TooolboxCommons.Utilities
{
    public static class Comparers
    {
        public class DistanceComparer : IComparer<Collider2D>
        {
            private Transform _source;

            public DistanceComparer(Transform source)
            {
                _source = source;
            }

            public int Compare(Collider2D a, Collider2D b)
            {
                var sourcePosition = _source.position;
                return Vector2
                    .Distance(a.transform.position, sourcePosition)
                    .CompareTo(Vector2.Distance(b.transform.position, sourcePosition));
            }
        }
    }
}
