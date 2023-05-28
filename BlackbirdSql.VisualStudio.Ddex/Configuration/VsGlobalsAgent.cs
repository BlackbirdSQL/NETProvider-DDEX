﻿// $License = https://github.com/BlackbirdSQL/NETProvider-DDEX/blob/master/Docs/license.txt
// $Authors = GA Christos (greg@blackbirdsql.org)



using System;

using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using EnvDTE;

using BlackbirdSql.Common;



namespace BlackbirdSql.VisualStudio.Ddex.Configuration;



// =========================================================================================================
//											VsGlobalsAgent Class
//
/// <summary>
/// Manages Globals and Visual Studio Options events
/// </summary>
// =========================================================================================================
internal class VsGlobalsAgent
{


	// =========================================================================================================
	#region Constants - VsGlobalsAgent
	// =========================================================================================================


	/// <summary>
	/// This key is the globals persistent key. When running in debug mode
	/// with PersistentValidation set to false any test solutions opened will have their persistent keys cleared
	/// </summary>
	const string G_PersistentKey = "GlobalBlackbirdPersistent";

	/// <summary>
	/// This key is the globals non-persistent key.
	/// </summary>
	const string G_TransitoryKey = "GlobalBlackbirdTransitory";

	/// <summary>
	/// For Projects: has been validated as a valid project type (Once it's been validated it's always been
	/// validated)
	/// For Solutions: has been loaded and in a validation state if <see cref="G_Valid"/> is false else
	/// validated
	/// </summary>
	const int G_Validated = 1;
	/// <summary>
	/// For Projects: Validated project is a valid executable C#/VB app (Project type). (Once [in]valid always
	/// [in]valid)
	/// For Solutions: Off: Solution has been loaded and is in a validation state. On: Validated
	/// (Only applicable if <see cref="G_Validated"/> is set)
	/// </summary>	
	const int G_Valid = 2;
	/// <summary>
	/// The app.config and all edmxs for a project have been scanned and configured if required. (Once
	/// successfully scanned always scanned)
	/// </summary>
	const int G_Scanned = 4;
	/// <summary>
	/// The app.config has the client system.data/DbProviderFactory configured and is good to go. (Once
	/// successfully configured always configured)
	/// </summary>
	const int G_DbProviderConfigured = 8;
	/// <summary>
	/// The app.config has the EntityFramework provider services and connection factory configured and is
	/// good to go. (Once successfully configured always configured)
	/// </summary>
	const int G_EFConfigured = 16;
	/// <summary>
	/// Existing legacy edmx's have been updated and are good to go. (Once all successfully updated always
	/// updated)
	/// </summary>
	const int G_EdmxsUpdated = 32;
	/// <summary>
	///  If at any point in solution projects' validation there was a fail, this is set to true on the
	///  solution and the solution Globals is reset to zero.
	///  Validation on failed solution entities will resume the next time the solution is loaded.
	/// </summary>
	const int G_ValidateFailed = 64;


	#endregion Constants





	// =========================================================================================================
	#region Private Variables
	// =========================================================================================================

	static VsGlobalsAgent _Instance;

	bool _ValidateConfig = false;
	bool _ValidateEdmx = false;

	bool _PersistentValidation = true;

	bool G_Persistent = true;

	/// <summary>
	/// The project and solution globals validation key. A single int32 using binary bitwise for the
	/// different status settings.
	/// If the PersistentValidation is true the persistent key will be used.
	/// </summary>
#if DEBUG
	string G_Key = G_TransitoryKey; // For non-persistent
#else
	G_Key = G_PersistentKey; // For persistent
#endif

	private readonly DTE _Dte = null;


	#endregion





	// =========================================================================================================
	#region Property Accessors - VsGlobalsAgent
	// =========================================================================================================


	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Returns a boolean indicating whether or not the app.config may be validated
	/// </summary>
	// ---------------------------------------------------------------------------------
	public bool ValidateConfig
	{
		get
		{
			return _ValidateConfig;
		}
	}


	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Returns a boolean indicating whether or not validation flags are persistent.
	/// Vlaidation flags are always persistent in Release builds.
	/// </summary>
	// ---------------------------------------------------------------------------------
	public bool PeristentValidation
	{
		get
		{
			return _PersistentValidation;
		}
	}


	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Returns a boolean indicating whether or not edmx files may be validated
	/// </summary>
	// ---------------------------------------------------------------------------------
	public bool ValidateEdmx
	{
		get
		{
			return _ValidateEdmx;
		}
	}



	// UI thread warning message suppression
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread",
		Justification = "UI thread ensured in code logic.")]
	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Get's or sets whether at any point a solution validation failed
	/// </summary>
	// ---------------------------------------------------------------------------------
	public bool IsValidateFailedStatus
	{
		get
		{
			return GetFlagStatus(_Dte.Solution.Globals, G_ValidateFailed);
		}

		set
		{
			SetFlagStatus(_Dte.Solution.Globals, G_ValidateFailed, value);
		}
	}


	#endregion Property accessors





	// =========================================================================================================
	#region Constructors / Destructors - VsGlobalsAgent
	// =========================================================================================================


	// ---------------------------------------------------------------------------------
	/// <summary>
	/// VsGlobalsAgent .ctor
	/// </summary>
	/// <param name="dte"></param>
	// ---------------------------------------------------------------------------------
	private VsGlobalsAgent(DTE dte)
	{
		_Dte = dte;

		try
		{
			_ValidateConfig = VsGeneralOptionModel.Instance.ValidateConfig;
			_ValidateEdmx = VsGeneralOptionModel.Instance.ValidateEdmx;

			_PersistentValidation = VsDebugOptionModel.Instance.PersistentValidation;


#if DEBUG
			G_Persistent = _PersistentValidation;

			if (!_PersistentValidation)
				G_Key = G_TransitoryKey;
			else
				G_Key = G_PersistentKey;

#else
			G_Persistent = true;
			G_Key = G_PersistentKey;
#endif

			Diag.EnableDiagnostics = VsGeneralOptionModel.Instance.EnableDiagnostics;
			Diag.EnableTaskLog = VsGeneralOptionModel.Instance.EnableTaskLog;

			Diag.EnableTrace = VsDebugOptionModel.Instance.EnableTrace;
			Diag.EnableDiagnosticsLog = VsDebugOptionModel.Instance.EnableDiagnosticsLog;
			Diag.EnableFbDiagnostics = VsDebugOptionModel.Instance.EnableFbDiagnostics;

			Diag.LogFile = VsDebugOptionModel.Instance.LogFile;
			Diag.FbLogFile = VsDebugOptionModel.Instance.FbLogFile;

			VsGeneralOptionModel.Saved += OnGeneralSettingsSaved;
			VsDebugOptionModel.Saved += OnDebugSettingsSaved;

		}
		catch (Exception ex)
		{
			Diag.Dug(ex, "Failed to retrieve Package settings");
		}

	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Gets the Singleton VsGlobalsAgent instance
	/// </summary>
	// ---------------------------------------------------------------------------------
	public static VsGlobalsAgent GetInstance(DTE dte)
	{
		return _Instance ??= new(dte);
	}





	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Gets the existing Singleton VsGlobalsAgent instance
	/// </summary>
	// ---------------------------------------------------------------------------------
	public static VsGlobalsAgent GetInstance()
	{
		if (_Instance == null)
		{
			NullReferenceException ex = new("Attempt to access uninitialized VsGlobalAgent instance");
			Diag.Dug(ex);
			throw ex;
		}

		return _Instance;
	}
	#endregion Constructors / Destructors





	// =========================================================================================================
	#region Methods - VsGlobalsAgent
	// =========================================================================================================


	// UI thread warning message suppression
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread",
	Justification = "UI thread ensured in code logic.")]
	// ---------------------------------------------------------------------------------
	/// <summary>
	/// For solutions: Sets a status indicator tagging it as previously validated or
	/// validated and valid.
	/// For projects: Sets a status indicator tagging it as previously validated for
	/// it's validity as a valid C#/VB executable.
	/// </summary>
	/// <param name="globals"></param>
	/// <param name="valid"></param>
	/// <returns>True if the operation was successful else False</returns>
	// ---------------------------------------------------------------------------------
	public bool SetIsValidStatus(Globals globals, bool valid)
	{
		return SetFlagStatus(globals, G_Validated, true, G_Valid, valid);
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Sets a status indicator tagging a project as having been scanned and it's
	/// app.config and edmxs validated.
	/// </summary>
	/// <param name="project"></param>
	/// <returns>True if the operation was successful else False</returns>
	// ---------------------------------------------------------------------------------
	public bool SetIsScannedStatus(Project project)
	{
		try
		{
			ThreadHelper.ThrowIfNotOnUIThread();
		}
		catch (Exception ex)
		{
			Diag.Dug(ex);
			return false;
		}

		return SetFlagStatus(project.Globals, G_Scanned, true);
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Sets status indicator tagging a project's app.config as having been validated
	/// for the DBProvider
	/// </summary>
	/// <param name="project"></param>
	/// <param name="valid"></param>
	/// <returns>True if the operation was successful else False</returns>
	// ---------------------------------------------------------------------------------
	public bool SetIsValidatedDbProviderStatus(Project project)
	{
		try
		{
			ThreadHelper.ThrowIfNotOnUIThread();
		}
		catch (Exception ex)
		{
			Diag.Dug(ex);
			return false;
		}

		return SetFlagStatus(project.Globals, G_DbProviderConfigured, true);
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Sets status indicator tagging a project's app.config as having been validated
	/// for EF.
	/// By definition the app.config will also have been validated for 
	/// </summary>
	/// <param name="project"></param>
	/// <param name="valid"></param>
	/// <returns>True if the operation was successful else False</returns>
	// ---------------------------------------------------------------------------------
	public bool SetIsValidatedEFStatus(Project project)
	{
		try
		{
			ThreadHelper.ThrowIfNotOnUIThread();
		}
		catch (Exception ex)
		{
			Diag.Dug(ex);
			return false;
		}

		return SetFlagStatus(project.Globals, G_EFConfigured, true, G_DbProviderConfigured, true);
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Sets non-persistent status indicator tagging a project's existing edmx's as
	/// having been validated/upgraded from legacy provider settings.
	/// </summary>
	/// <param name="project"></param>
	/// <param name="valid"></param>
	/// <returns>True if the operation was successful else False</returns>
	// ---------------------------------------------------------------------------------
	public bool SetIsUpdatedEdmxsStatus(Project project)
	{
		try
		{
			ThreadHelper.ThrowIfNotOnUIThread();
		}
		catch (Exception ex)
		{
			Diag.Dug(ex);
			return false;
		}

		return SetFlagStatus(project.Globals, G_EdmxsUpdated, true);
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Clears the status indicator of a solution.
	/// </summary>
	/// <param name="solution"></param>
	/// <returns>True if the operation was successful else False</returns>
	// ---------------------------------------------------------------------------------
	public bool ClearValidateStatus()
	{
		try
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (_Dte.Solution.Globals == null)
			{
				Diag.Dug(true, _Dte.Solution.FullName + ": Solution.Globals is null");
				return false;
			}

			if (!_Dte.Solution.Globals.get_VariableExists(G_Key))
			{
				return true;
			}

			_Dte.Solution.Globals[G_Key] = 0.ToString();
			_Dte.Solution.Globals.set_VariablePersists(G_Key, G_Persistent);
		}
		catch (Exception ex)
		{
			Diag.Dug(ex);
			return false;
		}


		return true;
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Clears the the persistent flag of a globals.
	/// </summary>
	/// <param name="globals"></param>
	/// <param name="key"></param>
	/// <returns>True if the operation was successful else False</returns>
	// ---------------------------------------------------------------------------------
	public bool ClearPersistentFlag(Globals globals, string key)
	{
		try
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (!globals.get_VariableExists(key))
				return true;

			globals.set_VariablePersists(key, false);
		}
		catch (Exception ex)
		{
			Diag.Dug(ex);
			return false;
		}


		return true;
	}



	// UI thread warning message suppression
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread",
		Justification = "UI thread ensured in code logic.")]
	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Verifies whether or not a solution is in a validation state (or previously
	/// validated) or a project has been validated as being valid or not.
	/// </summary>
	/// <param name="globals"></param>
	/// <returns></returns>
	// ---------------------------------------------------------------------------------
	public bool IsValidatedStatus(Globals globals)
	{
#if DEBUG
		if (!_PersistentValidation)
			ClearPersistentFlag(globals, G_PersistentKey);
#endif
		return GetFlagStatus(globals, G_Validated);
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Checks wether the project is a valid executable output type that requires
	/// configuration of the app.config
	/// </summary>
	/// <param name="project"></param>
	/// <returns>
	/// True if the project is a valid C#/VB executable project else false.
	/// </returns>
	/// <remarks>
	/// We're not going to worry about anything but C# and VB non=CSP projects
	/// </remarks>
	// ---------------------------------------------------------------------------------
	public bool IsValidExecutableProjectType(IVsSolution solution, Project project)
	{
		// We should already be on UI thread. Callers must ensure this can never happen
		try
		{
			ThreadHelper.ThrowIfNotOnUIThread();
		}
		catch (Exception ex)
		{
			Diag.Dug(ex);
			return false;
		}

		if (IsValidatedStatus(project.Globals))
			return IsValidStatus(project.Globals);

		// We're only supporting C# and VB projects for this - a dict list is at the end of this class
		if (project.Kind != "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"
			&& project.Kind != "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}")
		{
			SetIsValidStatus(project.Globals, false);
			return false;
		}

		bool result = false;


		// Don't process CPS projects
		solution.GetProjectOfUniqueName(project.UniqueName, out IVsHierarchy hierarchy);


		if (!IsCpsProject(hierarchy))
		{
			int outputType = int.MaxValue;

			if (project.Properties != null && project.Properties.Count > 0)
			{
				Property property = project.Properties.Item("OutputType");
				if (property != null)
					outputType = (int)property.Value;
			}


			if (outputType < 2)
				result = true;
		}

		SetIsValidStatus(project.Globals, result);

		return result;

	}


	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Identifies whether or not a project is a CPS project
	/// </summary>
	/// <param name="hierarchy"></param>
	/// <returns>true if project is CPS</returns>
	// ---------------------------------------------------------------------------------
	internal static bool IsCpsProject(IVsHierarchy hierarchy)
	{
		Requires.NotNull(hierarchy, "hierarchy");
		return hierarchy.IsCapabilityMatch("CPS");
	}



	// UI thread warning message suppression
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread",
		Justification = "UI thread ensured in code logic.")]
	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Verifies whether or not a solution has been validated or a project is a valid
	/// C#/VB executable. See remarks.
	/// </summary>
	/// <param name="globals"></param>
	/// <returns></returns>
	/// <remarks>
	/// Callers must call IsValidatedProjectStatus() before checking if a project is
	/// valid otherwise this indicator will be meaningless
	/// </remarks>
	// ---------------------------------------------------------------------------------
	public bool IsValidStatus(Globals globals)
	{
		return GetFlagStatus(globals, G_Valid);
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Verifies whether or not a project has been scanned and it's app.config and edmxs
	/// validated.
	/// </summary>
	/// <param name="project"></param>
	/// <returns></returns>
	// ---------------------------------------------------------------------------------
	public bool IsScannedStatus(Project project)
	{
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
		return GetFlagStatus(project.Globals, G_Scanned);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Verifies whether or not a project's App.config was validated for
	/// FirebirdSql.Data.FirebirdClient
	/// </summary>
	/// <param name="project"></param>
	/// <returns></returns>
	// ---------------------------------------------------------------------------------
	public bool IsConfiguredDbProviderStatus(Project project)
	{
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
		return GetFlagStatus(project.Globals, G_DbProviderConfigured);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Verifies whether or not a project's App.config was validated for
	/// EntityFramework.Firebird
	/// </summary>
	/// <param name="project"></param>
	/// <returns></returns>
	// ---------------------------------------------------------------------------------
	public bool IsConfiguredEFStatus(Project project)
	{
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
		return GetFlagStatus(project.Globals, G_EFConfigured);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Verifies whether or not a project's existing edmx models were updated from
	/// using legacy data providers to current.
	/// Firebird Client and EntityFramework providers.
	/// </summary>
	/// <param name="project"></param>
	/// <returns></returns>
	// ---------------------------------------------------------------------------------
	public bool IsUpdatedEdmxsStatus(Project project)
	{
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
		return GetFlagStatus(project.Globals, G_EdmxsUpdated);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Sets a Globals indicator flag.
	/// </summary>
	/// <param name="globals"></param>
	/// <param name="flag"></param>
	/// <param name="enabled"></param>
	/// <param name="flag2"></param>
	/// <param name="enabled2"></param>
	/// <returns>True if the operation was successful else False</returns>
	// ---------------------------------------------------------------------------------
	public bool SetFlagStatus(Globals globals, int flag, bool enabled, int flag2 = 0, bool enabled2 = false)
	{
		bool exists = false;
		int value = 0;
		string str;

		try
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (globals == null)
			{
				ArgumentNullException ex = new("Globals is null");
				Diag.Dug(ex);
				throw ex;
			}


			if (globals.get_VariableExists(G_Key))
			{
				str = (string)globals[G_Key];
				value = str == "" ? 0 : int.Parse(str);
				exists = true;
			}

			if (exists && (value & flag) != 0 == enabled)
			{
				if (flag2 == 0 || (value & flag2) != 0 == enabled2)
				{
					return true;
				}
			}

			if (enabled)
				value |= flag;
			else
				value &= ~flag;

			if (flag2 != 0)
			{
				if (enabled2)
					value |= flag2;
				else
					value &= ~flag2;
			}


			globals[G_Key] = value.ToString();

			if (!exists && G_Persistent)
				globals.set_VariablePersists(G_Key, G_Persistent);
		}
		catch (Exception ex)
		{
			Diag.Dug(ex);
			return false;
		}


		return true;
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Retrieves an indicator flag's status
	/// </summary>
	/// <param name="globals"></param>
	/// <param name="flag"></param>
	/// <returns></returns>
	// ---------------------------------------------------------------------------------
	protected bool GetFlagStatus(Globals globals, int flag)
	{
		int value;
		string str;

		try
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (globals == null)
			{
				ArgumentNullException ex = new("Globals is null");
				Diag.Dug(ex);
				throw ex;
			}

			if (globals.get_VariableExists(G_Key))
			{
				str = (string)globals[G_Key];
				value = str == "" ? 0 : int.Parse(str);

				return (value & flag) != 0;
			}

		}
		catch (Exception ex)
		{
			Diag.Dug(ex);
			return false;
		}

		return false;
	}


	#endregion Methods





	// =========================================================================================================
	#region Event handlers - VsGlobalsAgent
	// =========================================================================================================


	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Debug options saved event handler 
	/// </summary>
	/// <param name="e"></param>
	// ---------------------------------------------------------------------------------
	void OnDebugSettingsSaved(VsDebugOptionModel e)
	{
		_PersistentValidation = e.PersistentValidation;


#if DEBUG
		G_Persistent = _PersistentValidation;

		if (!_PersistentValidation)
			G_Key = G_TransitoryKey;
		else
			G_Key = G_PersistentKey;
#else
		G_Persistent = true;
		G_Key = G_PersistentKey;
#endif


		Diag.EnableTrace = e.EnableTrace;
		Diag.EnableDiagnosticsLog = e.EnableDiagnosticsLog;
		Diag.LogFile = e.LogFile;
		Diag.EnableFbDiagnostics = e.EnableFbDiagnostics;
		Diag.FbLogFile = e.FbLogFile;


	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// General options saved event handler
	/// </summary>
	/// <param name="e"></param>
	// ---------------------------------------------------------------------------------
	void OnGeneralSettingsSaved(VsGeneralOptionModel e)
	{
		_ValidateConfig = e.ValidateConfig;
		_ValidateEdmx = e.ValidateEdmx;

		Diag.EnableTaskLog = e.EnableTaskLog;
		Diag.EnableDiagnostics = e.EnableDiagnostics;
	}


	#endregion Event handlers

}