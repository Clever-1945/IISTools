using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISTools.Commands
{
    public class IISToolsCommandBase<T> where T : IISToolsCommandBase<T>
    {
        /// <summary> Command ID. </summary>
        public int CommandId;

        public CommandID MenuCommandId;

        public OleMenuCommand MenuCommand;

        /// <summary> Command menu group (command set GUID). </summary>fv
        public static Guid CommandSet = new Guid("128b6ee9-e7f7-45bd-89ea-76d1d86dba29");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        public AsyncPackage Package;
        public OleMenuCommandService CommandService;

        public Action OnAfterInitialize;

        public static async Task<T> InitializeAsync(AsyncPackage package, int commandId, Guid? commandSet = null)
        {
            if (commandSet != null)
                CommandSet = commandSet.Value;

            var instace = Activator.CreateInstance(typeof(T)) as T;
            instace.CommandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            instace.Package = package ?? throw new ArgumentNullException(nameof(package));
            instace.CommandId = commandId;

            instace.MenuCommandId = new CommandID(CommandSet, instace.CommandId);
            instace.MenuCommand = new OleMenuCommand(instace.Execute, instace.MenuCommandId);
            instace.CommandService.AddCommand(instace.MenuCommand);
            instace.OnAfterInitialize?.Invoke();

            return instace;
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        public virtual void Execute(object sender, EventArgs e)
        {

        }
    }
}
