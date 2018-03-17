using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using Eto.Drawing;
using Eto.Forms;

namespace Rocky
{
    public class CreateNetCommand : Rhino.Commands.Command
    {
        public override string EnglishName
        {
            get { return "CreateNet"; }
        }

        protected override Result RunCommand(Rhino.RhinoDoc doc, RunMode mode)
        {
            const Rhino.DocObjects.ObjectType selFilter = Rhino.DocObjects.ObjectType.PolysrfFilter
                                                        | Rhino.DocObjects.ObjectType.Extrusion;
            Rhino.DocObjects.ObjRef boxObjRef;
            // Guid boxObjGuid = Guid.Empty;

            Rhino.Commands.Result getObjResult = Rhino.Input.RhinoGet.GetOneObject("Select the box", false, selFilter, out boxObjRef);
            if (getObjResult == Result.Success)
            {
                Brep boxBrep = boxObjRef.Brep();
                Rhino.Geometry.BoundingBox bbox = boxBrep.GetBoundingBox(true);
                Point3d bboxMin = bbox.Min;
                Point3d bboxMax = bbox.Max;
                double xDist = bboxMax.X - bboxMin.X;
                double yDist = bboxMax.Y - bboxMin.Y;
                double zDist = bboxMax.Z - bboxMin.Z;

                Point3d origin = new Rhino.Geometry.Point3d(0, 0, 0);
                Point3d xAxisPt = new Rhino.Geometry.Point3d(xDist, 0, 0);
                Point3d yAxisPt = new Rhino.Geometry.Point3d(0, yDist, 0);

                Plane xyPlane = new Rhino.Geometry.Plane(origin, xAxisPt, yAxisPt);

                Rhino.Geometry.Rectangle3d rect = new Rhino.Geometry.Rectangle3d(xyPlane, xAxisPt, yAxisPt);
                Rhino.Geometry.Polyline polyLine = rect.ToPolyline();

                if (doc.Objects.AddPolyline(polyLine) != Guid.Empty)
                {
                    doc.Views.Redraw();
                    return Result.Success;
                }
            }
            return Result.Failure;
        }


        //public Rhino.Geometry.Curve MakeRectangleFromXY(double x, double y)
        //{
        //    Rhino.Geometry.Curve rect = new Rhino.Geometry.Curve
        //}
    }
}
