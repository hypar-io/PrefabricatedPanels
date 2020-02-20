using Elements;
using Elements.Analysis;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrefabricatedPanels
{
	public class WallBoardPanel : GeometricElement
	{
		public Profile Profile{get;set;}

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

    public class StudProfile : Profile
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

    public static class PrefabricatedPanels
    {
        /// <summary>
        /// The PrefabricatedPanels function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A PrefabricatedPanelsOutputs instance containing computed results and the model with any new elements.</returns>
        public static PrefabricatedPanelsOutputs Execute(Dictionary<string, Model> inputModels, PrefabricatedPanelsInputs input)
        {
			var wallPanels = inputModels["WallPanels"].AllElementsOfType<WallPanel>();
			
			var panelCount = 0;
			var elements = new List<Element>();

			var studWidth = Elements.Units.InchesToMeters(3.625);
			var studDepth = Elements.Units.InchesToMeters(1.5);
			var tolerance = 0.005;
			var studProfile = new StudProfile();
			var outerStudMaterial = new Material("Stud Frame", Colors.Red);
			var studMaterial = new Material("Stud", new Color(0.7, 0.7, 0.7, 1.0), 0.8f, 0.8f);
			var centerLineMaterial = new Material("Center Line", Colors.Gray);
			// var colorScale = new ColorScale(new List<Color>{Colors.Cyan, Colors.Orange}, 5);
			var wallBoard = new Material("Wall Board", new Color(0.9, 0.9, 0.9, 0.75), 0.0f, 0.0f);

			foreach(var panel in wallPanels)
			{
				// Draw the panel profiles.
				// var mc = new ModelCurve(panel.Transform.OfPolygon(panel.Profile.Perimeter), centerLineMaterial);
				// elements.Add(mc);

				// Offset the panel profile
				var offset = panel.Profile.Perimeter.Offset(-studDepth/2 - tolerance);
				if(offset.Length == 0)
				{
					continue;
				}
				var outer = panel.Transform.OfPolygon(offset[0]);

				// Get the plane of the panel
				var panelPlane = outer.Plane();

				// Draw the panel transform;
				// var panelTransform = new Transform(outer.Centroid(), panelPlane.Normal);
				// elements.AddRange(panelTransform.ToModelCurves());

				// Draw the panel frame.
				foreach(var seg in outer.Segments())
				{
					var d = seg.Direction();
					var dot = d.Dot(Vector3.ZAxis);
					var t = seg.TransformAt(0);

					// Draw the beam transforms
					// elements.AddRange(t.ToModelCurves());

					var beamRotation = t.XAxis.AngleTo(panelPlane.Normal);
					
					Vector3 a, b;
					if(Math.Abs(dot) == 1)
					{
						a = seg.Start += d * studDepth/2;
						b = seg.End -= d * studDepth/2;
					}
					else
					{
						a = seg.Start -= d * studDepth/2;
						b = seg.End += d * studDepth/2;
					}

					var beam = new Beam(new Line(a, b), studProfile, outerStudMaterial, rotation: beamRotation);
					elements.Add(beam);
				}

				// Draw the panel framing.
				var grid = new Grid2d(offset[0]);
				grid.U.DivideByApproximateLength(input.StudSpacing);

				var studCls = grid.GetCellSeparators(GridDirection.V);

				// Only take the inner studs.
				studCls = studCls.Skip(1).Take(studCls.Count - 2).ToList();
				foreach(var sep in studCls)
				{
					var cl = (Line)sep;
					var t = cl.TransformAt(0);
					var beamRotation = t.XAxis.AngleTo(panelPlane.Normal);
					var innerBeam = new Beam(panel.Transform.OfLine(cl), studProfile, studMaterial, rotation: beamRotation);
					elements.Add(innerBeam);
				}

				// Create the wall board panels
				var wallBoardGrid = new Grid2d(offset[0]);
				wallBoardGrid.U.DivideByApproximateLength(Units.FeetToMeters(8.0));
				wallBoardGrid.V.DivideByApproximateLength(Units.FeetToMeters(4.0));
				foreach(var cell in wallBoardGrid.CellsFlat)
				{
					var panelPerimeter = (Polygon)cell.GetCellGeometry();
					var leftTrans = new Transform(panel.Transform);
					leftTrans.Move(leftTrans.ZAxis * studWidth/2);
					var panelL = new WallBoardPanel(panelPerimeter, leftTrans, wallBoard);

					var rightTrans = new Transform(panel.Transform);
					rightTrans.Move(rightTrans.ZAxis * (-studWidth/2 - Units.InchesToMeters(0.625)));
					var panelR = new WallBoardPanel(panelPerimeter, rightTrans, wallBoard);
					elements.Add(panelL);
					elements.Add(panelR);
				}

				panelCount++;
			}

			var output = new PrefabricatedPanelsOutputs(panelCount);
			output.model.AddElements(elements);
            return output;
        }
    }
}