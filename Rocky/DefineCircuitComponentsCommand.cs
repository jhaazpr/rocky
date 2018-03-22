using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Collections;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;

namespace Rocky
{
    //[System.Runtime.InteropServices.Guid("${Guid2}")]
    public class DefineCircuitComponentsCommand : Rhino.Commands.Command
    {
        public DefineCircuitComponentsCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static DefineCircuitComponentsCommand Instance
        {
            get;
            private set;
        }

        public override string EnglishName
        {
            get { return "DefineCircuitComponents"; }
        }

        protected override Result RunCommand(Rhino.RhinoDoc doc, RunMode mode)
        {
            const Rhino.DocObjects.ObjectType selFilter = Rhino.DocObjects.ObjectType.Point;
            Rhino.DocObjects.ObjRef[] pointObjRefs;

            Result getPointsResults = RhinoGet.GetMultipleObjects("Select circuit component endpoints",
                                                                  false, selFilter, out pointObjRefs);

            if (getPointsResults == Result.Success)
            {
                RhinoList<Point> circuitPoints = new RhinoList<Point>();
                foreach (Rhino.DocObjects.ObjRef objRef in pointObjRefs)
                {
                    circuitPoints.Add(objRef.Point());
                }

                RhinoList<Line> conduitLines = autoroute(circuitPoints);
                foreach (Line conduitLine in conduitLines)
                {
                    doc.Objects.AddLine(conduitLine);
                }
                doc.Views.Redraw();
                return Result.Success;
            }

            return Result.Failure;
        }

        protected RhinoList<Line> autoroute(RhinoList<Point> points)
        {
            // TODO: define heuristics e.g. penalize length
            // TODO: define cost function for heuristics, do gradient descent

            // NOTE: currently we use a dumb greedy algorithm, but will change later

            RhinoList<Line> conduitLines = new RhinoList<Line>();

            conduitLines = naiveGreedyRoute(points);

            return conduitLines;
        }

        private RhinoList<Line> naiveGreedyRoute(RhinoList<Point> points)
        {
            RhinoList<Line> conduitLines = new RhinoList<Line>();

            for (int i = 0; i < points.Count; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    Point3d fromPoint, toPoint;
                    fromPoint = points[i].Location;
                    toPoint = points[j].Location;
                    conduitLines.Add(new Line(fromPoint, toPoint));
                }
            }

            return conduitLines;
        }

        private RhinoList<Line> iteratedOneSteiner(RhinoList<Point> points)
        {
            // TODO
        }
    }
}
