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

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            const ObjectType selFilter = ObjectType.PolysrfFilter | ObjectType.Extrusion;
            ObjRef boxObjRef;

            Result getObjResult = RhinoGet.GetOneObject("Select the box", false, selFilter, out boxObjRef);
            if (getObjResult == Result.Success)
            {
                drawNetFromObjRef(boxObjRef, doc);
                return Result.Success;
            }
            return Result.Failure;
        }

        /*
         * Wrapper function for drawing net
         *
         * TODO: shrinkToDimensions paramater makes sure that the resulting box
         * is no larger than the bounding box, as opposed to making the inner
         * void of the walls have equal dimensions as the bounding box. Use this
         * when you are making a box that serves as a void within a larger mold.
         *
         */
        protected void drawNetFromObjRef(ObjRef boxObjRef, RhinoDoc doc, bool shrinkToDimensions = false)
        {
            Vector3d widthHeightDepthVect = getWidthHeigthDepthVect(boxObjRef);
            Point3d bottomRightmostPoint;

            RhinoList<Rectangle3d> rectList = generateNetRects(widthHeightDepthVect, out bottomRightmostPoint,
                                                               thickness: BIRCH_CM);

            Polyline polyline;
            Polyline[] explodedLines;
            Line jointLine;
            Point3d rightEdgeBottom, rightEdgeTop;

            // Draw the first finger leftmost before iterating
            jointLine = new Line(rectList[0].Corner(0), rectList[0].Corner(3));
            polyline = generateFingerJoint(jointLine, BIRCH_CM, rightOnly: true);
            doc.Objects.AddPolyline(polyline);

            // Kludgy rectangle count but whatever
            int rectIndex = 0;

            foreach (Rectangle3d rect in rectList)
            {
                // First draw fingers
                rightEdgeBottom = rect.Corner(1);
                rightEdgeTop = rect.Corner(2);
                jointLine = new Line(rightEdgeBottom, rightEdgeTop);

                // Draw on both sides of seam unless we are on the rightmost rectangle
                if (rectIndex == 3)
                {
                    polyline = generateFingerJoint(jointLine, BIRCH_CM, leftOnly: true);
                }
                else
                {
                    polyline = generateFingerJoint(jointLine, BIRCH_CM);
                }
                doc.Objects.AddPolyline(polyline);

                // Then draw rectangle itself, explode, and remove seams
                polyline = rect.ToPolyline();
                explodedLines = polyline.BreakAtAngles(Math.PI / 2);
                doc.Objects.AddPolyline(explodedLines[0]);
                doc.Objects.AddPolyline(explodedLines[2]);

                rectIndex += 1;
            }

            // Finally, draw bottom rectangle
            Rectangle3d bottomRect = generateBottomRect(widthHeightDepthVect,
                                                       bottomRightmostPoint);
            doc.Objects.AddPolyline(bottomRect.ToPolyline());

            doc.Views.Redraw();
        }

        protected RhinoList<Rectangle3d> generateNetRects(Vector3d widthHeightDepthVect,
                                                          out Point3d bottomRightmostPoint,
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

            // Set the bottomRightmost point so caller function can keep drawing
            // where we leave off
            bottomRightmostPoint = origin3 + new Vector3d(rect3.Width, 0, 0);

            return rectList;
        }

        protected Rectangle3d generateBottomRect(Vector3d widthHeightDepthVect,
                                                 Point3d origin, double thickness = 0)
        {
            double xDist = widthHeightDepthVect.X;
            double yDist = widthHeightDepthVect.Y;

            return MakeRect(origin, xDist, yDist);
        }

        protected Polyline generateFingerJoint(Line jointLine, double thickness,
                                              bool leftOnly = false, bool rightOnly = false)
        {
            Point3d currPoint = new Point3d(jointLine.FromX, jointLine.FromY, 0);
            Point3dList points = new Point3dList();
            points.Add(currPoint);

            // An even finger count means the finger will be drawn right of the
            // center line
            int fingerCount = 0;
            int fingerDirection = -1;
            bool skipFinger = false;

            // Loop invariant: incrementing and placing current point will always
            // result in a point before the stoppingY
            while (currPoint.Y + thickness <= jointLine.ToY)
            {
                // Multiplier for right finger on even, vice versa for odd
                fingerDirection = fingerCount % 2 == 0 ? 1 : -1;

                // If we have leftOnly or rightOnly and we will make a finger
                // in the right or left direction, respectively, then skip
                // that and just increment upwards
                skipFinger = fingerDirection == 1 && leftOnly
                    || fingerDirection == -1 && rightOnly;

                if (skipFinger)
                {
                    currPoint += new Vector3d(0, thickness, 0);
                    points.Add(currPoint);
                }
                else
                {
                    currPoint += new Vector3d(thickness * fingerDirection, 0, 0);
                    points.Add(currPoint);
                    currPoint += new Vector3d(0, thickness, 0);
                    points.Add(currPoint);
                    currPoint += new Vector3d(-thickness * fingerDirection, 0, 0);
                    points.Add(currPoint);
                }

                fingerCount += 1;
            }

            // Finish the last truncated finger if necessary
            if (currPoint.Y < jointLine.ToY)
            {
                fingerDirection = fingerCount % 2 == 0 ? 1 : -1;
                skipFinger = fingerDirection == 1 && leftOnly
                    || fingerDirection == -1 && rightOnly;

                if (skipFinger)
                {
                    currPoint += new Vector3d(0, jointLine.ToY - currPoint.Y, 0);
                    points.Add(currPoint);
                }
                else
                {
                    currPoint += new Vector3d(thickness * fingerDirection, 0, 0);
                    points.Add(currPoint);
                    currPoint += new Vector3d(0, jointLine.ToY - currPoint.Y, 0);
                    points.Add(currPoint);
                    currPoint += new Vector3d(-thickness * fingerDirection, 0, 0);
                    points.Add(currPoint);
                }
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
