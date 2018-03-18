﻿using System;
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

            Result getObjResult = RhinoGet.GetOneObject("Select the box", false, selFilter, out boxObjRef);
            if (getObjResult == Result.Success)
            {
                Vector3d widthHeightDepthVect = getWidthHeigthDepthVect(boxObjRef);
                double xDist = widthHeightDepthVect.X;
                double yDist = widthHeightDepthVect.Y;
                double zDist = widthHeightDepthVect.Z;

                // Line 4 rectangles X, Y, X, Y; all Z tall

                Rhino.Collections.RhinoList<Rectangle3d> rectList = new Rhino.Collections.RhinoList<Rectangle3d>();

                Point3d worldOrigin = new Point3d(0, 0, 0);
                Point3d origin1 = new Point3d(xDist, 0, 0);
                Point3d origin2 = new Point3d(xDist + yDist, 0, 0);
                Point3d origin3 = new Point3d(xDist + yDist + xDist, 0, 0);

                Rectangle3d rect0 = MakeRect(worldOrigin, xDist, zDist);
                Rectangle3d rect1 = MakeRect(origin1, yDist, zDist);
                Rectangle3d rect2 = MakeRect(origin2, xDist, zDist);
                Rectangle3d rect3 = MakeRect(origin3, yDist, zDist);

                rectList.Add(rect0);
                rectList.Add(rect1);
                rectList.Add(rect2);
                rectList.Add(rect3);

                Polyline polyline;
                foreach (Rectangle3d rect in rectList)
                {
                    polyline = rect.ToPolyline();
                    doc.Objects.AddPolyline(polyline);
                }
                doc.Views.Redraw();
                return Result.Success;

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
            Point3d xAxisPt = new Point3d(width, 0, 0) + origin;
            Point3d yAxisPt = new Point3d(0, height, 0) + origin;

            Vector3d zVector = new Vector3d(0, 0, 1);
            Plane worldXYPlane = new Plane(origin, zVector);

            Rectangle3d rect = new Rectangle3d(worldXYPlane, xAxisPt, yAxisPt);
            return rect;
        }
    }
}
