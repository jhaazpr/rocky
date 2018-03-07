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
                RhinoApp.WriteLine("Z is {0}", zDist);
                return Result.Success;
            }
            return Result.Failure;
        }
    }
}
