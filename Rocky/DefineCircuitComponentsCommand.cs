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
                    RhinoList<Line> conduitLines = autoroute(circuitPoints);
                }
            }

            return Result.Success;
        }

        protected RhinoList<Line> autoroute(RhinoList<Point> points)
        {
            return new RhinoList<Point>();
        }
    }
}
