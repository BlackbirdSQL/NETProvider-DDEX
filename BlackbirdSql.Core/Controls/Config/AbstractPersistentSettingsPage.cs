﻿//
// Plagiarized from Community.VisualStudio.Toolkit extension
//
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

using BlackbirdSql.Core.Controls.Events;
using BlackbirdSql.Core.Controls.Interfaces;
using BlackbirdSql.Core.Ctl.ComponentModel;
using BlackbirdSql.Core.Ctl.Diagnostics;
using BlackbirdSql.Core.Ctl.Interfaces;
using BlackbirdSql.Core.Model.Config;
using BlackbirdSql.Core.Properties;
using Microsoft.VisualStudio.Shell;

using Control = System.Windows.Forms.Control;
using TextBox = System.Windows.Forms.TextBox;


namespace BlackbirdSql.Core.Controls.Config;

[SuppressMessage("Usage", "VSTHRD104:Offer async methods")]

// =========================================================================================================
//										AbstractPersistentSettingsPage Class
//
/// <summary>
/// VS Options DialogPage base class.
/// Disclosure: This class exposes some PropertyGridView members with hidden access modifiers using
/// the Visual Studio's Reflection library, so that we can implement a few standard or comparable windows
/// functionality features like single-click check boxes, radio buttons and cardinal synonyms into the
/// DialogPage property grid.
/// Common cardinal synonyms include current culture min[imum], max[imum], unlimited, default etc.
/// </summary>
// =========================================================================================================
[ComVisible(true)]
public abstract class AbstractPersistentSettingsPage<TPage, T> : AbstractSettingsPage<T>
	where TPage : AbstractSettingsPage<T> where T : AbstractSettingsModel<T>, new()
{

	// ---------------------------------------------------------------------------------
	#region Variables - AbstractPersistentSettingsPage
	// ---------------------------------------------------------------------------------


	#endregion Variables




	// =========================================================================================================
	#region Exposing Property Accessors - AbstractPersistentSettingsPage
	// =========================================================================================================


	#endregion Exposing Property Accessors




	// =========================================================================================================
	#region Property Accessors - AbstractPersistentSettingsPage
	// =========================================================================================================


	#endregion Property Accessors




	// =========================================================================================================
	#region Constructors / Destructors - AbstractPersistentSettingsPage
	// =========================================================================================================


	public AbstractPersistentSettingsPage()
	{
		// Tracer.Trace(GetType(), ".ctor");

		_Model = ThreadHelper.JoinableTaskFactory.Run(new Func<Task<T>>(AbstractSettingsModel<T>.CreateAsync));
		_Model.SettingsResetEvent += OnResetSettings;

		EditControlGotFocusEvent += _Model.OnEditControlGotFocus;
		EditControlLostFocusEvent += _Model.OnEditControlLostFocus;
		AutomationPropertyValueChangedEvent += _Model.OnAutomationPropertyValueChanged;
	}

	protected override void Dispose(bool disposing)
	{
		if (!disposing)
			return;

		if (_Model != null)
		{
			EditControlGotFocusEvent -= _Model.OnEditControlGotFocus;
			EditControlLostFocusEvent -= _Model.OnEditControlLostFocus;
			AutomationPropertyValueChangedEvent -= _Model.OnAutomationPropertyValueChanged;
			_Model = null;
		}

		base.Dispose(disposing);
	}

	#endregion Constructors / Destructors




	// =========================================================================================================
	#region Methods - AbstractPersistentSettingsPage
	// =========================================================================================================



	#endregion Event Handling


}
