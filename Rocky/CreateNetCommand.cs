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
            using (GetObject getObjectAction = new GetObject())
            {
                getObjectAction.SetCommandPrompt("Select box for net");
                if (getObjectAction.Get() != GetResult.Object)
                {
                    RhinoApp.WriteLine("Must select a box.");
                    return getObjectAction.CommandResult();
                }
                RhinoApp.WriteLine("Result is {0}",getObjectAction.Object(0));
                return Result.Success;
            }   
        }
    }
}
