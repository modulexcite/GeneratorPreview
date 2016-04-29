﻿//------------------------------------------------------------------------------
// <copyright file="PickGeneratorCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using CodeConnect.GeneratorPreview.Helpers;
using CodeConnect.GeneratorPreview.View;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using CodeConnect.GeneratorPreview.Execution;

namespace CodeConnect.GeneratorPreview
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PickGeneratorCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 256;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("1ecf9a6a-6521-444a-b5db-20951ade188a");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;
        private readonly GeneratorManager _manager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PickGeneratorCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private PickGeneratorCommand(Package package, GeneratorManager manager)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _package = package;

            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }
            _manager = manager;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PickGeneratorCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this._package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package, GeneratorManager manager)
        {
            Instance = new PickGeneratorCommand(package, manager);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void MenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                var textManager = (IVsTextManager)ServiceProvider.GetService(typeof(SVsTextManager));
                var nodeAndDocument = (await Helpers.WorkspaceHelpers.GetSelectedSyntaxNode(textManager));
                var node = nodeAndDocument.Item1;
                var baseMethod = node.AncestorsAndSelf().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
                _manager.SetGenerator(baseMethod);
                StatusBar.ShowStatus("Generator picked.");
            }
            catch (Exception ex)
            {
                StatusBar.ShowStatus("Generator not picked: " + ex.Message);
            }
        }
    }
}
