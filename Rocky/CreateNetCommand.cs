using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using Eto.Drawing;
using Eto.Forms;

namespace Rocky
{
    public class CreateNetCommand : Rhino.Commands.Command
    {
        public static readonly Point3d ORIGIN = new Point3d(0, 0, 0);
        public static readonly double BIRCH_MM = 3.17;
        public static readonly double BIRCH_CM = .317;

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

                RhinoList<Rectangle3d> rectList = generateNetRects(widthHeightDepthVect, thickness: BIRCH_CM);

                // Draw Rectangles
                Polyline polyline;
                foreach (Rectangle3d rect in rectList)
                {
                    polyline = rect.ToPolyline();
                    doc.Objects.AddPolyline(polyline);
                }

                // Draw finger joints
                Line jointLine;
                Point3d rightEdgeBottom, rightEdgeTop;
                foreach(Rectangle3d rect in rectList)
                {
                    rightEdgeBottom = rect.Corner(1);
                    rightEdgeTop = rect.Corner(2);
                    jointLine = new Line(rightEdgeBottom, rightEdgeTop);
                    polyline = generateFingerJoint(jointLine, BIRCH_CM);
                    doc.Objects.AddPolyline(polyline);
                }

                // Go back and draw the first once
                jointLine = new Line(rectList[0].Corner(0), rectList[0].Corner(3));
                polyline = generateFingerJoint(jointLine, BIRCH_CM);
                doc.Objects.AddPolyline(polyline);

                doc.Views.Redraw();
                return Result.Success;

            }
            return Result.Failure;
        }

        protected RhinoList<Rectangle3d> generateNetRects(Vector3d widthHeightDepthVect,
                                                          double thickness = 0)
        {
            RhinoList<Rectangle3d> rectList = new RhinoList<Rectangle3d>();

            double xDist = widthHeightDepthVect.X;
            double yDist = widthHeightDepthVect.Y;
            double zDist = widthHeightDepthVect.Z;

            // Add thickness for fingering gaps
            Point3d origin1 = new Point3d(xDist + (2 * thickness), 0, 0);
            Point3d origin2 = new Point3d(xDist + yDist + (4 * thickness), 0, 0);
            Point3d origin3 = new Point3d(xDist + yDist + xDist + (6 * thickness), 0, 0);

            // Line 4 rectangles X, Y, X, Y; all Z tall
            Rectangle3d rect0 = MakeRect(ORIGIN, xDist, zDist, margin: BIRCH_CM);
            Rectangle3d rect1 = MakeRect(origin1, yDist, zDist, margin: BIRCH_CM);
            Rectangle3d rect2 = MakeRect(origin2, xDist, zDist, margin: BIRCH_CM);
            Rectangle3d rect3 = MakeRect(origin3, yDist, zDist, margin: BIRCH_CM);

            rectList.Add(rect0);
            rectList.Add(rect1);
            rectList.Add(rect2);
            rectList.Add(rect3);

            return rectList;
        }

        protected Polyline generateFingerJoint(Line jointLine, double thickness)
        {
            Point3d currPoint = new Point3d(jointLine.FromX, jointLine.FromY, 0);
            Point3dList points = new Point3dList();
            points.Add(currPoint);

            // An even finger count means the finger will be drawn right of the
            // center line
            double fingerCount = 0;
            double fingerDirection = -1;

            // Loop invariant: incrementing and placing current point will always
            // result in a point before the stoppingY
            while (currPoint.Y + thickness <= jointLine.ToY)
            {
                // Multiplier for right finger on even, vice versa for odd
                fingerDirection = fingerCount % 2 == 0 ? 1 : -1;

                currPoint += new Vector3d(thickness * fingerDirection, 0, 0);
                points.Add(currPoint);
                currPoint += new Vector3d(0, thickness, 0);
                points.Add(currPoint);
                currPoint += new Vector3d(-thickness * fingerDirection, 0, 0);
                points.Add(currPoint);

                fingerCount += 1;
            }

            // Finish the last truncated finger if necessary
            if (currPoint.Y < jointLine.ToY)
            {
                fingerDirection *= -1;
                currPoint += new Vector3d(thickness * fingerDirection, 0, 0);
                points.Add(currPoint);
                currPoint += new Vector3d(0, jointLine.ToY - currPoint.Y, 0);
                points.Add(currPoint);
                currPoint += new Vector3d(-thickness * fingerDirection, 0, 0);
                points.Add(currPoint);
            }

            return new Polyline(points);
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

        protected Rectangle3d MakeRect(Point3d origin, double width, double height,
                                      double margin = 0)
        {
            Point3d leftAdjustedOrigin = origin + new Point3d(-margin, 0, 0);
            Point3d xAxisPt = new Point3d(width + (2 * margin), 0, 0) + leftAdjustedOrigin;
            Point3d yAxisPt = new Point3d(0, height, 0) + leftAdjustedOrigin;

            Vector3d zVector = new Vector3d(0, 0, 1);
            Plane worldXYPlane = new Plane(leftAdjustedOrigin, zVector);

            Rectangle3d rect = new Rectangle3d(worldXYPlane, xAxisPt, yAxisPt);
            return rect;
        }
    }
}
