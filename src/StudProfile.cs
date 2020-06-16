using System;
using System.Collections.Generic;
using Elements.Geometry;

namespace PrefabricatedPanels
{
    public partial class StudProfile : Profile
    {
        public StudProfile() : base(null, null, Guid.NewGuid(), "Stud")
        {
            var w = Elements.Units.InchesToMeters(3.625);
            var d = Elements.Units.InchesToMeters(1.5);
            var t = 0.001;

            var vertices = new List<Vector3>(){
                new Vector3(-w/2 + t, -d/2 + t),
                new Vector3(-w/2 + t, d/2),
                new Vector3(-w/2, d/2),
                new Vector3(-w/2, -d/2),
                new Vector3(-w/2 + t, -d/2),
                new Vector3(w/2, -d/2),
                new Vector3(w/2, d/2),
                new Vector3(w/2 - t, d/2),
                new Vector3(w/2 - t, -d/2 + t)
            };

            this.Perimeter = new Polygon(vertices);
        }
    }
}