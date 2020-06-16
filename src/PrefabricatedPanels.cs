using Elements;
using Elements.Geometry;
using Elements.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrefabricatedPanels
{
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
            var totalFramingLength = 0.0;
            var elements = new List<Element>();

            var studWidth = Elements.Units.InchesToMeters(3.625);
            var studDepth = Elements.Units.InchesToMeters(1.5);
            var tolerance = 0.005;
            var studProfile = new StudProfile();
            var outerStudMaterial = new Material("Stud Frame", Colors.Orange);
            var studMaterial = new Material("Stud", new Color(0.7, 0.7, 0.7, 1.0), 0.8f, 0.8f);
            var headerProfile = new Profile(Polygon.Rectangle(Units.InchesToMeters(3.625), Units.InchesToMeters(3.625)));
            var headerTrans = new Transform(new Vector3(0, Units.InchesToMeters(3.625) / 2));
            // headerProfile.Transform(headerTrans);

            var centerLineMaterial = new Material("Center Line", Colors.Gray);
            var wallBoard = new Material("Wall Board", new Color(0.9, 0.9, 0.9, 0.75), 0.0f, 0.0f);

            foreach (var panel in wallPanels)
            {
                // Draw the panel profiles.
                // var mc = new ModelCurve(panel.Transform.OfPolygon(panel.Profile.Perimeter), centerLineMaterial);
                // elements.Add(mc);

                var wallBoardOffset = panel.Profile.Perimeter.Offset(-tolerance);

                // Offset the panel profile
                var offset = panel.Profile.Perimeter.Offset(-studDepth / 2 - tolerance);
                if (offset.Length == 0)
                {
                    continue;
                }
                var outer = (Polygon)offset[0].Transformed(panel.Transform);

                // Get the plane of the panel
                var panelPlane = outer.Plane();

                // Draw the panel transform;
                // var panelTransform = new Transform(outer.Centroid(), panelPlane.Normal);
                // elements.AddRange(panelTransform.ToModelCurves());

                // Draw the panel frame.

                foreach (var seg in outer.Segments())
                {
                    var l = seg.Length();
                    var d = seg.Direction();
                    var dot = d.Dot(Vector3.ZAxis);
                    var t = seg.TransformAt(0);

                    // Draw the beam transforms
                    // elements.AddRange(t.ToModelCurves());

                    var beamRotation = t.XAxis.AngleTo(panelPlane.Normal);
                    // if(Double.IsNaN(beamRotation))
                    // {
                    // 	Console.WriteLine($"l: {l}, d:{d}, dot:{dot}");
                    // }

                    Vector3 a, b;
                    if (Math.Abs(dot) == 1)
                    {
                        a = seg.Start += d * studDepth / 2;
                        b = seg.End -= d * studDepth / 2;
                    }
                    else
                    {
                        a = seg.Start -= d * studDepth / 2;
                        b = seg.End += d * studDepth / 2;
                    }

                    var cl = new Line(a, b);
                    // var beam = new Beam(cl, (dot == 0.0 && seg.Start.Z > 0.1) ? headerProfile : studProfile, outerStudMaterial, rotation: (Double.IsNaN(beamRotation) ? 0.0 : beamRotation));
                    var beam = new Beam(cl, studProfile, outerStudMaterial, rotation: (Double.IsNaN(beamRotation) ? 0.0 : beamRotation));
                    totalFramingLength += cl.Length();

                    elements.Add(beam);
                }

                // Draw the panel framing.

                var grid = new Grid2d(offset[0]);
                grid.U.DivideByFixedLength(input.StudSpacing);

                var studCls = grid.GetCellSeparators(GridDirection.V);

                // Only take the inner studs.
                var innerCls = studCls.Skip(1).Take(studCls.Count - 2).ToList().Cast<Line>().ToList();
                var trimBoundary = offset[0].Segments();
                var trimmedInnerCls = TrimLinesToBoundary(innerCls, trimBoundary);

                foreach (var trimmedLine in trimmedInnerCls)
                {
                    var t = trimmedLine.TransformAt(0);
                    var beamRotation = t.XAxis.AngleTo(panelPlane.Normal);
                    var innerBeam = new Beam(trimmedLine.Transformed(panel.Transform), studProfile, studMaterial, rotation: beamRotation);
                    totalFramingLength += trimmedLine.Length();
                    elements.Add(innerBeam);
                }

                grid.V.DivideByFixedLength(Units.FeetToMeters(4.0));
                var kickerCls = grid.GetCellSeparators(GridDirection.U);
                var kickerInnerCls = kickerCls.Skip(1).Take(kickerCls.Count - 2).ToList().Cast<Line>().ToList(); ;
                var trimmedKickerCls = TrimLinesToBoundary(kickerInnerCls, trimBoundary);
                foreach (var trimmedKicker in trimmedKickerCls)
                {
                    var beam = new Beam(trimmedKicker.Transformed(panel.Transform), studProfile, studMaterial);
                    totalFramingLength += trimmedKicker.Length();
                    elements.Add(beam);
                }

                if (input.CreateWallBoard)
                {
                    var wallBoardGrid = new Grid2d(wallBoardOffset[0]);
                    wallBoardGrid.U.DivideByFixedLength(Units.FeetToMeters(8.0));
                    wallBoardGrid.V.DivideByFixedLength(Units.FeetToMeters(4.0));

                    if (wallBoardGrid.CellsFlat.Count > 0)
                    {
                        foreach (var cell in wallBoardGrid.CellsFlat)
                        {
                            foreach (Polygon panelPerimeter in cell.GetTrimmedCellGeometry())
                            {
                                var wallBoards = CreateOffsetPanel(panel, studWidth, panelPerimeter, wallBoard);
                                elements.AddRange(new[] { wallBoards.left, wallBoards.right });
                            }
                        }
                    }
                    else
                    {
                        var panelPerimeter = wallBoardOffset[0];
                        var wallBoards = CreateOffsetPanel(panel, studWidth, panelPerimeter, wallBoard);
                        elements.AddRange(new[] { wallBoards.left, wallBoards.right });
                    }
                }

                panelCount++;
            }

            var output = new PrefabricatedPanelsOutputs(panelCount, totalFramingLength);
            output.Model.AddElements(elements);
            return output;
        }

        private static (WallBoardPanel left, WallBoardPanel right) CreateOffsetPanel(WallPanel panel, double studWidth, Polygon panelPerimeter, Material wallBoard)
        {
            var leftTrans = new Transform(panel.Transform);
            leftTrans.Move(leftTrans.ZAxis * studWidth / 2);
            var panelL = new WallBoardPanel(panelPerimeter, leftTrans, wallBoard);

            var rightTrans = new Transform(panel.Transform);
            rightTrans.Move(rightTrans.ZAxis * (-studWidth / 2 - Units.InchesToMeters(0.625)));
            var panelR = new WallBoardPanel(panelPerimeter, rightTrans, wallBoard);
            return (panelL, panelR);
        }

        private static List<Line> TrimLinesToBoundary(List<Line> lines, IList<Line> boundarySegements)
        {
            var trims = new List<Line>();
            foreach (var grid in lines)
            {
                var xsects = new List<Vector3>();
                foreach (var s in boundarySegements)
                {
                    if (!Intersects(s, grid, out Vector3 xsect))
                    {
                        continue;
                    }
                    xsects.Add(xsect);
                }

                if (xsects.Count < 2)
                {
                    continue;
                }

                xsects.Sort(new IntersectionComparer(grid.Start));

                for (var i = 0; i < xsects.Count - 1; i += 2)
                {
                    if (xsects[i].IsAlmostEqualTo(xsects[i + 1]))
                    {
                        continue;
                    }
                    trims.Add(new Line(xsects[i], xsects[i + 1]));
                }
            }
            return trims;
        }

        /// <summary>
        /// https://social.msdn.microsoft.com/Forums/vstudio/en-US/e5993847-c7a9-46ec-8edc-bfb86bd689e3/help-on-line-segment-intersection-algorithm?forum=csharpgeneral
        /// </summary>
        /// <param name="AB"></param>
        /// <param name="CD"></param>
        /// <returns></returns>
        public static bool Intersects(Line AB, Line CD, out Vector3 result)
        {
            double deltaACy = AB.Start.Y - CD.Start.Y;
            double deltaDCx = CD.End.X - CD.Start.X;
            double deltaACx = AB.Start.X - CD.Start.X;
            double deltaDCy = CD.End.Y - CD.Start.Y;
            double deltaBAx = AB.End.X - AB.Start.X;
            double deltaBAy = AB.End.Y - AB.Start.Y;

            double denominator = deltaBAx * deltaDCy - deltaBAy * deltaDCx;
            double numerator = deltaACy * deltaDCx - deltaACx * deltaDCy;

            result = new Vector3();

            if (denominator == 0)
            {
                if (numerator == 0)
                {
                    // collinear. Potentially infinite intersection points.
                    // Check and return one of them.
                    if (AB.Start.X >= CD.Start.X && AB.Start.X <= CD.End.X)
                    {
                        result = AB.Start;
                        return true;
                    }
                    else if (CD.Start.X >= AB.Start.X && CD.Start.X <= AB.End.X)
                    {
                        result = CD.Start;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                { // parallel
                    return false;
                }
            }

            double r = numerator / denominator;
            if (r < 0 || r > 1)
            {
                return false;
            }

            double s = (deltaACy * deltaBAx - deltaACx * deltaBAy) / denominator;
            if (s < 0 || s > 1)
            {
                return false;
            }

            result = new Vector3((AB.Start.X + r * deltaBAx), (AB.Start.Y + r * deltaBAy));
            return true;
        }
    }
}