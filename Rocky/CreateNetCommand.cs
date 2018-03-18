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

            Result getObjResult = RhinoGet.GetOneObject("Select the box", false, selFilter, out boxObjRef);
            if (getObjResult == Result.Success)
            {
                Brep boxBrep = boxObjRef.Brep();
                BoundingBox bbox = boxBrep.GetBoundingBox(true);
                Point3d bboxMin = bbox.Min;
                Point3d bboxMax = bbox.Max;
                double xDist = bboxMax.X - bboxMin.X;
                double yDist = bboxMax.Y - bboxMin.Y;
                double zDist = bboxMax.Z - bboxMin.Z;

                Point3d origin = new Point3d(0, 0, 0);
                Point3d xAxisPt = new Point3d(xDist, 0, 0);
                Point3d yAxisPt = new Point3d(0, yDist, 0);

                Plane xyPlane = new Plane(origin, xAxisPt, yAxisPt);

                Rectangle3d rect = new Rectangle3d(xyPlane, xAxisPt, yAxisPt);
                Polyline polyLine = rect.ToPolyline();

                //Point3d worldOrigin = new Point3d(0, 0, 0);
                //Rhino.Geometry.Rec

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
