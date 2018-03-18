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
                Vector3d widthHeightDepthVect = getWidthHeigthDepthVect(boxObjRef);
                Point3d worldOrigin = new Point3d(0, 0, 0);
                Rectangle3d rect = MakeRect(worldOrigin, widthHeightDepthVect.X, widthHeightDepthVect.Y);
                Polyline polyLine = rect.ToPolyline();

                if (doc.Objects.AddPolyline(polyLine) != Guid.Empty)
                {
                    doc.Views.Redraw();
                    return Result.Success;
                }
            }
            return Result.Failure;
        }

        protected Vector3d getWidthHeigthDepthVect(Rhino.DocObjects.ObjRef boxObjRef)
        {
            Brep boxBrep = boxObjRef.Brep();
            BoundingBox bbox = boxBrep.GetBoundingBox(true);
            Point3d bboxMin = bbox.Min;
            Point3d bboxMax = bbox.Max;
            double xDist = bboxMax.X - bboxMin.X;
            double yDist = bboxMax.Y - bboxMin.Y;
            double zDist = bboxMax.Z - bboxMin.Z;

            return new Vector3d(xDist, yDist, zDist);
        }

        protected Rectangle3d MakeRect(Point3d origin, double width, double height)
        {
            Point3d xAxisPt = new Rhino.Geometry.Point3d(width, 0, 0);
            Point3d yAxisPt = new Rhino.Geometry.Point3d(0, height, 0);

            Point3d worldOrigin = new Point3d(0, 0, 0);
            Vector3d zVector = new Vector3d(0, 0, 1);
            Plane worldXYPlane = new Rhino.Geometry.Plane(worldOrigin, zVector);

            Rectangle3d rect = new Rectangle3d(worldXYPlane, xAxisPt, yAxisPt);
            return rect;
        }
    }
}
