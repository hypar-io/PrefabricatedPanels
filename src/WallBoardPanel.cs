using System;
using System.Collections.Generic;
using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;

namespace PrefabricatedPanels
{
    public partial class WallBoardPanel : GeometricElement
    {
        public Profile Profile { get; set; }

        public WallBoardPanel(Profile profile, Transform transform = null, Material material = null) : base(transform, material, null, false, Guid.NewGuid(), null)
        {
            this.Profile = profile;
            this.Representation = new Representation(new List<SolidOperation>());
        }

        public override void UpdateRepresentations()
        {
            this.Representation.SolidOperations.Clear();
            this.Representation.SolidOperations.Add(new Extrude(this.Profile, Units.InchesToMeters(0.625), Vector3.ZAxis, false));
        }
    }
}