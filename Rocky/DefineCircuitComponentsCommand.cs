using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;

namespace Rocky
{
    [System.Runtime.InteropServices.Guid("${Guid2}")]
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
            get { return "DefineCircuitComponentsCommand"; }
        }

        protected override Result RunCommand(Rhino.RhinoDoc doc, RunMode mode)
        {
            // TODO: start here modifying the behaviour of your command.

            return Result.Success;
        }
    }
}
