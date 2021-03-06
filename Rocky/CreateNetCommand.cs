﻿using System;
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
        public static readonly double ZERO_CM = 0.0;

        public override string EnglishName
        {
            get { return "CreateNet"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            const ObjectType selFilter = ObjectType.PolysrfFilter | ObjectType.Extrusion;
            bool value = false;
            double preShrinkValue = ZERO_CM;

            OptionToggle shrinkToDimensions = new OptionToggle(value, "Off", "On");
            OptionDouble preShrink = new OptionDouble(preShrinkValue, 0.0, 1.0);
            ObjRef boxObjRef = null;

            GetObject go = new GetObject();
            go.SetCommandPrompt("Select the box to net-ify");
            go.AddOptionToggle("Constrain", ref shrinkToDimensions);
            go.AddOptionDouble("PreShrink", ref preShrink, "Preshrink");
            go.GeometryFilter = selFilter;

            go.EnableClearObjectsOnEntry(true);
            go.EnableUnselectObjectsOnExit(true);

            // TODO: clean up this hot mess
            for (;;)
            {
                GetResult getObjResult = go.Get();
                if (getObjResult == GetResult.Object)
                {
                    boxObjRef = go.Object(0);
                    boxObjRef.Object().Select(on: true, syncHighlight: true);
                    go.EnablePreSelect(false, true);
                    go.AcceptNothing(true);
                    continue;
                }

                else if (getObjResult == GetResult.Cancel)
                {
                    return Result.Cancel;
                }

                // Case where user presses enter
                else if (getObjResult == GetResult.Nothing)
                {
                    if (boxObjRef != null)
                    {
                        preShrinkValue = preShrink.CurrentValue;
                        if (preShrinkValue != ZERO_CM)
                        {
                            boxObjRef = shrinkBoxObj(boxObjRef, preShrinkValue);
                        }
                        drawNetFromObjRef(boxObjRef, doc, shrinkToDimensions.CurrentValue);
                        return Result.Success;
                    }
                }
            }

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
        protected void drawNetFromObjRef(ObjRef objRef, RhinoDoc doc, bool shrinkToDimensions = false)
        {
            //drawBoxNet(objRef, doc);
            drawPolyNet(objRef, doc, BIRCH_CM, shrinkToDimensions);
        }

        private void drawBoxNet(ObjRef boxObjRef, RhinoDoc doc, bool shrinkToDimensions = false)
        {
            Vector3d widthHeightDepthVect = getWidthHeigthDepthVect(boxObjRef);
            Point3d bottomRightmostPoint;

            double effectiveNetThickness = shrinkToDimensions ? 0 : BIRCH_CM;

            RhinoList<Rectangle3d> rectList = generateNetRects(widthHeightDepthVect, out bottomRightmostPoint,
                                                               thickness: effectiveNetThickness);

            Polyline polyline;
            Polyline[] explodedLines;
            Line jointLine;
            Point3d rightEdgeBottom, rightEdgeTop;

            // Draw the first finger leftmost before iterating
            jointLine = new Line(rectList[0].Corner(0), rectList[0].Corner(3));
            polyline = generateFingerJoint(jointLine, BIRCH_CM);
            doc.Objects.AddPolyline(polyline);

            foreach (Rectangle3d rect in rectList)
            {
                // First draw fingers
                rightEdgeBottom = rect.Corner(1);
                rightEdgeTop = rect.Corner(2);
                jointLine = new Line(rightEdgeBottom, rightEdgeTop);

                // Draw on both sides of seam
                polyline = generateFingerJoint(jointLine, BIRCH_CM);
                doc.Objects.AddPolyline(polyline);

                // Then draw rectangle itself, explode, and remove seams
                polyline = rect.ToPolyline();
                explodedLines = polyline.BreakAtAngles(Math.PI / 2);
                doc.Objects.AddPolyline(explodedLines[0]);
                doc.Objects.AddPolyline(explodedLines[2]);
            }

            // Finally, draw bottom rectangle
            bottomRightmostPoint += new Vector3d(BIRCH_CM / 2, 0, 0);
            Rectangle3d bottomRect = generateBottomRect(widthHeightDepthVect,
                                                        bottomRightmostPoint,
                                                        thickness: BIRCH_CM);
            doc.Objects.AddPolyline(bottomRect.ToPolyline());

            doc.Views.Redraw();
        }

        private void drawPolyNet(ObjRef objRef, RhinoDoc doc, double thickness, bool shrinkToDimensions = false)
        {
            // Get the section polyline
            Polyline sectionPolyline = getSectionPolyline(objRef);

            double polyDepth = getWidthHeigthDepthVect(objRef).Z;

            double effectiveNetThickness = shrinkToDimensions ? -thickness : thickness;

             //Generate rectangles + polygon based on the face dimensions
            Point3d bottomRightmostPoint;
            RhinoList<Rectangle3d> rectList = generatePolyRects(sectionPolyline, polyDepth, out bottomRightmostPoint, effectiveNetThickness);

            Polyline polyline;
            Polyline[] explodedLines;
            Line jointLine;
            Point3d rightEdgeBottom, rightEdgeTop;

            // Draw the first finger leftmost before iterating
            jointLine = new Line(rectList[0].Corner(0), rectList[0].Corner(3));
            polyline = generateFingerJoint(jointLine, thickness);
            doc.Objects.AddPolyline(polyline);

            foreach (Rectangle3d rect in rectList)
            {
                // First draw fingers
                rightEdgeBottom = rect.Corner(1);
                rightEdgeTop = rect.Corner(2);
                jointLine = new Line(rightEdgeBottom, rightEdgeTop);

                // Draw on both sides of seam
                polyline = generateFingerJoint(jointLine, thickness);
                doc.Objects.AddPolyline(polyline);

                // Then draw rectangle itself, explode, and remove seams
                polyline = rect.ToPolyline();
                explodedLines = polyline.BreakAtAngles(Math.PI / 2);
                doc.Objects.AddPolyline(explodedLines[0]);
                doc.Objects.AddPolyline(explodedLines[2]);
            }
            doc.Views.Redraw();

            // Finally, draw bottom rectangle
            bottomRightmostPoint += new Vector3d(effectiveNetThickness / 2, 0, 0);
            Curve bottomCurve = generatePolyBottomCurve(objRef, bottomRightmostPoint,
                                                        thickness, shrinkToDimensions);
            doc.Objects.AddCurve(bottomCurve);

            doc.Views.Redraw();
        }

        private Curve generatePolyBottomCurve(ObjRef objRef, Point3d origin,
                                           double thickness, bool shrinkToDimensions = false)
        {
            // First get the section curve (not poly) and use built-in
            // offset, not a scale transform, to increment
            Brep brep = objRef.Brep();
            Vector3d zNorm = new Vector3d(0, 0, 1);
            Plane worldXYPlane = new Plane(ORIGIN, zNorm);
            Curve[] contours = Brep.CreateContourCurves(brep, worldXYPlane);
            Curve sectionCurve = contours[0];

            if (!shrinkToDimensions) {
                Curve[] offsetCurves = sectionCurve.Offset(worldXYPlane, thickness,
                                           0, CurveOffsetCornerStyle.Sharp);
                if (offsetCurves.Length != 1)
                {
                    throw new Exception("Could not properly offset curve.");
                }

                sectionCurve = offsetCurves[0];
            }

            // Place our curve to have concentric alignment with a bounding box
            // anchored at the provided origin, which we first need to offset
            // from being a bottom corner to "center"
            BoundingBox boundingBox = sectionCurve.GetBoundingBox(worldXYPlane);
            double bboxHeight = boundingBox.Max.X - boundingBox.Min.X;
            double bboxWidth = boundingBox.Max.Y - boundingBox.Min.Y;
            origin += new Vector3d(bboxHeight / 2, bboxWidth / 2, 0);
            origin += new Vector3d(thickness, 0, 0);
            double xDiff = origin.X - boundingBox.Center.X;
            double yDiff = origin.Y - boundingBox.Center.Y;
            Transform translation = Transform.Translation(new Vector3d(xDiff, yDiff, 0));
            sectionCurve.Transform(translation);

            return sectionCurve;

        }

        private Polyline getSectionPolyline(ObjRef objRef)
        {
            Brep brep = objRef.Brep();

            Vector3d zNorm = new Vector3d(0, 0, 1);
            Plane worldXYPlane = new Plane(ORIGIN, zNorm);
            Curve[] contours = Brep.CreateContourCurves(brep, worldXYPlane);
            Curve sectionCurve = contours[0];

            Polyline sectionPolyline;
            double[] sectionPolylineConversionResults;
            bool success = sectionCurve.TryGetPolyline(out sectionPolyline, out sectionPolylineConversionResults);

            if (!success) {
                throw new Exception("Cannot find section curve of polygon.");
            }

            return sectionPolyline;
        }

        protected RhinoList<Rectangle3d> generatePolyRects(Polyline charCurve,
                                                           double depth,
                                                  out Point3d bottomRightmostPoint,
                                                  double thickness = 0)
        {
            RhinoList<double> sectionDistances = new RhinoList<double>();
            Line[] sectionSegments = charCurve.GetSegments();

            foreach(Line segment in sectionSegments)
            {
                sectionDistances.Add(segment.Length);
            }

            // Add origin points, accounting for thickness
            RhinoList<Point3d> origins = new RhinoList<Point3d>();
            Point3d currOrigin = ORIGIN;

            // Loop invariant: CURR_ORIGIN be safely added and incremented
            foreach (double distance in sectionDistances)
            {
                origins.Add(currOrigin);
                currOrigin += new Vector3d(distance + thickness, 0, 0);
            }

            RhinoList<Rectangle3d> rectList = new RhinoList<Rectangle3d>();
            Rectangle3d currRect;
            int i;
            for (i = 0; i < origins.Count; i++)
            {
                currRect = MakeRect(origins[i], sectionDistances[i], depth, margin: thickness);
                rectList.Add(currRect);
            }

            bottomRightmostPoint = origins.Last + new Vector3d(sectionDistances[i - 1], 0, 0);

            return rectList;
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
            Point3d origin1 = ORIGIN + new Vector3d(xDist + thickness, 0, 0);
            Point3d origin2 = origin1 + new Vector3d(yDist + thickness, 0, 0);
            Point3d origin3 = origin2 + new Vector3d(xDist + thickness, 0, 0);

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
            double xDist = widthHeightDepthVect.X + (2 * thickness);
            double yDist = widthHeightDepthVect.Y + (2 * thickness);

            Point3d leftDownAdjustedOrigin = origin + new Vector3d(-thickness, -thickness, 0);

            // NOTE: maybe bad design, but we handle "margins" in this method
            return MakeRect(origin, xDist, yDist, margin: 0);
        }

        protected Polyline generateFingerJoint(Line jointLine, double thickness)
        {
            Point3d currPoint = new Point3d(jointLine.FromX, jointLine.FromY, 0);
            Point3dList points = new Point3dList();
            points.Add(currPoint);

            double xIncr, yIncr;
            yIncr = thickness;
            xIncr = thickness / 2;

            // An even finger count means the finger will be drawn right of the
            // center line
            int fingerCount = 0;
            int fingerDirection = -1;

            // Loop invariant: incrementing and placing current point will always
            // result in a point before the stoppingY
            while (currPoint.Y + yIncr <= jointLine.ToY)
            {
                // Multiplier for right finger on even, vice versa for odd
                fingerDirection = fingerCount % 2 == 0 ? 1 : -1;

                currPoint += new Vector3d(xIncr * fingerDirection, 0, 0);
                points.Add(currPoint);
                currPoint += new Vector3d(0, yIncr, 0);
                points.Add(currPoint);
                currPoint += new Vector3d(-xIncr * fingerDirection, 0, 0);
                points.Add(currPoint);

                fingerCount += 1;
            }

            // Finish the last truncated finger if necessary
            if (currPoint.Y < jointLine.ToY)
            {
                fingerDirection = fingerCount % 2 == 0 ? 1 : -1;

                currPoint += new Vector3d(xIncr * fingerDirection, 0, 0);
                points.Add(currPoint);
                currPoint += new Vector3d(0, jointLine.ToY - currPoint.Y, 0);
                points.Add(currPoint);
                currPoint += new Vector3d(-xIncr * fingerDirection, 0, 0);
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
            Point3d xAxisPt = new Point3d(width + margin, 0, 0) + origin;
            Point3d yAxisPt = new Point3d(0, height, 0) + origin;

            Vector3d zVector = new Vector3d(0, 0, 1);
            Point3d originLeftHalfMargin = new Vector3d(-(margin / 2), 0, 0) + origin;
            Plane worldXYPlane = new Plane(originLeftHalfMargin, zVector);

            Rectangle3d rect = new Rectangle3d(worldXYPlane, xAxisPt, yAxisPt);
            return rect;
        }

        private ObjRef shrinkBoxObj(ObjRef boxObjRef, double preShrinkValue)
        {
            Brep brep = boxObjRef.Brep();
            BoundingBox boundingBox = brep.GetBoundingBox(accurate: false);
            Point3d bboxCenter = boundingBox.Center;


            Transform scale = Transform.Scale(bboxCenter, preShrinkValue);
            brep.Transform(scale);
            return boxObjRef;
        }
    }
}
